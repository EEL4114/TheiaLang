using System.Text;

namespace TheiaLang;

public static class IRGenerator
{
    public static readonly List<string> BuiltinTypes = new List<string>
    {
        "bool",

        "int",      // literals only
        "s8",
        "s16",
        "s32",
        "s64",
        "s128",
        "s256",

        "float",    // literals only
        "f16",
        "f32",
        "f64",
        "f128",

        // "void",
    };

    public static bool IsBuiltinType(string type) => BuiltinTypes.Contains(type);
    public static int BuiltinTypeIndex(string type) => BuiltinTypes.IndexOf(type);

    // we won't deal with SSA optimisation for now but once we have all basic features done we will
    static readonly Stack<Dictionary<string, (string ptr, string? ssa)>> allocas = new();
    static readonly Stack<Dictionary<string, TypeInfo>> varTypes = new();
    static ulong tmpCounter = 0;
    static Scope? currentScope;
    static ulong labelCounter = 0;

    public static void Emit(ProgramNode program, Scope globalScope, string pathLl)
    {
        allocas.Clear();
        varTypes.Clear();

        allocas.Push(new Dictionary<string, (string ptr, string? ssa)>());
        varTypes.Push(new Dictionary<string, TypeInfo>());

        currentScope = globalScope;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("; ModuleID = 'theia_module'");
        sb.AppendLine("target triple = \"x86_64-pc-windows-msvc19.44.35211\"");
        sb.AppendLine("declare i32 @puts(i8*, ...)");
        sb.AppendLine("@.theia_print_str = private constant[19 x i8] c\"Hello from Theia!\\0A\\00\"");

        sb.AppendLine("declare i32 @printf(i8*, ...)");
        sb.AppendLine("@.print_ret_fmt = private constant [16 x i8] c\"%s returned %d\\0A\\00\"");

        sb.AppendLine();

        foreach (INode decl in program.Nodes)
            if (decl is StructDeclaration sd)
                EmitStructType(sd, sb);

        sb.AppendLine();

        foreach (INode node in program.Nodes)
            switch (node)
            {
                case FunctionDeclaration fn:
                    fn.Name = $"{fn.Scope!.FullName}";
                    EmitFunction(fn, sb);
                    break;

                case StructDeclaration sd:
                    // for each method, emit it as a real LLVM function
                    EnterScope(sd.Name);
                    foreach (FunctionDeclaration function in sd.Functions)
                    {
                        // create a synthetic FunctionDeclaration with a mangled name
                        string mangle = $"{function.Scope!.FullName}";
                        function.Name = mangle;
                        EmitFunction(function, sb);
                    }
                    ExitScope();
                    break;
            }

        File.WriteAllText(pathLl, sb.ToString());
    }

    static void EmitStructType(StructDeclaration sd, StringBuilder sb)
    {
        List<string> fieldLLVMTypes = new List<string>();
        foreach (TypeNamePair field in sd.Fields)
            fieldLLVMTypes.Add(TypeToLLVM(field.ResolvedType!)!);

        string fieldIr = string.Join(
            ", ",
            fieldLLVMTypes
        );

        string llvmName = $"%{sd.Name}";

        IEnumerable<string> fieldTypes = sd.Fields.Select(f => f.TypeName);

        // emit: %StructName = type { <field1>, <field2>, … }
        sb.AppendLine($"{llvmName} = type {{ {fieldIr} }}");

        // TODO this seems unnecessary?
        TypeInfo typeInfo = new TypeInfo
        (
            sd.Name,
            fieldNames: sd.Fields.Select(f => f.Name).ToList(),
            fieldTypes: fieldTypes.ToList()
        );

        varTypes.Peek()[sd.Name] = typeInfo;
    }

