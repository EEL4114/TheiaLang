using TheiaLang;

static class AstPrinter
{
    const int tab = 4;
    public static void Print(ProgramNode program, TextWriter w)
        => PrintProgram(program, w, 0);

    static void PrintProgram(ProgramNode p, TextWriter w, int indent)
    {
        w.WriteLine($"Program: {p.Name}");
        w.WriteLine($"Generated at {DateTime.Now}");
        w.WriteLine();

        foreach (INode node in p.Declarations)
        {
            if (node is FunctionDeclaration function)
                PrintFunction(function, w, indent);
            if (node is StructDeclaration structDeclaration)
                PrintStruct(structDeclaration, w, indent);
            if (node is UnionDeclaration unionDeclaration)
                PrintUnion(unionDeclaration, w, indent);
        }
    }

    static void PrintStruct(StructDeclaration structDeclaration, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}StructDeclaration: {structDeclaration.Name}");
        w.WriteLine($"{Indent(indent + tab)}Fields: {{");
        foreach (TypeNamePair typeNamePair in structDeclaration.Fields)
            PrintTypeNamePair(typeNamePair, w, indent + tab * 2);
        w.WriteLine($"{Indent(indent + tab)}}}");

        if (structDeclaration.Functions.Count == 0)
            w.WriteLine();

        foreach (FunctionDeclaration function in structDeclaration.Functions)
            PrintFunction(function, w, indent + tab);
    }

    static void PrintUnion(UnionDeclaration unionDeclaration, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}UnionDeclaration: {unionDeclaration.Name}");
        foreach (TypeNamePair variant in unionDeclaration.Variants)
            PrintTypeNamePair(variant, w, indent + tab);
        w.WriteLine();
    }

    static void PrintTypeNamePair(TypeNamePair typeNamePair, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}{typeNamePair.TypeName} {typeNamePair.Name}");
    }

    static void PrintFunction(FunctionDeclaration functionDeclaration, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}FunctionDeclaration: {functionDeclaration.ResolvedType} {functionDeclaration.Name}");
        w.WriteLine($"{Indent(indent + tab)}Arguments: (");
        foreach (TypeNamePair typeNamePair in functionDeclaration.Parameters)
            PrintTypeNamePair(typeNamePair, w, indent + tab * 2);
        w.WriteLine($"{Indent(indent + tab)})");

        PrintBlock(functionDeclaration.Statements, w, indent + tab);
    }

    static void PrintBlock(List<IStatement> block, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}BlockSatement");
        foreach (IStatement stmt in block)
            PrintStatement(stmt, w, indent + tab);
        w.WriteLine();
    }

    static void PrintStatement(IStatement stmt, TextWriter w, int indent)
    {
        switch (stmt)
        {
            case VariableDeclaration vd:
                w.WriteLine($"{Indent(indent)}VariableDeclaration: {vd.TypeName} {vd.Name}" +
                            (vd.Init is not null ? " =" : ""));
                if (vd.Init is not null)
                    PrintExpression(vd.Init, w, indent + tab);
                w.WriteLine();
                break;

            case AssignmentStatement a:
                w.WriteLine($"{Indent(indent)}Assign: {a.Target.Name} =");
                PrintExpression(a.Expression, w, indent + tab);
                w.WriteLine();
                break;
            case ReturnStatement r:
                w.WriteLine($"{Indent(indent)}Return");
                PrintExpression(r.Expr, w, indent + tab);
                break;
            case ExpressionStatement e:
                w.WriteLine($"{Indent(indent)}Expression:");
                PrintExpression(e.Expression, w, indent + tab);
                w.WriteLine();
                break;

            default:
                w.WriteLine($"{Indent(indent)}<unknown statement '{stmt.GetType().Name}'>");
                break;
        }
    }

    static void PrintExpression(IExpression expr, TextWriter w, int indent)
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
            case CallExpression call:
                w.WriteLine($"{Indent(indent)}Call: {call.CalleeName}");
                w.WriteLine($"{Indent(indent + tab)}Arguments: (");

                foreach (IExpression arument in call.Arguments)
                    PrintExpression(arument, w, indent + tab * 2);

                w.WriteLine($"{Indent(indent + tab)})");
                break;
            case UnaryExpression u:
                w.WriteLine($"{Indent(indent)}UnaryExpression: {u.Op}");
                w.WriteLine($"{Indent(indent + tab)}Operand:");
                PrintExpression(u.Operand, w, indent + tab * 2);
                break;
            case MemberAccessExpression m:
                w.WriteLine($"{Indent(indent)}MemberAccess: Target: '{m.Target.Name}'");
                w.WriteLine($"{Indent(indent + tab)}Member: '{m.Member.Name}'");
                break;
            case InstantiationExpression isnt:
                w.WriteLine($"{Indent(indent)}Instantiation: {isnt.Type}");
                w.WriteLine($"{Indent(indent + tab)}Arguments: (");
                foreach (IExpression arument in isnt.Arguments)
                    PrintExpression(arument, w, indent + tab * 2);

                w.WriteLine($"{Indent(indent + tab)})");
                break;

            default:
                w.WriteLine($"{Indent(indent)}<unknown expression {expr.GetType().Name}>");
                break;
        }
    }

    static string Indent(int n) => new string(' ', n);
}
