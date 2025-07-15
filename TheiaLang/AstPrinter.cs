using TheiaLang;

static class AstPrinter
{
    const int tab = 4;
    public static void Print(ProgramNode program, TextWriter w)
        => PrintProgram(program, w, 0);

    private static void PrintProgram(ProgramNode p, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}Program");
        foreach (INode node in p.Declarations)
        {
            if (node is FunctionDeclaration function)
                PrintFunction(function, w, indent);
            if (node is StructDeclaration structDeclaration)
                PrintStruct(structDeclaration, w, indent);
        }
    }

    private static void PrintStruct(StructDeclaration structDeclaration, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}StructDeclaration: {structDeclaration.Name}");
        if (structDeclaration.Functions.Count == 0)
            w.WriteLine();

        foreach (FunctionDeclaration function in structDeclaration.Functions)
            PrintFunction(function, w, indent + tab);
    }

    private static void PrintFunction(FunctionDeclaration fn, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}FunctionDeclaration: {fn.ReturnType} {fn.Name}()");
        PrintBlock(fn.Statements, w, indent + tab);
    }

    private static void PrintBlock(List<IStatement> block, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}BlockSatement");
        foreach (IStatement stmt in block)
            PrintStatement(stmt, w, indent + tab);
        w.WriteLine();
    }

    private static void PrintStatement(IStatement stmt, TextWriter w, int indent)
    {
        switch (stmt)
        {
            case VariableDeclaration vd:
                w.WriteLine($"{Indent(indent)}VariableDeclaration: {vd.Type} {vd.Name}" +
                            (vd.Init is not null ? " =" : ""));
                if (vd.Init is not null)
                    PrintExpression(vd.Init, w, indent + tab);
                break;

            case AssignmentStatement a:
                w.WriteLine($"{Indent(indent)}Assign: {a.TargetName} =");
                PrintExpression(a.Expression, w, indent + tab);
                break;

            case ReturnStatement r:
                w.WriteLine($"{Indent(indent)}Return");
                PrintExpression(r.Expr, w, indent + tab);
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
                PrintExpression(bin.Left, w, indent + tab);
                PrintExpression(bin.Right, w, indent + tab);
                break;

            default:
                w.WriteLine($"{Indent(indent)}<unknown expression {expr.GetType().Name}>");
                break;
        }
    }

    private static string Indent(int n) => new string(' ', n);
}