    #region Functions
    static void EmitFunction(FunctionDeclaration fn, StringBuilder sb)
    {
        EnterScope(fn.Scope!);

        string returnTypeLLVM = TypeToLLVM(fn.ResolvedType)!;

        List<string> args = new List<string>();
        if (fn.Scope!.Parent?.DeclaringNode is StructDeclaration parentStruct)
        {
            string structPtrType = $"%{parentStruct.Name}*";
            args.Add($"{structPtrType} %this");
        }

        // this is messy but works for now?

        string paramList = "";
        foreach (TypeNamePair parameter in fn.Arguments)
        {
            string parameterLLVMType = TypeToLLVM(parameter.ResolvedType!)!;
            args.Add($"{parameterLLVMType} %{parameter.Name}");
        }
        paramList = string.Join(", ", args);

        sb.AppendLine($"@.fn_{currentScope!.Name}_str = private constant [{currentScope.Name.Length + 1} x i8] c\"{currentScope.Name}\\00\"");

        sb.AppendLine($"define {returnTypeLLVM} @{fn.Name}({paramList}) {{");
        sb.AppendLine("entry:");

        foreach (TypeNamePair parameter in fn.Arguments)
        {
            string LLVMType = TypeToLLVM(parameter.ResolvedType!)!;
            string varName = $"%{NewTempVar()}";

            sb.AppendLine($"  {varName} = alloca {LLVMType}");
            sb.AppendLine($"  store {LLVMType} %{parameter.Name}, {LLVMType}* {varName}");

            allocas.Peek()[parameter.Name] = (varName, null);
            varTypes.Peek()[parameter.Name] = parameter.ResolvedType!;
        }

        foreach (IStatement statement in fn.Statements)
            EmitStatement(statement, sb);

        bool hasReturn = fn.Statements.Any(s => s is ReturnStatement);

        sb.AppendLine("}");
        sb.AppendLine();
        ExitScope();
    }

    #endregion

    #region Statements

    static void EmitStatement(IStatement statement, StringBuilder sb)
    {
        switch (statement)
        {
            case VariableDeclaration v: EmitVariableDeclaration(v, sb); break;
            case AssignmentStatement a: EmitAssignmentStatement(a, sb); break;
            case CompoundAssignmentStatement c: EmitCompoundAssignmentStatement(c, sb); break;
            case IfStatement i: EmitIfStatement(i, sb); break;
            case ForStatement f: EmitForStatement(f, sb); break;
            case ReturnStatement r: EmitReturnStatement(r, sb); break;
            case ExpressionStatement e: EmitExpressionStatement(e, sb); break;
            default: throw new Exception($"Unknown Statement: {statement.GetType().Name}");
        }
    }

    static void EmitVariableDeclaration(VariableDeclaration variableDeclaration, StringBuilder sb)
    {
        string LLVMType = TypeToLLVM(variableDeclaration.ResolvedType!)!;

        string slot = $"%{variableDeclaration.Name}_{currentScope!.Name}";
        sb.AppendLine($"  {slot} = alloca {LLVMType}");

        allocas.Peek()[variableDeclaration.Name] = (slot, null);
        varTypes.Peek()[variableDeclaration.Name] = variableDeclaration.ResolvedType!;

        if (variableDeclaration.Init is InstantiationExpression inst)
        {
            for (int i = 0; i < inst.Arguments.Count; i++)
            {
                // evaluate the argument
                (StringBuilder argCode, string argReg) = EmitExpression(inst.Arguments[i]);
                sb.Append(argCode);

                string gep = $"%{NewTempVar()}";
                sb.AppendLine(
                  $"  {gep} = getelementptr {LLVMType}, {LLVMType}* {slot}, i32 0, i32 {i}");

                string LLVMTypeArg = TypeToLLVM(inst.Arguments[i].ResolvedType!)!;

                sb.AppendLine(
                  $"  store {LLVMTypeArg} {argReg}, {LLVMTypeArg}* {gep}");
            }
            sb.AppendLine();
        }
        else if (variableDeclaration.Init != null)
        {
            (StringBuilder initCode, string initReg) = EmitExpression(variableDeclaration.Init);
            sb.Append(initCode);
            sb.AppendLine(
              $"  store {LLVMType} {initReg}, {LLVMType}* {slot}");
        }
    }

