using System.Text;

namespace TheiaLang;

public static class IRGenerator
{
    static readonly Stack<Dictionary<string, string>> allocas = new();
    static readonly Stack<Dictionary<string, string>> varTypes = new();
    static ulong tmpCounter = 0;
    static Scope? currentScope;
    public static void Emit(ProgramNode program, Scope globalScope, string pathLl)
    {
        allocas.Clear();
        varTypes.Clear();

        allocas.Push(new Dictionary<string, string>());
        varTypes.Push(new Dictionary<string, string>());

        currentScope = globalScope;
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("; ModuleID = 'theia_module'");
        sb.AppendLine("target triple = \"x86_64-pc-windows-msvc19.44.35211\"");
        sb.AppendLine("declare i32 @puts(i8*, ...)");
        sb.AppendLine("@.theia_print_str = private constant[19 x i8] c\"Hello from Theia!\\0A\\00\"");
        sb.AppendLine();

        foreach (INode decl in program.Declarations)
            if (decl is StructDeclaration sd)
                EmitStructType(sd, sb);

        sb.AppendLine();

        foreach (INode node in program.Declarations)
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
        string fieldIr = string.Join(
            ", ",
            sd.Fields.Select(f => TypeToIR(f.Type))
        );

        // emit: %StructName = type { <field1>, <field2>, … }
        sb.AppendLine($"%{sd.Name} = type {{ {fieldIr} }}");
    }

