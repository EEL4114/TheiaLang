namespace TheiaLang;

using static TypeKind;
using static BuiltinType;
using static BlockTermination;

using System.Text;
using System.Globalization;

public class IRGenerator
{
    // we won't deal with SSA optimisation for now but once we have all basic features done we will
    readonly Stack<Dictionary<string, (string ptr, string? ssa)>> allocas = [];
    readonly Stack<Dictionary<string, TypeInfo>> varTypes = [];
    ulong tmpCounter = 0;
    Scope? currentScope;
    ulong labelCounter = 0;
    string currentBlock;

    readonly record struct LoopIRTarget(
        string ContinueLabel,
        string BreakLabel
    );

    readonly Dictionary<ForStatement, LoopIRTarget> loopTargets = [];

    LayoutCalc Layout;

    bool AutoLog;

    const string INTRINSICS_PATH = "Intrinsics.ll";
    static readonly string INTRINSICS_PATH_REL = Path.Combine(AppContext.BaseDirectory, INTRINSICS_PATH);

    public void Emit(ProgramNode program, Scope globalScope, string pathLl, LayoutCalc layout, bool autoLog = true)
    {
        if (!File.Exists(INTRINSICS_PATH_REL))
            Log.Error(19, "Intrinsics module could not be located");

        string intrinsicsIR = File.ReadAllText(INTRINSICS_PATH_REL);
        StringBuilder sb = new StringBuilder();
        Layout = layout;

        sb.AppendLine($"; Creaded at: {DateTime.Now}'");
        sb.AppendLine("; =============================================================================");

        sb.Append(intrinsicsIR);

        AutoLog = autoLog;
        allocas.Clear();
        varTypes.Clear();
        loopTargets.Clear();

        allocas.Push([]);
        varTypes.Push([]);

        currentScope = globalScope;

        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("; =============================================================================");
        sb.AppendLine("; ModuleID = 'theia_module'");
        // sb.AppendLine("target triple = \"x86_64-pc-windows-msvc19.44.35211\"");

        if (AutoLog)
        {
            sb.AppendLine("@.theia_print_str = private constant[19 x i8] c\"Hello from Theia!\\0A\\00\"");
            sb.AppendLine("declare i32 @printf(ptr, ...)");
            sb.AppendLine("@.print_ret_fmt = private constant [16 x i8] c\"%s returned %d\\0A\\00\"");
        }
        sb.AppendLine();

        foreach (INode decl in program.Nodes)
        {
            if (decl is StructDeclaration sd)
                EmitStructType(sd, sb);
            if(decl is UnionDeclaration ud)
                EmitUnionType(ud, sb); 
        }

        sb.AppendLine();

        foreach (INode node in program.Nodes)
            switch (node)
            {
                case FunctionDeclaration fn:
                    EmitFunction(fn, sb);
                    break;

                case StructDeclaration sd:
                    // for each method, emit it as a real LLVM function
                    EnterScope(sd.Scope!);
                    foreach (FunctionDeclaration function in sd.Functions)
                        EmitFunction(function, sb);
                    
                    ExitScope();
                    break;
            }

        File.WriteAllText(pathLl, sb.ToString());
    }

    void EmitStructType(StructDeclaration sd, StringBuilder sb)
    {
        List<string> fieldLLVMTypes = [];
        foreach (TypeNamePair field in sd.Fields)
            fieldLLVMTypes.Add(TypeToLLVM(field.ResolvedType!)!);

        string fieldIr = string.Join(
            ", ",
            fieldLLVMTypes
        );

        string llvmName = $"%{sd.Name}";

        // emit: %StructName = type { <field1>, <field2>, … }
        sb.AppendLine($"{llvmName} = type {{ {fieldIr} }}");

        varTypes.Peek()[sd.Name] = sd.ResolvedType;
    }

    void EmitUnionType(UnionDeclaration ud, StringBuilder sb)
    {
        TypeLayout unionLayout = Layout.GetLayout(ud.ResolvedType);
        long alignment = unionLayout.Alignment;

        long size = unionLayout.Size;
        long padding = size - alignment;

        string llvmName = $"%{ud.Name}";
        // emit: %UnionName = type { alignment [padding to fill total size] }
        if (padding == 0)
            sb.AppendLine($"{llvmName} = type {{ i{8 * alignment} }}");
        else
            sb.AppendLine($"{llvmName} = type {{ i{8 * alignment}, [{padding} x i8] }}");
    
        varTypes.Peek()[ud.Name] = ud.ResolvedType;
    }

    #region Functions