    static void EmitAssignmentStatement(AssignmentStatement assignment, StringBuilder sb)
    {
        (sb, string ptr, string LLVMType) = EmitAddressOf(assignment.Target, sb);

        (StringBuilder code, string val) = EmitExpression(assignment.Expression);
        sb.Append(code);
        sb.AppendLine($"  store {LLVMType} {val}, {LLVMType}* {ptr}");
    }

    static void EmitCompoundAssignmentStatement(CompoundAssignmentStatement assignment, StringBuilder sb)
    {
        (sb, string ptr, string LLVMType) = EmitAddressOf(assignment.Target, sb);

        BinaryOperator op = assignment.Op switch
        {
            BinaryOperator.PlusEqual => BinaryOperator.Add,
            BinaryOperator.MinusEqual => BinaryOperator.Subtract,
            BinaryOperator.MultEqual => BinaryOperator.Multiply,
            BinaryOperator.DivEqual => BinaryOperator.Divide,
            _ => throw new Exception($"Invalid compound operator: '{assignment.Op}'")
        };

        (StringBuilder code, string val) = EmitExpression(new BinaryExpression(assignment.Target, op, assignment.Expression));

        sb.Append(code);
        sb.AppendLine($"  store {LLVMType} {val}, {LLVMType}* {ptr}");
    }

    static void EmitIfStatement(IfStatement ifStatement, StringBuilder sb)
    {
        (StringBuilder condCode, string condReg) = EmitExpression(ifStatement.Condition);
        sb.Append(condCode);  // discard the result, but emit code for side effects

        string thenLabel = $"if_then_{labelCounter}";
        string? elseLabel = ifStatement.ElseBranch != null
                                ? $"if_else_{labelCounter}"
                                : null;
        string mergeLabel = $"if_end_{labelCounter}";
        labelCounter++;

        sb.AppendLine(
              $"  br i1 {condReg}, label %{thenLabel}, label %{elseLabel ?? mergeLabel}");

        sb.AppendLine($"{thenLabel}:");
        EnterScope(ifStatement.ThenScope);
        foreach (IStatement statement in ifStatement.ThenBranch)
            EmitStatement(statement, sb);
        ExitScope();
        sb.AppendLine($"  br label %{mergeLabel}");

        if (ifStatement.ElseBranch != null)
        {
            EnterScope(ifStatement.ElseScope!);
            sb.AppendLine($"{elseLabel}:");
            foreach (IStatement statement in ifStatement.ElseBranch)
                EmitStatement(statement, sb);
            ExitScope();
            sb.AppendLine($"  br label %{mergeLabel}");
        }
        sb.AppendLine($"{mergeLabel}:");
    }

    static void EmitForStatement(ForStatement forStatement, StringBuilder sb)
    {
        // Loop init
        EmitStatement(forStatement.Initialiser!, sb);
        EnterScope(forStatement.Scope);
        string condLabel = $"for_cond{labelCounter}";
        string bodyLabel = $"for_body{labelCounter}";
        string iterLabel = $"for_iter{labelCounter}";
        string endLabel = $"for_end{labelCounter}";
        labelCounter++;

        sb.AppendLine($"  br label %{condLabel}");

        // Loop Condition
        sb.AppendLine($"{condLabel}:");
        (StringBuilder condCode, string condReg) = EmitExpression(forStatement.Condition!);
        sb.Append(condCode);
        sb.AppendLine($"  br i1 {condReg}, label %{bodyLabel}, label %{endLabel}");
        // Loop Body
        sb.AppendLine($"{bodyLabel}:");
        foreach (IStatement statement in forStatement.Body)
            EmitStatement(statement, sb);
        sb.AppendLine($"  br label %{iterLabel}");
        // Loop Iterator
        sb.AppendLine($"{iterLabel}:");
        EmitStatement(forStatement.Iterator!, sb);
        sb.AppendLine($"  br label %{condLabel}");
        // Loop End
        sb.AppendLine($"{endLabel}:");

        ExitScope();
    }