    #region Functions
    static void EmitFunction(FunctionDeclaration fn, StringBuilder sb)
    {
        EnterScope(fn.Scope!);

        string returnType = TypeToIR(fn.ReturnType, $"Unsupported return type '{fn.ReturnType}'");

        List<string> args = new List<string>();
        if (fn.Scope!.Parent?.DeclaringNode is StructDeclaration parentStruct)
        {
            string structPtrType = $"%{parentStruct.Name}*";
            args.Add($"{structPtrType} %this");
        }

        // this is messy but works for now?

        string paramList = "";
        foreach (TypeNamePair parameter in fn.Parameters)
        {
            var llvmTy = TypeToIR(parameter.Type);
            args.Add($"{llvmTy} %{parameter.Name}");
        }
        paramList = string.Join(", ", args);

        sb.AppendLine($"define {returnType} @{fn.Name}({paramList}) {{");
        sb.AppendLine("entry:");

        foreach (TypeNamePair parameter in fn.Parameters)
        {
            var llvmTy = TypeToIR(parameter.Type);
            string varName = $"%{NewTempVar()}";

            sb.AppendLine($"  {varName} = alloca {llvmTy}");
            sb.AppendLine($"  store {llvmTy} %{parameter.Name}, {llvmTy}* {varName}");

            allocas.Peek()[parameter.Name] = varName;
            varTypes.Peek()[parameter.Name] = llvmTy;
            args.Add($"{llvmTy} %{parameter.Name}");
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
            case VariableDeclaration variableDeclaration:
                {
                    var irType = TypeToIR(variableDeclaration.Type,
                                          $"Unknown type '{variableDeclaration.Type}' in variable declaration");

                    var slot = $"%{variableDeclaration.Name}";
                    sb.AppendLine($"  {slot} = alloca {irType}");
                    allocas.Peek()[variableDeclaration.Name] = slot;
                    varTypes.Peek()[variableDeclaration.Name] = irType;

                    if (variableDeclaration.Init is InstantiationExpression inst)
                    {
                        for (int i = 0; i < inst.Arguments.Count; i++)
                        {
                            // evaluate the argument
                            (StringBuilder argCode, string argReg) = EmitExpression(inst.Arguments[i]);
                            sb.Append(argCode);

                            // get the pointer to field `i` of our *variable* slot
                            string gep = $"%{NewTempVar()}";
                            sb.AppendLine(
                              $"  {gep} = getelementptr {irType}, {irType}* {slot}, i32 0, i32 {i}");

                            // store the argument into that field
                            string? argTy = InferExpressionType(inst.Arguments[i]);
                            sb.AppendLine(
                              $"  store {argTy} {argReg}, {argTy}* {gep}");
                        }
                        sb.AppendLine();
                    }
                    else if (variableDeclaration.Init != null)
                    {
                        (StringBuilder initCode, string initReg) = EmitExpression(variableDeclaration.Init);
                        sb.Append(initCode);
                        sb.AppendLine(
                          $"  store {irType} {initReg}, {irType}* {slot}");
                    }
                }
                break;
            case AssignmentStatement assignment:
                {
                    if (!TryResolveSlot(assignment.TargetName, out string? ptr, out string? type))
                        throw new Exception($"Undefined Identifier '{assignment.TargetName}' in AssignmentStatement: \n" +
                        $"{assignment.TargetName} = {InferExpressionType(assignment.Expression)} {assignment.Expression}");

                    (StringBuilder code, string val) = EmitExpression(assignment.Expression);
                    sb.AppendLine($"  store {type} {val}, {type}* {ptr}");
                    sb.Append(code);
                }
                break;

            case ReturnStatement @return:
                {
                    sb.AppendLine(
                      "  call i32 @puts(i8* getelementptr inbounds " +
                      "([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))");

                    (StringBuilder code, string val) = EmitExpression(@return.Expr);
                    sb.Append(code);
                    string? ty = InferExpressionType(@return.Expr);

                    sb.AppendLine($"  ret {ty} {val}");
                }
                break;
        }
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
            _ => throw new Exception($"Unsupported expression: {expression.GetType().Name}"),
        };
    }
    static (StringBuilder code, string name) EmitUnaryExpression(UnaryExpression unaryExpression, StringBuilder code)
    {
        (StringBuilder cl, string val) = EmitExpression(unaryExpression.Operand);
        code.Append(code);

        string tmp = NewTempVar();

        string? type = InferExpressionType(unaryExpression.Operand);
        string instr = type switch
        {
            "i32" => "sub",
            "double" => "fsub",
            _ => throw new NotSupportedException($"Unary - on {type}")
        };

        string zero = type == "i32" ? "0" : "0.0";
        code.AppendLine($"  %{tmp} = {instr} {type} {zero}, {val}");
        return (code, $"%{tmp}");
    }

    static (StringBuilder code, string name) EmitLiteralExpression(LiteralExpression literalExpression, StringBuilder code)
    {
        if (literalExpression.Value is int i) return (code, literalExpression.Lexeme);
        if (literalExpression.Value is double d) return (code, literalExpression.Lexeme);
        if (literalExpression.Value is bool b) return (code, b ? "1" : "0");
        throw new Exception("Unknown literal");
    }

    static (StringBuilder code, string name) EmitIdentifierExpression(IdentifierExpression identifier, StringBuilder code)
    {
        if (TryResolveSlot(identifier.Name, out var ptr, out var type))
        {
            string tmp = $"tmp{tmpCounter++}";
            code.AppendLine($"  %{tmp} = load {type}, {type}* %{identifier.Name}");
            return (code, $"%{tmp}");
        }

        if (currentScope!.Parent?.DeclaringNode is StructDeclaration sd)
        {
            int index = sd.Fields.FindIndex(f => f.Name == identifier.Name);
            if (index >= 0)
            {
                string irType = TypeToIR(sd.Fields[index].Type);
                string gep = NewTempVar();
                code.AppendLine(
                    $"  {gep} = getelementptr %struct.{sd.Name}, %struct.{sd.Name}* %this, i32 0, i32 {index}");
                string tempIdentifier = NewTempVar();
                code.AppendLine($"  {tempIdentifier} = load {irType}, {irType}* {gep}");
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

        string? ty = InferExpressionType(binaryExpression.Left);
        string tmp = $"tmp{tmpCounter++}";
        string op;

        if (ty == "i32") op = binaryExpression.Op switch
        {
            BinaryOperator.Add => "add",
            BinaryOperator.Subtract => "sub",
            BinaryOperator.Multiply => "mul",
            BinaryOperator.Greater => "icmp sgt",
            BinaryOperator.Less => "icmp slt",
            _ => throw new Exception($"Op {binaryExpression.Op}")
        };
        else if (ty == "double") op = binaryExpression.Op switch
        {
            BinaryOperator.Add => "fadd",
            BinaryOperator.Subtract => "fsub",
            BinaryOperator.Multiply => "fmul",
            BinaryOperator.Greater => "fcmp ogt",
            BinaryOperator.Less => "fcmp olt",
            _ => throw new Exception($"Op {binaryExpression.Op}")
        };

        else throw new Exception($"Unsupported ty {ty}");

        code.AppendLine($"  %{tmp} = {op} {ty} {vl}, {vr}");
        code.AppendLine();

        return (code, $"%{tmp}");
    }

    static (StringBuilder code, string name) EmitInstantiationExpression(InstantiationExpression instantiation, StringBuilder code)
    {
        string irType = TypeToIR(instantiation.TypeName,
                              $"Unknown type '{instantiation.TypeName}' in instantiation");
        string ptrName = $"%{NewTempVar()}";
        code.AppendLine($"  {ptrName} = alloca {irType}");

        for (int i = 0; i < instantiation.Arguments.Count; i++)
        {
            (StringBuilder argCode, string argReg) = EmitExpression(instantiation.Arguments[i]);
            code.Append(argCode);

            string gep = $"%{NewTempVar()}";
            code.AppendLine(
                $"  {gep} = getelementptr {irType}, {irType}* {ptrName}, i32 0, i32 {i}");

            var argTy = InferExpressionType(instantiation.Arguments[i]);
            code.AppendLine($"  store {argTy} {argReg}, {argTy}* {gep}");
        }
        return (code, ptrName);
    }

    static (StringBuilder code, string name) EmitCallExpression(CallExpression call, StringBuilder code)
    {
        if (!currentScope!.TryLookup(call.CalleeName, out IDeclaration? callee))
            throw new Exception($"Undefined identifier '{call.CalleeName}' in {currentScope.FullName}");

        if (callee is not FunctionDeclaration fnDecl)
            throw new Exception($"'{call.CalleeName}' is not a function in scope '{currentScope.FullName}'");

        if (call.Arguments.Count != fnDecl.Parameters.Count)
            Log.Error(11,  // pick an unused code
                $"Function '{call.CalleeName}' expects {fnDecl.Parameters.Count} arguments, " +
                $"but got {call.Arguments.Count}");
        string retTy = TypeToIR(fnDecl.ReturnType);

        List<string> argumentList = new List<string>();

        for (int i = 0; i < call.Arguments.Count; i++)
        {
            IExpression argument = call.Arguments[i];

            (StringBuilder argCode, string argReg) = EmitExpression(argument);
            code.Append(argCode);

            // infer the LLVM type of the argument
            string? actualType = InferExpressionType(argument);
            string expectedType = TypeToIR(fnDecl.Parameters[i].Type);

            if (actualType != expectedType)
                Log.Error(12,
                    $"Type mismatch in call to '{call.CalleeName}': parameter '{fnDecl.Parameters[i].Name}' " +
                    $"expected {expectedType}, got {actualType}");

            argumentList.Add($"{expectedType} {argReg}");
        }

        string tmp = $"%{NewTempVar()}";
        code.AppendLine(
            $"  {tmp} = call {retTy} @{fnDecl.Name}({string.Join(", ", argumentList)})");
        return (code, tmp);
    }


    #endregion

    #region  Helpers

    static string NewTempVar() => $"tmp{tmpCounter++}";

    static bool TryResolveSlot(string name, out string? ptr, out string type)
    {
        // copy the stacks into arrays so that index 0 is the top of the stack
        var allocArr = allocas.ToArray();
        var typeArr = varTypes.ToArray();
        for (int i = 0; i < allocArr.Length; i++)
        {
            if (allocArr[i].TryGetValue(name, out ptr))
            {
                // assume varTypes frame has the same key
                type = typeArr[i][name];
                return true;
            }
        }
        ptr = type = null!;
        return false;
    }

    static bool TryResolveType(string name, out string? llvmType)
    {
        // _varTypesStack is a Stack<Dictionary<string,string>>
        foreach (var frame in varTypes)
        {
            if (frame.TryGetValue(name, out llvmType))
                return true;
        }
        llvmType = null!;
        return false;
    }

    static string? InferExpressionType(IExpression expr) => expr switch
    {
        LiteralExpression lit when lit.Value is int => "i32",
        LiteralExpression lit when lit.Value is double => "double",
        LiteralExpression lit when lit.Value is bool => "i1",
        IdentifierExpression id when TryResolveType(id.Name, out string? type) => type,
        BinaryExpression bin => InferExpressionType(bin.Left),
        _ => throw new Exception("Cannot infer type")
    };

    static string TypeToIR(string t, string errorMessage = "") => t switch
    {
        "s32" => "i32",
        "f32" => "double",  // f64 in LLVM
        "bool" => "i1",

        _ when currentScope!.TryLookup(t, out var decl)
            && decl is StructDeclaration
        => $"%{t}",


        _ => errorMessage == null ?
            throw new NotSupportedException($"No IR for type {t}")
            : throw new Exception(errorMessage)
    };

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

        if (scope.DeclaringNode is FunctionDeclaration)
        {
            allocas.Push(new Dictionary<string, string>());
            varTypes.Push(new Dictionary<string, string>());
        }

        currentScope = scope;
    }

    static void ExitScope()
    {
        if (currentScope == null)
            throw new Exception("'currentScope' is null!");

        if (currentScope.Parent == null)
            Log.Error(9, $"Can't exit out of scope '{currentScope.FullName}'");

        if (currentScope.DeclaringNode is FunctionDeclaration)
        {
            allocas.Pop();
            varTypes.Pop();
        }

        currentScope = currentScope.Parent;
    }
    #endregion
}