using System.Text;

namespace TheiaLang;

public static class IRGenerator
{
    static Dictionary<string, string> allocas;
    private static Dictionary<string, string> varTypes;
    static ulong tmpCounter = 0;
    public static void Emit(ProgramNode program, string pathLl)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("; ModuleID = 'theia_module'");
        sb.AppendLine("declare i32 @puts(i8*, ...)");
        sb.AppendLine("@.theia_print_str = private constant[19 x i8] c\"Hello from Theia!\\0A\\00\"");
        sb.AppendLine();

        foreach (var decl in program.Declarations)
            if (decl is StructDeclaration sd)
                EmitStructType(sd, sb);

        sb.AppendLine();

        foreach (var node in program.Declarations)
            if (node is FunctionDeclaration function)
                EmitFunction(function, sb);

        File.WriteAllText(pathLl, sb.ToString());
    }

    static void EmitStructType(StructDeclaration sd, StringBuilder sb)
    {
        var fieldIr = string.Join(
            ", ",
            sd.Fields.Select(f => TypeToIr(f.Type))
        );

        // emit: %StructName = type { <field1>, <field2>, … }
        sb.AppendLine($"%{sd.Name} = type {{ {fieldIr} }}");
    }

    static void EmitFunction(FunctionDeclaration fn, StringBuilder sb)
    {
        allocas = new Dictionary<string, string>();
        varTypes = new Dictionary<string, string>();

        string returnType = fn.ReturnType switch
        {
            Type.s32 => "i32",
            Type.f32 => "f32",
            Type.Bool => "i1",
            _ => throw new Exception($"Unsupported return type {fn.ReturnType}")
        };

        sb.AppendLine($"define {returnType} @{fn.Name}() {{");
        sb.AppendLine("entry:");

        foreach (IStatement statement in fn.Body.Statements)
        {
            if (statement is not VariableDeclarationStatement variableDeclaration)
                continue;

            string varType = variableDeclaration.Type switch
            {
                Type.s32 => "i32",
                Type.f32 => "double",
                Type.Bool => "i1",
                _ => throw new Exception($"Bad var type {variableDeclaration.Type}")
            };

            varTypes[variableDeclaration.Name] = varType;

            sb.AppendLine($"  %{variableDeclaration.Name} = alloca {varType}");
            allocas[variableDeclaration.Name] = variableDeclaration.Name;

            if (variableDeclaration.Init != null)
            {
                var (exprCode, exprRes) = EmitExpression(variableDeclaration.Init);
                sb.Append(exprCode);
                sb.AppendLine($"  store {varType} {exprRes}, {varType}* %{variableDeclaration.Name}");
                sb.AppendLine();
            }
        }

        foreach (var statement in fn.Body.Statements)
        {
            switch (statement)
            {
                case AssignmentStatement a:
                    {
                        var (code, val) = EmitExpression(a.Expression);
                        sb.Append(code);
                        var ty = InferExpressionType(a.Expression);

                        sb.AppendLine($"  store {ty} {val}, {ty}* %{a.TargetName}");
                    }
                    break;

                case ReturnStatement r:
                    {
                        sb.AppendLine(
                          "call i32 @puts(i8* getelementptr inbounds " +
                          "([19 x i8], [19 x i8]* @.theia_print_str, i32 0, i32 0))");

                        var (code, val) = EmitExpression(r.Expr);
                        sb.Append(code);
                        var ty = InferExpressionType(r.Expr);

                        sb.AppendLine($"  ret {ty} {val}");
                    }
                    break;
            }
        }

        bool hasReturn = fn.Body.Statements.Any(s => s is ReturnStatement);

        if (returnType == "i1")
            sb.AppendLine("  ret i1 0");
        else if (returnType == "double")
            sb.AppendLine("  ret double 0.0");
        else // assume i32
            sb.AppendLine("  ret i32 0");

        sb.AppendLine("}");
        sb.AppendLine();
    }

    static (StringBuilder, string) EmitExpression(IExpression expression)
    {
        StringBuilder code = new StringBuilder();
        switch (expression)
        {
            case UnaryExpression un:
                {
                    // 1) emit operand
                    var (cl, val) = EmitExpression(un.Operand);
                    code.Append(code);

                    // 2) generate a fresh temp
                    var tmp = $"tmp{tmpCounter++}";

                    // 3) pick instruction based on type
                    string type = InferExpressionType(un.Operand);
                    string instr = type switch
                    {
                        "i32" => "sub",   // integer: 0 - x
                        "double" => "fsub",  // float:   0.0 - x
                        _ => throw new NotSupportedException($"Unary - on {type}")
                    };

                    // 4) emit it
                    //    for integers, use literal zero; for double use 0.0
                    var zero = type == "i32" ? "0" : "0.0";
                    code.AppendLine($"  %{tmp} = {instr} {type} {zero}, {val}");
                    return (code, $"%{tmp}");
                }
            case LiteralExpression literalExpression:
                {
                    if (literalExpression.Value is int i) return (code, literalExpression.Lexeme);
                    if (literalExpression.Value is double d) return (code, literalExpression.Lexeme);
                    if (literalExpression.Value is bool b) return (code, b ? "1" : "0");
                    throw new Exception("Unknown literal");
                }
            case IdentifierExpression id:
                {
                    // load from local
                    // we assume naming "%<name>_val" for the loaded value
                    if (!allocas.TryGetValue(id.Name, out var ptrName)
                         || !varTypes.TryGetValue(id.Name, out var varTy))
                        throw new Exception($"Undefined variable '{id.Name}'");

                    var tmp = $"tmp{tmpCounter++}";   // unique per reference
                    var ty = InferExpressionType(expression);
                    code.AppendLine($"  %{tmp} = load {ty}, {ty}* %{id.Name}");
                    return (code, $"%{tmp}");
                }
            case BinaryExpression bin:
                {
                    var (cl, vl) = EmitExpression(bin.Left);
                    var (cr, vr) = EmitExpression(bin.Right);
                    code.Append(cl);
                    code.Append(cr);

                    var ty = InferExpressionType(bin.Left);
                    var tmp = $"tmp{tmpCounter++}";
                    string op;

                    if (ty == "i32") op = bin.Op switch
                    {
                        BinaryOperator.Add => "add",
                        BinaryOperator.Subtract => "sub",
                        BinaryOperator.Multiply => "mul",
                        BinaryOperator.Greater => "icmp sgt",
                        BinaryOperator.Less => "icmp slt",
                        _ => throw new Exception($"Op {bin.Op}")
                    };
                    else if (ty == "double") op = bin.Op switch
                    {
                        BinaryOperator.Add => "fadd",
                        BinaryOperator.Subtract => "fsub",
                        BinaryOperator.Multiply => "fmul",
                        BinaryOperator.Greater => "fcmp ogt",
                        BinaryOperator.Less => "fcmp olt",
                        _ => throw new Exception($"Op {bin.Op}")
                    };

                    else throw new Exception($"Unsupported ty {ty}");

                    code.AppendLine($"  %{tmp} = {op} {ty} {vl}, {vr}");
                    code.AppendLine();

                    return (code, $"%{tmp}");
                }

            default:
                throw new Exception($"Expr not supported: {expression.GetType().Name}");
        }
    }

    static string InferExpressionType(IExpression expr) => expr switch
    {
        LiteralExpression lit when lit.Value is int => "i32",
        LiteralExpression lit when lit.Value is double => "double",
        LiteralExpression lit when lit.Value is bool => "i1",
        IdentifierExpression id when varTypes.ContainsKey(id.Name) => varTypes[id.Name],
        BinaryExpression bin => InferExpressionType(bin.Left),
        _ => throw new Exception("Cannot infer type")
    };

    static string TypeToIr(Type t) => t switch
    {
        Type.s32 => "i32",
        Type.f32 => "double",  // f64 in LLVM
        Type.Bool => "i1",
        // once you have string support:
        // Type.String => "%String",
        _ => throw new NotSupportedException($"No IR for type {t}")
    };
}