    static void EmitReturnStatement(ReturnStatement returnStatement, StringBuilder sb)
    {
        (StringBuilder code, string val) = EmitExpression(returnStatement.Expression);
        sb.Append(code);
        string LLVMType = TypeToLLVM(returnStatement.Expression.ResolvedType!)!;

        if (currentScope!.DeclaringNode is FunctionDeclaration)
            sb.AppendLine(
                $"  call i32 (i8*, ...) @printf(i8* getelementptr inbounds " +
            $"([16 x i8], [16 x i8]* @.print_ret_fmt, i32 0, i32 0), " +
            $"i8* getelementptr inbounds ([{LLVMType.Length + 1} x i8], [{LLVMType.Length + 1} x i8]* @.fn_{currentScope.Name}_str, i32 0, i32 0), " +
            $"{LLVMType} {val})"
            );

        sb.AppendLine($"  ret {LLVMType} {val}");
    }

    static void EmitExpressionStatement(ExpressionStatement stmt, StringBuilder sb)
    {
        (StringBuilder code, _) = EmitExpression(stmt.Expression);
        sb.Append(code);  // discard the result, but emit code for side effects
    }

    #endregion

    #region Expressions
    static (StringBuilder, string) EmitExpression(IExpression expression)
    {
        StringBuilder code = new StringBuilder();
        return expression switch
        {
            UnaryExpression unaryExpression => EmitUnaryExpression(unaryExpression, code),
            LiteralExpression literal => EmitLiteralExpression(literal, code),
            IdentifierExpression identifier => EmitIdentifierExpression(identifier, code),
            BinaryExpression binaryExpression => EmitBinaryExpression(binaryExpression, code),
            InstantiationExpression instantiation => EmitInstantiationExpression(instantiation, code),
            CallExpression call => EmitCallExpression(call, code),
            MemberAccessExpression memberAccess => EmitMemberAccessExpression(memberAccess, code),
            IndexExpression index => EmitIndexExpression(index, code),
            _ => throw new Exception($"Unsupported expression: {expression.GetType().Name}"),
        };
    }
    static (StringBuilder code, string name) EmitUnaryExpression(UnaryExpression unaryExpression, StringBuilder code)
    {
        TypeInfo typeInfo = unaryExpression.Operand.ResolvedType!;
        switch (unaryExpression.Op)
        {
            case UnaryOperator.Negate:
                (StringBuilder cl, string val) = EmitExpression(unaryExpression.Operand);
                code.Append(cl);

                string tmp = NewTempVar();

                string instr = typeInfo.TypeName switch
                {
                    "s32" => "sub",
                    "f32" => "fsub",
                    _ => throw new NotSupportedException($"Unary - on {typeInfo.TypeName}")
                };

                string zero = typeInfo.TypeName == "s32" ? "0" : "0.0";

                string llvmType = TypeToLLVM(typeInfo)!;

                code.AppendLine($"  %{tmp} = {instr} {llvmType} {zero}, {val}");
                return (code, $"%{tmp}");
            case UnaryOperator.AddressOf:
                (code, string ptr, _) = EmitAddressOf(unaryExpression.Operand, code);
                return (code, ptr);
            case UnaryOperator.Dereference:
                (cl, val) = EmitExpression(unaryExpression.Operand);
                code.Append(cl);
                TypeInfo pointee = unaryExpression.Operand.ResolvedType!.Pointee!;
                llvmType = TypeToLLVM(pointee)!;
                tmp = NewTempVar();
                code.AppendLine($"  %{tmp} = load {llvmType}, {llvmType}* {val}");
                return (code, $"%{tmp}");

            default: throw new NotSupportedException($"{unaryExpression.Op}");
        }
    }

