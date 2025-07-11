using TheiaLang;

static class AstPrinter
{
    public static void Print(ProgramNode program, TextWriter w)
        => PrintProgram(program, w, 0);

    private static void PrintProgram(ProgramNode p, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}Program");
        foreach (var node in p.Declarations)
            if (node is FunctionDeclaration function)
                PrintFunction(function, w, indent + 2);
    }

    private static void PrintFunction(FunctionDeclaration fn, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}FunctionDeclaration: {fn.ReturnType} {fn.Name}()");
        PrintBlock(fn.Body, w, indent + 2);
    }

    private static void PrintBlock(BlockSatement block, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}BlockSatement");
        foreach (var stmt in block.Statements)
            PrintStatement(stmt, w, indent + 2);
    }

    private static void PrintStatement(IStatement stmt, TextWriter w, int indent)
    {
        switch (stmt)
        {
            case VariableDeclarationStatement vd:
                w.WriteLine($"{Indent(indent)}VariableDeclaration: {vd.Type} {vd.Name}" +
                            (vd.Init is not null ? " =" : ""));
                if (vd.Init is not null)
                    PrintExpression(vd.Init, w, indent + 2);
                break;

            case AssignmentStatement a:
                w.WriteLine($"{Indent(indent)}Assign: {a.TargetName} =");
                PrintExpression(a.Expression, w, indent + 2);
                break;

            case ReturnStatement r:
                w.WriteLine($"{Indent(indent)}Return");
                PrintExpression(r.Expr, w, indent + 2);
                break;

            default:
                w.WriteLine($"{Indent(indent)}<unknown statement {stmt.GetType().Name}>");
                break;
        }
    }

    private static void PrintExpression(IExpression expr, TextWriter w, int indent)
    {
        switch (expr)
        {
            case LiteralExpression lit:
                w.WriteLine($"{Indent(indent)}Literal: {lit.Lexeme}");
                break;

            case IdentifierExpression id:
                w.WriteLine($"{Indent(indent)}Identifier: {id.Name}");
                break;

            case BinaryExpression bin:
                w.WriteLine($"{Indent(indent)}BinaryExpression: {bin.Op}");
                PrintExpression(bin.Left, w, indent + 2);
                PrintExpression(bin.Right, w, indent + 2);
                break;

            default:
                w.WriteLine($"{Indent(indent)}<unknown expression {expr.GetType().Name}>");
                break;
        }
    }

    private static string Indent(int n) => new string(' ', n);
}