    void EmitFunction(FunctionDeclaration fn, StringBuilder sb)
    {
        EnterScope(fn.Scope!);

        string returnTypeLLVM = TypeToLLVM(fn.ResolvedType)!;

        List<string> args = [];
        // this is messy but works for now?

        string paramList = "";
        foreach (TypeNamePair parameter in fn.Parameters)
        {
            string parameterLLVMType = TypeToLLVM(parameter.ResolvedType!)!;
            args.Add($"{parameterLLVMType} %{parameter.Identifier}");
        }
        paramList = string.Join(", ", args);
        if (AutoLog)
            sb.AppendLine($"@.fn_{fn.Scope!.Name}_str = private constant [{fn.Scope.Name.Length + 1} x i8] c\"{fn.Scope.Name}\\00\"");

        sb.AppendLine($"define {returnTypeLLVM} @{fn.Scope!.FullName}({paramList}) {{");
        sb.AppendLine("entry:");
        currentBlock = "entry";

        foreach (TypeNamePair parameter in fn.Parameters)
        {
            string LLVMType = TypeToLLVM(parameter.ResolvedType!)!;
            string varName = $"%{NewTempVar()}";

            sb.AppendLine($"  {varName} = alloca {LLVMType}");
            sb.AppendLine($"  store {LLVMType} %{parameter.Identifier}, ptr {varName}");

            allocas.Peek()[parameter.Identifier] = (varName, null);
            varTypes.Peek()[parameter.Identifier] = parameter.ResolvedType!;
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

    // true: terminated (return)
    // false: not terminated
    BlockTermination EmitStatement(IStatement statement, StringBuilder sb)
        => statement switch
        {
            VariableDeclaration v         => EmitVariableDeclaration(v, sb),
            AssignmentStatement a         => EmitAssignmentStatement(a, sb),
            CompoundAssignmentStatement c => EmitCompoundAssignmentStatement(c, sb),
            IfStatement i                 => EmitIfStatement(i, sb),
            ForStatement f                => EmitForStatement(f, sb),
            ReturnStatement r             => EmitReturnStatement(r, sb),
            BreakStatement b              => EmitBreakStatement(b, sb),
            ContinueStatement c           => EmitContinueStatement(c, sb),
            ExpressionStatement e         => EmitExpressionStatement(e, sb),
            _ => throw new Exception($"Unknown Statement: {statement.GetType().Name}"),
        };

    BlockTermination EmitVariableDeclaration(VariableDeclaration variableDeclaration, StringBuilder sb)
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
                  $"  {gep} = getelementptr {LLVMType}, ptr {slot}, i32 0, i32 {i}");

                string LLVMTypeArg = TypeToLLVM(inst.Arguments[i].ResolvedType!)!;

                sb.AppendLine(
                  $"  store {LLVMTypeArg} {argReg}, ptr {gep}");
            }
            sb.AppendLine();
        }
        else if (variableDeclaration.Init != null)
        {
            (StringBuilder initCode, string initReg) = EmitExpression(variableDeclaration.Init);
            sb.Append(initCode);
            sb.AppendLine(
              $"  store {LLVMType} {initReg}, ptr {slot}");
        }
        
        return NotTerminated;
    }

    BlockTermination EmitAssignmentStatement(AssignmentStatement assignment, StringBuilder sb)
    {
        (string ptr, string LLVMType) = EmitAddressOf(assignment.Target, sb);

        (StringBuilder code, string val) = EmitExpression(assignment.Expression);
        sb.Append(code);
        sb.AppendLine($"  store {LLVMType} {val}, ptr {ptr}");

        return NotTerminated;
    }

    BlockTermination EmitCompoundAssignmentStatement(CompoundAssignmentStatement assignment, StringBuilder sb)
    {
        (string ptr, string LLVMType) = EmitAddressOf(assignment.Target, sb);

        BinaryOperator op = assignment.Op switch
        {
            BinaryOperator.PlusEqual  => BinaryOperator.Add,
            BinaryOperator.MinusEqual => BinaryOperator.Subtract,
            BinaryOperator.MultEqual  => BinaryOperator.Multiply,
            BinaryOperator.DivEqual   => BinaryOperator.Divide,
            _ => throw new Exception($"Invalid compound operator: '{assignment.Op}'")
        };

        (StringBuilder code, string val) = EmitExpression(new BinaryExpression(assignment.Target, op, assignment.Expression, SourePosition.None));

        sb.Append(code);
        sb.AppendLine($"  store {LLVMType} {val}, ptr {ptr}");

        return NotTerminated;
    }

    BlockTermination EmitIfStatement(IfStatement ifStatement, StringBuilder sb)
    {
        BlockTermination blockTermination = NotTerminated;
        
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
        currentBlock = thenLabel;
        EnterScope(ifStatement.ThenScope);

        BlockTermination thenTermination = NotTerminated;

        foreach (IStatement statement in ifStatement.ThenBranch)
        {
            BlockTermination bt = EmitStatement(statement, sb);
            if(bt == Terminated)
            {
                thenTermination = Terminated;
                break;  // no need to emit unreachable remaining statements
            }
        }

        ExitScope();
        if(thenTermination == NotTerminated)
            sb.AppendLine($"  br label %{mergeLabel}");


        BlockTermination elseTermination = NotTerminated;

        if (ifStatement.ElseBranch != null)
        {
            EnterScope(ifStatement.ElseScope!);
            sb.AppendLine($"{elseLabel}:");
            currentBlock = elseLabel!;

            foreach (IStatement statement in ifStatement.ElseBranch)
            {
                BlockTermination bt = EmitStatement(statement, sb);
                if(bt == Terminated)
                {
                    elseTermination = Terminated;
                    break;  // no need to emit unreachable remaining statements
                }
            }
            ExitScope();
            if(elseTermination == NotTerminated)
                sb.AppendLine($"  br label %{mergeLabel}");
        }

        if(thenTermination == Terminated && elseTermination == Terminated)
            blockTermination = Terminated;
        
        if(blockTermination == NotTerminated)
        {
            sb.AppendLine($"{mergeLabel}:");
            currentBlock = mergeLabel;   
        }
        return blockTermination;
    }

    BlockTermination EmitForStatement(ForStatement forStatement, StringBuilder sb)
    {
        // Loop init
        EnterScope(forStatement.HeadScope);
        EmitStatement(forStatement.Initialiser!, sb);
        string condLabel = $"for_cond{labelCounter}";
        string bodyLabel = $"for_body{labelCounter}";
        string iterLabel = $"for_iter{labelCounter}";
        string endLabel  = $"for_end{labelCounter}";
        labelCounter++;

        sb.AppendLine($"  br label %{condLabel}");

        // Loop Condition
        sb.AppendLine($"{condLabel}:");
        currentBlock = condLabel;
        (StringBuilder condCode, string condReg) = EmitExpression(forStatement.Condition!);
        sb.Append(condCode);
        sb.AppendLine($"  br i1 {condReg}, label %{bodyLabel}, label %{endLabel}");
        // Loop Body
        sb.AppendLine($"{bodyLabel}:");
        currentBlock = bodyLabel;

        BlockTermination bodyTermination = NotTerminated;

        EnterScope(forStatement.BodyScope);

        loopTargets.Add(
            forStatement,
            new LoopIRTarget(
                ContinueLabel: iterLabel,
                BreakLabel: endLabel
            )
        );

        foreach (IStatement statement in forStatement.Body)
        {
            BlockTermination bt = EmitStatement(statement, sb);
            if (bt == Terminated)
            {
                bodyTermination = Terminated;
                break;
            }
        }

        ExitScope();    // body

        if (bodyTermination == NotTerminated)
            sb.AppendLine($"  br label %{iterLabel}");
        // Loop Iterator
        sb.AppendLine($"{iterLabel}:");
        currentBlock = iterLabel;
        EmitStatement(forStatement.Iterator!, sb);
        sb.AppendLine($"  br label %{condLabel}");
        // Loop End
        sb.AppendLine($"{endLabel}:");
        currentBlock = endLabel;

        ExitScope();    // head
        return NotTerminated;
    }

    BlockTermination EmitReturnStatement(ReturnStatement returnStatement, StringBuilder sb)
    {
        (StringBuilder code, string val) = EmitExpression(returnStatement.Expression);
        sb.Append(code);
        string LLVMType = TypeToLLVM(returnStatement.Expression.ResolvedType!)!;

        if (AutoLog && currentScope!.DeclaringNode is FunctionDeclaration && LLVMType != "void")
            sb.AppendLine(
                $"  call i32 (ptr, ...) @printf(ptr getelementptr inbounds " +
            $"([16 x i8], ptr @.print_ret_fmt, i32 0, i32 0), " +
            $"ptr getelementptr inbounds ([{LLVMType.Length + 1} x i8], ptr @.fn_{currentScope.Name}_str, i32 0, i32 0), " +
            $"{LLVMType} {val})");

        sb.AppendLine($"  ret {LLVMType} {val}");

        return Terminated;
    }

    BlockTermination EmitBreakStatement(BreakStatement breakStatement, StringBuilder sb)
    {
        if(breakStatement.Target == null
        || !loopTargets.TryGetValue(breakStatement.Target, out LoopIRTarget target))
            throw new Exception("BreakStatement has no active resolved loop target");
        sb.AppendLine($"  br label %{target.BreakLabel}");
        return Terminated;
    }

    BlockTermination EmitContinueStatement(ContinueStatement continueStatement, StringBuilder sb)
    {
        if(continueStatement.Target == null
        || !loopTargets.TryGetValue(continueStatement.Target, out LoopIRTarget target))
            throw new Exception("BreakStatement has no active resolved loop target");
        sb.AppendLine($"  br label %{target.ContinueLabel}");
        return Terminated;
    }

    BlockTermination EmitExpressionStatement(ExpressionStatement stmt, StringBuilder sb)
    {
        (StringBuilder code, _) = EmitExpression(stmt.Expression);
        sb.Append(code);  // discard the result, but emit code for side effects

        return NotTerminated;
    }

    #endregion

    #region Expressions
    (StringBuilder, string value) EmitExpression(IExpression expression)
    {
        StringBuilder code = new StringBuilder();
        return expression switch
        {
            UnaryExpression unaryExpression       => EmitUnaryExpression(unaryExpression, code),
            LiteralExpression literal             => EmitLiteralExpression(literal, code),
            IdentifierExpression identifier       => EmitIdentifierExpression(identifier, code),
            BinaryExpression binaryExpression     => EmitBinaryExpression(binaryExpression, code),
            InstantiationExpression instantiation => EmitInstantiationExpression(instantiation, code),
            CallExpression call                   => EmitCallExpression(call, code),
            MemberAccessExpression memberAccess   => EmitMemberAccessExpression(memberAccess, code),
            IndexExpression index                 => EmitIndexExpression(index, code),
            CastExpression cast                   => EmitCastExpression(cast, code),
            _ => throw new Exception($"Unsupported expression: {expression.GetType().Name}"),
        };
    }
    (StringBuilder code, string value) EmitUnaryExpression(UnaryExpression unaryExpression, StringBuilder code)
    {
        TypeInfo typeInfo = unaryExpression.Operand.ResolvedType!;
        switch (unaryExpression.Op)
        {
            case UnaryOperator.Negate:
                (StringBuilder cl, string val) = EmitExpression(unaryExpression.Operand);
                code.Append(cl);

                string tmp = NewTempVar();

                string instr;
                string zero;

                if(Builtins.IsInt(typeInfo.BuiltinType))
                {
                    instr = "sub";
                    zero = "0";   
                }
                else if(Builtins.IsIEE754Float(typeInfo.BuiltinType))
                {
                    instr = "fsub";
                    zero = "0.0";   
                }
                else
                    throw new NotSupportedException($"Unary '-' on {typeInfo}");

                string llvmType = TypeToLLVM(typeInfo)!;

                code.AppendLine($"  %{tmp} = {instr} {llvmType} {zero}, {val}");
                return (code, $"%{tmp}");
            case UnaryOperator.AddressOf:
                (string ptr, _) = EmitAddressOf(unaryExpression.Operand, code);
                return (code, ptr);
            case UnaryOperator.Dereference:
                (cl, val) = EmitExpression(unaryExpression.Operand);
                code.Append(cl);
                TypeInfo pointee = unaryExpression.Operand.ResolvedType!.Pointee!;
                llvmType = TypeToLLVM(pointee)!;
                tmp = NewTempVar();
                code.AppendLine($"  %{tmp} = load {llvmType}, ptr {val}");
                return (code, $"%{tmp}");

            default: throw new NotSupportedException($"{unaryExpression.Op}");
        }
    }

    static (StringBuilder code, string value) EmitLiteralExpression(
        LiteralExpression literal,
        StringBuilder code)
    {
        TypeInfo type = literal.ResolvedType
            ?? throw new InvalidOperationException("Literal has no ResolvedType");

        return type.BuiltinType switch
        {
            BOOL => literal.Value is bool b
                ? (code, b ? "1" : "0")
                : throw new Exception($"Invalid literal: {literal}, {type}"),

            S8 or S16 or S32 or S64 =>
                EmitIntegerLiteral(literal, code),

            F16 or F32 or F64 or F128 =>
                EmitFloatLiteral(literal, code, type.BuiltinType),

            VOID => literal.Value is null
                ? (code, "")
                : throw new Exception($"Invalid literal: {literal}, {type}"),

            _ => throw new Exception($"{literal.Pos} Invalid literal: {literal}, {type} {type.BuiltinType}"),
        };
    }

    static (StringBuilder code, string value) EmitIntegerLiteral(
        LiteralExpression literal,
        StringBuilder code)
    {
        if (literal.Value is not long value)
            throw new Exception($"Invalid literal: {literal}, {literal.ResolvedType}");

        return (code, value.ToString(CultureInfo.InvariantCulture));
    }

    static (StringBuilder code, string value) EmitFloatLiteral(
        LiteralExpression literal,
        StringBuilder code,
        BuiltinType? type)
    {
        double value = literal.Value switch
        {
            long l => l,
            double d => d,
            _ => throw new Exception($"Invalid literal: {literal}, {literal.ResolvedType}")
        };

        return type switch
        {
            F16 or F32 or F64 =>
                (code, FormatLLVMFloat(value, (BuiltinType)literal.ResolvedType!.BuiltinType!)),

            F128 =>
                (code, FP128ToHex(value)),

            _ => throw new Exception($"Invalid literal: {literal}, {literal.ResolvedType}")
        };
    }

    static string FormatLLVMFloat(double value, BuiltinType type)
    {
        string text;
        switch (type)
        {
            case F16:
                    Half half = (Half)value;
                    ushort bits = BitConverter.HalfToUInt16Bits(half);
                    text = $"0xH{bits:X4}";
                    break;

            case F32:
                    text = ((float)value).ToString("R", CultureInfo.InvariantCulture);

                    if (!text.Contains('.') && !text.Contains('E') && !text.Contains('e'))
                        text += ".0";

                    break;

            case F64:
                    text = value.ToString("R", CultureInfo.InvariantCulture);

                    if (!text.Contains('.') && !text.Contains('E') && !text.Contains('e'))
                        text += ".0";

                    break;

            default:
                throw new ArgumentException($"Unsupported float type: {type}");
        }
        return text;
    }

    (StringBuilder code, string value) EmitIdentifierExpression(IdentifierExpression identifier, StringBuilder code)
    {
        if (TryResolveSlot(identifier.Name, out (string ptr, string? ssa) alloc, out TypeInfo? typeInfo))
        {
            string LLVMType = TypeToLLVM(typeInfo)!;

            string tmp = $"tmp{tmpCounter++}";
            code.AppendLine($"  %{tmp} = load {LLVMType}, ptr {alloc.ptr}");
            return (code, $"%{tmp}");
        }

        if (currentScope!.Parent?.DeclaringNode is StructDeclaration sd)
        {
            int index = sd.Fields.FindIndex(f => f.Identifier == identifier.Name);
            if (index >= 0)
            {
                string LLVMType = TypeToLLVM(sd.Fields[index].ResolvedType!)!;

                string tmp = $"%{NewTempVar()}";
                code.AppendLine($"  {tmp} = load %{sd.ResolvedType.TypeName}, ptr %this");
                string tmp2 =  $"%{NewTempVar()}";
                code.AppendLine(
                    $"  {tmp2} = extractvalue %{sd.ResolvedType.TypeName} {tmp}, {index}");
                return (code, tmp2);
            }
        }

        throw new Exception($"Undefined variable or field '{identifier.Name}'");
    }

    (StringBuilder code, string value) EmitBinaryExpression(BinaryExpression binaryExpression, StringBuilder code)
    {
        if (binaryExpression.Op == BinaryOperator.AND ||
            binaryExpression.Op == BinaryOperator.OR)
        {
            return EmitShortCircuitBinaryExpression(binaryExpression);
        }

        (StringBuilder cl, string vl) = EmitExpression(binaryExpression.Left);
        (StringBuilder cr, string vr) = EmitExpression(binaryExpression.Right);
        code.Append(cl);
        code.Append(cr);

        TypeInfo typeInfo;

        if(binaryExpression.ResolvedType == binaryExpression.Right.ResolvedType!
         || binaryExpression.ResolvedType == binaryExpression.Left.ResolvedType!)
            typeInfo = binaryExpression.ResolvedType;
        else
            typeInfo = binaryExpression.Left.ResolvedType!;

        string tmp = $"tmp{tmpCounter++}";
        string op;

        if (typeInfo.BuiltinType == null)
        {
            throw new Exception($"Unsupported type '{typeInfo.TypeName}'");
        }
        else if (typeInfo.BuiltinType == BOOL) op = binaryExpression.Op switch
        {
            BinaryOperator.EqualEqual => "icmp eq",
            BinaryOperator.NotEqual   => "icmp ne",
            _ => throw new Exception($"Unsupported operation '{binaryExpression.Op}' for type 'bool'")
        };
        else if (Builtins.IsSignedInt(typeInfo.BuiltinType)) op = binaryExpression.Op switch
        {
            BinaryOperator.Add        => "add",
            BinaryOperator.Subtract   => "sub",
            BinaryOperator.Multiply   => "mul",
            BinaryOperator.Divide     => "sdiv",
            BinaryOperator.Greater    => "icmp sgt",
            BinaryOperator.Less       => "icmp slt",
            BinaryOperator.EqualEqual => "icmp eq",
            BinaryOperator.NotEqual   => "icmp ne",
            _ => throw new Exception($"Unsupported operation '{binaryExpression.Op}' for type {typeInfo.TypeName}")
        };
        else if (Builtins.IsIEE754Float(typeInfo.BuiltinType)) op = binaryExpression.Op switch
        {
            BinaryOperator.Add        => "fadd",
            BinaryOperator.Subtract   => "fsub",
            BinaryOperator.Multiply   => "fmul",
            BinaryOperator.Divide     => "fdiv",
            BinaryOperator.Greater    => "fcmp ogt",
            BinaryOperator.Less       => "fcmp olt",
            BinaryOperator.EqualEqual => "fcmp oeq",
            BinaryOperator.NotEqual   => "fcmp one",
            _ => throw new Exception($"Op {binaryExpression.Op}")
        };
        else throw new Exception($"Unsupported operation '{binaryExpression.Op}' for type {typeInfo.TypeName}");

        string LLVMType = TypeToLLVM(typeInfo)!;

        code.AppendLine($"  %{tmp} = {op} {LLVMType} {vl}, {vr}");
        code.AppendLine();

        return (code, $"%{tmp}");
    }

    (StringBuilder Code, string Result) EmitShortCircuitBinaryExpression(BinaryExpression binaryExpression)
    {
        StringBuilder code = new();

        (StringBuilder leftCode, string leftReg) =
            EmitExpression(binaryExpression.Left);

        code.Append(leftCode);

        ulong id = labelCounter++;

        string rhsLabel = $"logic_rhs_{id}";
        string endLabel = $"logic_end_{id}";

        string leftBlockLabel = currentBlock;

        if (binaryExpression.Op == BinaryOperator.AND)
        {
            code.AppendLine(
                $"  br i1 {leftReg}, label %{rhsLabel}, label %{endLabel}");
        }
        else
        {
            code.AppendLine(
                $"  br i1 {leftReg}, label %{endLabel}, label %{rhsLabel}");
        }

        code.AppendLine($"{rhsLabel}:");
        currentBlock = rhsLabel;

        (StringBuilder rightCode, string rightReg) =
            EmitExpression(binaryExpression.Right);

        code.Append(rightCode);

        string rightBlockLabel = currentBlock;

        code.AppendLine($"  br label %{endLabel}");

        code.AppendLine($"{endLabel}:");
        currentBlock = endLabel;

        string result = $"%{NewTempVar()}";

        if (binaryExpression.Op == BinaryOperator.AND)
        {
            code.AppendLine(
                $"  {result} = phi i1 [ 0, %{leftBlockLabel} ], [ {rightReg}, %{rightBlockLabel} ]");
        }
        else
        {
            code.AppendLine(
                $"  {result} = phi i1 [ 1, %{leftBlockLabel} ], [ {rightReg}, %{rightBlockLabel} ]");
        }

        return (code, result);
    }

    (StringBuilder code, string value) EmitInstantiationExpression(InstantiationExpression instantiation, StringBuilder code)
    {
        string irType = TypeToLLVM(instantiation.ResolvedType!)!;
        string ptrName = $"%{NewTempVar()}";
        code.AppendLine($"  {ptrName} = alloca {irType}");

        for (int i = 0; i < instantiation.Arguments.Count; i++)
        {
            (StringBuilder argCode, string argumentValue) = EmitExpression(instantiation.Arguments[i]);
            code.Append(argCode);

            string gep = $"%{NewTempVar()}";
            code.AppendLine(
                $"  {gep} = getelementptr {irType}, ptr {ptrName}, i32 0, i32 {i}");

            string llvmType = TypeToLLVM(instantiation.Arguments[i].ResolvedType!)!;

            code.AppendLine($"  store {llvmType} {argumentValue}, ptr {gep}");
        }

        string valueName = $"%{NewTempVar()}";
        code.AppendLine($"  {valueName} = load {irType}, ptr {ptrName}");

        return (code, valueName);
    }

    (StringBuilder code, string value) EmitCallExpression(CallExpression call, StringBuilder code)
    {
        if (call.Target is IdentifierExpression id)
        {
            string retTy = TypeToLLVM(call.ResolvedType!)!;

            List<string> argumentList = [];

            for (int i = 0; i < call.Arguments.Count; i++)
            {
                IExpression argument = call.Arguments[i];

                (StringBuilder argCode, string argReg) = EmitExpression(argument);
                code.Append(argCode);

                // infer the LLVM type of the argument
                TypeInfo actualType = argument.ResolvedType!;

                string actualLLVMType = TypeToLLVM(actualType)!;
                string expectedLLVMType = TypeToLLVM(call.Arguments[i].ResolvedType!)!;

                if (actualType.TypeName != call.Arguments[i].ResolvedType!.TypeName && AutoLog)
                    Log.Error(12,
                        $"Type mismatch in call to '{"call.CalleeName"}.{call.Arguments[i]}' " +
                        $"expected {call.Arguments[i].ResolvedType!.TypeName}, got {actualType.TypeName}");

                argumentList.Add($"{actualLLVMType} {argReg}");
            }

            string tmp = $"%{NewTempVar()}";

            string calleeName = ((IdentifierExpression)call.Target).Name;

            calleeName = string.IsNullOrEmpty(call.Scope!.FullName) ? calleeName : $"{call.Scope!.FullName}.{calleeName}";

            if (retTy != "void")
                code.AppendLine($"  {tmp} = call {retTy} @{calleeName}({string.Join(", ", argumentList)})");
            else
                code.AppendLine($"  call {retTy} @{calleeName}({string.Join(", ", argumentList)})");
            return (code, tmp);
        }

        Log.Info("ZZZ");
        return (null, null);
    }

    (StringBuilder code, string value) EmitCastExpression(CastExpression cast, StringBuilder code)
    {
        (StringBuilder argCode, string argReg) = EmitExpression(cast.Target);
        code.Append(argCode);

        if (cast.CastKind == CastOp.NoOp)
            return (code, argReg);

        string sourceLLVMType = TypeToLLVM(cast.Target.ResolvedType!)!;
        string targetLLVMType = TypeToLLVM(cast.ResolvedType!)!;

        string tmp = $"%{NewTempVar()}";

        switch (cast.CastKind)
        {
            // int <-> int
            case CastOp.SExt:       code.AppendLine($"  {tmp} = sext {sourceLLVMType} {argReg} to {targetLLVMType}"); break;
            case CastOp.Trunc:      code.AppendLine($"  {tmp} = trunc {sourceLLVMType} {argReg} to {targetLLVMType}"); break;

            // float <-> float
            case CastOp.FPExt:      code.AppendLine($"  {tmp} = fpext {sourceLLVMType} {argReg} to {targetLLVMType}"); break;
            case CastOp.FPTrunc:    code.AppendLine($"  {tmp} = fptrunc {sourceLLVMType} {argReg} to {targetLLVMType}"); break;

            // int <-> float
            case CastOp.SIToFP:     code.AppendLine($"  {tmp} = sitofp {sourceLLVMType} {argReg} to {targetLLVMType}"); break;
            case CastOp.FPToSI:     code.AppendLine($"  {tmp} = fptosi {sourceLLVMType} {argReg} to {targetLLVMType}"); break;

            // bools
            case CastOp.BoolToInt:  code.AppendLine($"  {tmp} = zext i1 {argReg} to {targetLLVMType}"); break;
            case CastOp.IntToBool:  code.AppendLine($"  {tmp} = icmp ne {sourceLLVMType} {argReg}, 0"); break;
            case CastOp.BoolToFP:   code.AppendLine($"  {tmp} = uitofp i1 {argReg} to {targetLLVMType}"); break;
            case CastOp.FPToBool:   code.AppendLine($"  {tmp} = fcmp une {sourceLLVMType} {argReg}, 0.0"); break;

            // ptr <-> int / bool
            case CastOp.PtrToInt:   code.AppendLine($"  {tmp} = ptrtoint ptr {argReg} to {targetLLVMType}"); break;
            case CastOp.IntToPtr:   code.AppendLine($"  {tmp} = inttoptr {sourceLLVMType} {argReg} to ptr"); break;
            case CastOp.PtrToBool:  code.AppendLine($"  {tmp} = icmp ne ptr {argReg}, null"); break;
            default: throw new Exception($"Unhandled cast op {cast.CastKind}");
        }

        return (code, tmp);
    }

    (StringBuilder code, string value) EmitMemberAccessExpression(MemberAccessExpression memberAccess, StringBuilder code)
    {
        (string ptr, string llvmType) = EmitAddressOf(memberAccess, code);

        string tmp = $"%{NewTempVar()}";
        code.AppendLine($"  {tmp} = load {llvmType}, ptr {ptr}");
        return (code, tmp);
    }

    (StringBuilder code, string value) EmitIndexExpression(IndexExpression index, StringBuilder code)
    {
        (string ptr, string llvmType) = EmitAddressOf(index, code);

        string tmp = $"%{NewTempVar()}";
        code.AppendLine($"  {tmp} = load {llvmType}, ptr {ptr}");
        return (code, tmp);
    }

    #endregion

    #region  Helpers

    (string ptr, string llvmType) EmitAddressOf(IExpression target, StringBuilder code)
    {
        switch (target)
        {
            case IdentifierExpression identifier:
                if (!TryResolveSlot(identifier.Name, out (string ptr, string? ssa) alloc, out TypeInfo? typeInfo))
                    throw new Exception($"Undefined Identifier '{identifier.Name}'");
                string ptr = alloc.ptr;
                string llvmType = TypeToLLVM(typeInfo)!;
                return (ptr, llvmType);
            case UnaryExpression u when u.Op == UnaryOperator.AddressOf:
                // treat @foo exactly like foo itself for address-of
                return EmitAddressOf(u.Operand, code);
            case UnaryExpression u when u.Op == UnaryOperator.Dereference:
                (StringBuilder? ptrCode, string? reg) = EmitExpression(u.Operand);
                code.Append(ptrCode);
                TypeInfo ti = u.Operand.ResolvedType!;
                string irElemTy = TypeToLLVM(ti.Pointee!)!;
                return (reg, irElemTy);
            case MemberAccessExpression memberAccess:
                (string targetAddr, string targetLLVMType) = EmitAddressOf(memberAccess.Target, code);

                TypeInfo targetType = memberAccess.Target.ResolvedType!;
                TypeInfo memberType = memberAccess.Member.ResolvedType!;
                string memberLLVMType = TypeToLLVM(memberType)!;

                if(targetType.TypeKind == Union)
                    return (targetAddr, memberLLVMType);

                if(targetType.TypeKind == Struct)
                {
                    int memberIndex = targetType.FieldNames!.IndexOf(memberAccess.Member.Name);
                    if (memberIndex < 0)
                        throw new Exception($"Type '{targetType.TypeName}' has no member '{memberAccess.Member.Name}'");

                    string temp = $"%{NewTempVar()}";

                    code.AppendLine(
                        $"  {temp} = getelementptr inbounds {targetLLVMType}, ptr {targetAddr}, i32 0, i32 {memberIndex}");
                    
                    return(temp, memberLLVMType);
                }

                throw new Exception(
                    $"Cannot access member '{memberAccess.Member.Name}' of type '{targetType.TypeName}'");
            case IndexExpression index:
                (string targetPtr, string arrayTypeLLVM) = EmitAddressOf(index.Target, code);

                (StringBuilder indexCode, string indexReg) = EmitExpression(index.Index);
                code.Append(indexCode);

                string elementType = TypeToLLVM(index.Target.ResolvedType!.ElementType!)!;
                string indexType = TypeToLLVM(index.Index.ResolvedType!)!;

                string gep = $"%{NewTempVar()}";

                code.AppendLine(
                    $"  {gep} = getelementptr inbounds {arrayTypeLLVM}, ptr {targetPtr}, i32 0, i32 {indexReg}");
                return (gep, elementType);

            default: throw new Exception($"Unsupported expression type: {target.GetType()}");
        }
    }

    string NewTempVar() => $"tmp{tmpCounter++}";

    bool TryResolveSlot(string name, out (string ptr, string? ssa) alloc, out TypeInfo typeInfo)
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

    static string FP128ToHex(double v)
    {
        if (v == 0.0) return "0xL00";

        // Decompose a double into sign, exponent, and mantissa
        long bits = BitConverter.DoubleToInt64Bits(v);
        bool sign = (bits >> 63) != 0;
        int  exp  = (int)((bits >> 52) & 0x7FF) - 1023;
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
        if (type.BuiltinType == VOIDPTR)
            return "ptr";

        // TODO make this work with n-Dimensional arrays
        if (type.ArrayLength != null && type.ArrayLength > 0)
            return $"[{type.ArrayLength} x {TypeToLLVM(type.ElementType!)}]";

        string typeName = type.TypeName;
        if (type.BuiltinType != null)
            return type.BuiltinType switch
            {
                BOOL => "i1",

                S8   => "i8",
                S16  => "i16",
                S32  => "i32",
                S64  => "i64",
                S128 => "i128",
                S256 => "i256",

                F16  => "half",
                F32  => "float",
                F64  => "double",
                F128 => "fp128",

                VOID => "void",

                _ => throw new NotImplementedException(typeName),
            };
        else    // assume composite type
            return $"%{typeName}";

        throw new Exception($"Unsupported type: '{type.TypeName}'");
    }

    void EnterScope(Scope scope)
    {
        allocas.Push([]);
        varTypes.Push([]);

        currentScope = scope;
    }

    void ExitScope()
    {
#if DEBUG   // this can only fail if there is a bug in the IRGen itself
        if (currentScope == null)
            throw new Exception("'currentScope' is null!");
#endif
        allocas.Pop();
        varTypes.Pop();

        currentScope = currentScope.Exit();
    }

    #endregion

}

enum BlockTermination
{
    NotTerminated = 0,
    Terminated    = 1,
}