    static (StringBuilder code, string name) EmitLiteralExpression(
    LiteralExpression literalExpression,
    StringBuilder code)
    {
        // Make sure the semantic pass has filled in the type
        TypeInfo typeInfo = literalExpression.ResolvedType
            ?? throw new InvalidOperationException("Literal has no ResolvedType");

        // Integer literals
        if (literalExpression.Value is int i)
        {
            return typeInfo.TypeName switch
            {
                "f16" or "f32" or "f64" => (code, $"{i}.0"),// decimal is fine
                "f128" => (code, ToHexFp128(i)),
                _ => (code, literalExpression.Lexeme),
            };
        }

        // Floating‐point literals
        if (literalExpression.Value is double d)
        {
            return typeInfo.TypeName switch
            {
                "f128" => (code, ToHexFp128(d)),// the original Lexeme is decimal; convert to hex‐float
                _ => (code, literalExpression.Lexeme),// leave as written for f32/f64
            };
        }

        // Booleans
        if (literalExpression.Value is bool b)
            return (code, b ? "1" : "0");

        throw new Exception("Unknown literal");
    }

    static (StringBuilder code, string name) EmitIdentifierExpression(IdentifierExpression identifier, StringBuilder code)
    {
        if (TryResolveSlot(identifier.Name, out (string ptr, string? ssa) alloc, out TypeInfo? typeInfo))
        {
            string LLVMType = TypeToLLVM(typeInfo)!;

            string tmp = $"tmp{tmpCounter++}";
            code.AppendLine($"  %{tmp} = load {LLVMType}, {LLVMType}* {alloc.ptr}");
            return (code, $"%{tmp}");
        }

        if (currentScope!.Parent?.DeclaringNode is StructDeclaration sd)
        {
            int index = sd.Fields.FindIndex(f => f.Name == identifier.Name);
            if (index >= 0)
            {
                string LLVMType = TypeToLLVM(sd.Fields[index].ResolvedType!)!;

                string gep = $"%{NewTempVar()}";
                code.AppendLine(
                    $"  {gep} = getelementptr %{sd.ResolvedType.TypeName}, %{sd.ResolvedType.TypeName}* %this, i32 0, i32 {index}");
                string tempIdentifier = $"%{NewTempVar()}";
                code.AppendLine($"  {tempIdentifier} = load {LLVMType}, {LLVMType}* {gep}");
                return (code, tempIdentifier);
            }
        }

        throw new Exception($"Undefined variable or field '{identifier.Name}'");
    }

    static (StringBuilder code, string name) EmitBinaryExpression(BinaryExpression binaryExpression, StringBuilder code)
    {
        (StringBuilder cl, string vl) = EmitExpression(binaryExpression.Left);
        (StringBuilder cr, string vr) = EmitExpression(binaryExpression.Right);
        code.Append(cl);
        code.Append(cr);

        TypeInfo typeInfo = binaryExpression.Left.ResolvedType!;
        string tmp = $"tmp{tmpCounter++}";
        string op;

        if (!IsBuiltinType(typeInfo.TypeName))
        {
            throw new Exception($"Unsupported type '{typeInfo.TypeName}'");
        }
        else if (typeInfo.TypeName == "bool") op = binaryExpression.Op switch
        {
            BinaryOperator.EqualEqual => "icmp eq",
            BinaryOperator.NotEqual => "icmp ne",
            BinaryOperator.AND => "and",
            BinaryOperator.OR => "or",
            _ => throw new Exception($"Op {binaryExpression.Op}")
        };
        else if (typeInfo.TypeName.StartsWith('s')) op = binaryExpression.Op switch
        {
            BinaryOperator.Add => "add",
            BinaryOperator.Subtract => "sub",
            BinaryOperator.Multiply => "mul",
            BinaryOperator.Greater => "icmp sgt",
            BinaryOperator.Less => "icmp slt",
            BinaryOperator.EqualEqual => "icmp eq",
            BinaryOperator.NotEqual => "icmp ne",
            _ => throw new Exception($"Op {binaryExpression.Op}")
        };
        else if (typeInfo.TypeName.StartsWith('f')) op = binaryExpression.Op switch
        {
            BinaryOperator.Add => "fadd",
            BinaryOperator.Subtract => "fsub",
            BinaryOperator.Multiply => "fmul",
            BinaryOperator.Greater => "fcmp ogt",
            BinaryOperator.Less => "fcmp olt",
            BinaryOperator.EqualEqual => "fcmp oeq",
            BinaryOperator.NotEqual => "fcmp one",
            _ => throw new Exception($"Op {binaryExpression.Op}")
        };

        else throw new Exception($"Unsupported type '{typeInfo.TypeName}'");

        string LLVMType = TypeToLLVM(typeInfo)!;

        code.AppendLine($"  %{tmp} = {op} {LLVMType} {vl}, {vr}");
        code.AppendLine();

        return (code, $"%{tmp}");
    }

    static (StringBuilder code, string name) EmitInstantiationExpression(InstantiationExpression instantiation, StringBuilder code)
    {
        string irType = instantiation.TypeName;
        string ptrName = $"%{NewTempVar()}";
        code.AppendLine($"  {ptrName} = alloca {irType}");

        for (int i = 0; i < instantiation.Arguments.Count; i++)
        {
            (StringBuilder argCode, string argReg) = EmitExpression(instantiation.Arguments[i]);
            code.Append(argCode);

            string gep = $"%{NewTempVar()}";
            code.AppendLine(
                $"  {gep} = getelementptr {irType}, {irType}* {ptrName}, i32 0, i32 {i}");

            string llvmType = TypeToLLVM(instantiation.Arguments[i].ResolvedType!)!;

            code.AppendLine($"  store {llvmType} {argReg}, {llvmType}* {gep}");
        }

        return (code, ptrName);
    }

    static (StringBuilder code, string name) EmitCallExpression(CallExpression call, StringBuilder code)
    {
        if (!currentScope!.TryLookup(call.CalleeName, out SymbolInfo? calleeInfo, out _))
            throw new Exception($"Undefined identifier '{call.CalleeName}' in {currentScope.FullName}");

        if (calleeInfo!.Kind != SymbolKind.Function)
            throw new Exception($"'{call.CalleeName}' is not a function in scope '{currentScope.FullName}'");

        if (call.Arguments.Count != calleeInfo.Parameters!.Count)
            Log.Error(11,  // pick an unused code
                $"Function '{call.CalleeName}' expects {calleeInfo.Parameters.Count} arguments, " +
                $"but got {call.Arguments.Count}");
        string retTy = TypeToLLVM(calleeInfo.Type)!;

        List<string> argumentList = new List<string>();

        for (int i = 0; i < call.Arguments.Count; i++)
        {
            IExpression argument = call.Arguments[i];

            (StringBuilder argCode, string argReg) = EmitExpression(argument);
            code.Append(argCode);

            // infer the LLVM type of the argument
            TypeInfo actualType = argument.ResolvedType!;
            string actualLLVMType = TypeToLLVM(actualType)!;
            string expectedLLVMType = TypeToLLVM(calleeInfo.Type)!;

            if (actualType.TypeName != calleeInfo.Type.TypeName)
                Log.Error(12,
                    $"Type mismatch in call to '{call.CalleeName}': parameter '{calleeInfo.Parameters[i].Name}' " +
                    $"expected {calleeInfo.Type.TypeName}, got {actualType.TypeName}");

            argumentList.Add($"{actualLLVMType} {argReg}");
        }

        string tmp = $"%{NewTempVar()}";
        code.AppendLine(
            $"  {tmp} = call {retTy} @{calleeInfo.Name}({string.Join(", ", argumentList)})");
        return (code, tmp);
    }

    static (StringBuilder code, string name) EmitMemberAccessExpression(MemberAccessExpression memberAccess, StringBuilder code)
    {
        (code, string ptr, string llvmType) = EmitAddressOf(memberAccess, code);

        string tmp = $"%{NewTempVar()}";
        code.AppendLine($"  {tmp} = load {llvmType}, {llvmType}* {ptr}");
        return (code, tmp);
    }

    static (StringBuilder code, string name) EmitIndexExpression(IndexExpression index, StringBuilder code)
    {
        (code, string ptr, string llvmType) = EmitAddressOf(index, code);

        string tmp = $"%{NewTempVar()}";
        code.AppendLine($"  {tmp} = load {llvmType}, {llvmType}* {ptr}");
        return (code, tmp);
    }


    #endregion

    #region  Helpers

    static (StringBuilder code, string ptr, string llvmType) EmitAddressOf(IExpression target, StringBuilder code)
    {
        switch (target)
        {
            case IdentifierExpression identifier:
                if (!TryResolveSlot(identifier.Name, out (string ptr, string? ssa) alloc, out TypeInfo? typeInfo))
                    throw new Exception($"Undefined Identifier '{identifier.Name}'");
                string ptr = alloc.ptr;
                string llvmType = TypeToLLVM(typeInfo)!;
                return (code, ptr, llvmType);
            case UnaryExpression u when u.Op == UnaryOperator.AddressOf:
                // treat @foo exactly like foo itself for address-of
                return EmitAddressOf(u.Operand, code);
            case UnaryExpression u when u.Op == UnaryOperator.Dereference:
                (StringBuilder? ptrCode, string? reg) = EmitExpression(u.Operand);
                code.Append(ptrCode);
                TypeInfo ti = u.Operand.ResolvedType!;
                string irElemTy = TypeToLLVM(ti.Pointee!)!;
                return (code, reg, irElemTy);
            case MemberAccessExpression memberAccess:
                string targetName = memberAccess.Target.Name;
                string memberName = memberAccess.Member.Name;

                if (!TryResolveSlot(targetName, out alloc, out TypeInfo? structInfo))
                    throw new Exception($"Undefined variable '{targetName}'");

                if (!currentScope!.TryLookup(targetName, out SymbolInfo? targetVarInfo, out Scope? _))
                    throw new Exception($"Could not find identifier '{targetName}' in Scope {currentScope.FullName}");

                if (!currentScope!.TryLookup(targetVarInfo!.Type.TypeName, out SymbolInfo? targetInfo, out Scope? definitionScope))
                    throw new Exception($"Could not find type '{targetVarInfo.Type.TypeName}' in Scope {currentScope.FullName}");

                // the scope the target *defines*
                Scope targetScope = definitionScope!.Children[targetVarInfo.Type.TypeName.TrimStart('%')];

                if (!targetScope!.TryLookup(memberName, out SymbolInfo? memberInfo, out Scope? memberScope))
                    throw new Exception($"Could not find member '{memberName}' in '{currentScope.FullName}'");

                if (varTypes.Peek()[targetName].FieldNames?.Count == 0)
                    throw new Exception($"Variable {targetName} does not define any fields");

                int memberIndex = targetInfo!.Parameters!.FindIndex(x => x.Name == memberName);
                if (memberIndex < 0)
                    throw new Exception($"Variable {targetName} does not define a field '{memberName}'");

                string LLVMType = TypeToLLVM(varTypes.Peek()[targetName])!;   // alredy LLVM type
                string memberLLVMType = TypeToLLVM(memberInfo!.Type)!;

                string gep = $"%{NewTempVar()}";

                code.AppendLine(
                    $"  {gep} = getelementptr inbounds {LLVMType}, {LLVMType}* {alloc.ptr}, i32 0, i32 {memberIndex}");

                return (code, gep, memberLLVMType);
            case IndexExpression index:
                // TODO make AddressOf not return the StringBuilder for clarity
                (StringBuilder targetCode, string targetPtr, string arrayTypeLLVM) = EmitAddressOf(index.Target, code);

                (StringBuilder indexCode, string indexReg) = EmitExpression(index.Index);
                code.Append(indexCode);

                string elementType = TypeToLLVM(index.Target.ResolvedType!.ElementType!)!;
                string indexType = TypeToLLVM(index.Index.ResolvedType!)!;

                gep = $"%{NewTempVar()}";

                code.AppendLine(
                    $"  {gep} = getelementptr inbounds {arrayTypeLLVM}, {arrayTypeLLVM}* {targetPtr}, i32 0, i32 {indexReg}");
                return (code, gep, elementType);

            default: throw new Exception($"Unsupported expression type: {target.GetType()}");
        }
    }

    static string NewTempVar() => $"tmp{tmpCounter++}";

    static bool TryResolveSlot(string name, out (string ptr, string? ssa) alloc, out TypeInfo typeInfo)
    {
        // copy the stacks into arrays so that index 0 is the top of the stack
        Dictionary<string, (string ptr, string? ssa)>[] allocArr = allocas.ToArray();
        Dictionary<string, TypeInfo>[] typeArr = varTypes.ToArray();
        for (int i = 0; i < allocArr.Length; i++)
        {
            if (allocArr[i].TryGetValue(name, out alloc))
            {
                // assume varTypes frame has the same key
                typeInfo = typeArr[i][name];
                return true;
            }
        }
        alloc = (null, null)!;
        typeInfo = null!;
        return false;
    }

    static string ToHexFp128(double v)
    {
        if (v == 0.0) return "0xL00";

        // Decompose a double into sign, exponent, and mantissa
        long bits = BitConverter.DoubleToInt64Bits(v);
        bool sign = (bits >> 63) != 0;
        int exp = (int)((bits >> 52) & 0x7FF) - 1023;
        long mant = bits & 0xFFFFFFFFFFFFFL;
        mant |= 1L << 52;

        // Normalize to one hex digit before the point:
        // mantissa is (mant / 2^52), so shift left by 4 bits
        // to extract the first hex digit, then the rest.
        int shift = 52 - 4;
        long hexMant = mant >> shift;           // top 13 bits
        string hex = hexMant.ToString("X");     // hex digits
        // split hex into a.b form (first digit, rest)
        string a = hex[..1];
        string b = hex[1..];

        return (sign ? "-" : "") + $"0xL{a}{b}{exp}";
    }

    static string? TypeToLLVM(TypeInfo type)
    {
        if (type.Pointee != null)
            return $"{TypeToLLVM(type.Pointee)}*";

        // TODO make this work with n-Dimensional arrays
        if (type.ArrayLengths != null && type.ArrayLengths.Count > 0)
            return $"[{type.ArrayLengths[0]} x {TypeToLLVM(type.ElementType!)}]";

        string typeName = type.TypeName;
        if (IsBuiltinType(typeName))
            return typeName switch
            {
                "bool" => "i1",

                "s8" => "i8",
                "s16" => "i16",
                "s32" => "i32",
                "s64" => "i64",
                "s128" => "i128",
                "s256" => "i256",

                "f16" => "half",
                "f32" => "float",
                "f64" => "double",
                "f128" => "fp128",

                _ => throw new NotImplementedException(typeName),
            };
        else    // assume composite type
            return $"%{typeName}";

        throw new Exception($"Unsupported type: '{type.TypeName}'");
    }

    static void EnterScope(string scopeName)
    {
        if (currentScope == null)
            throw new Exception("'currentScope' is null!");

        if (!currentScope.Children.ContainsKey(scopeName))  // verify that we can enter that scope
            Log.Error(8, $"Scope '{scopeName}' does not exist in '{currentScope.FullName}'");

        currentScope = currentScope.Children[scopeName];
    }

    static void EnterScope(Scope scope)
    {
        if (currentScope == null)
            throw new Exception("'currentScope' is null!");

        if (!currentScope.Children.ContainsValue(scope))    // verify that we can enter that scope
            Log.Error(8, $"Scope '{scope.Name}' does not exist in '{currentScope.FullName}'");

        allocas.Push(new Dictionary<string, (string ptr, string? ssa)>());
        varTypes.Push(new Dictionary<string, TypeInfo>());

        currentScope = scope;
    }

    static void ExitScope()
    {
        if (currentScope == null)
            throw new Exception("'currentScope' is null!");

        if (currentScope.Parent == null)
            Log.Error(9, $"Can't exit out of scope '{currentScope.FullName}'");

        if (currentScope.DeclaringNode != null)
        {
            allocas.Pop();
            varTypes.Pop();
        }

        currentScope = currentScope.Parent;
    }
    #endregion
}