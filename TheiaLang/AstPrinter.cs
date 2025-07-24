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

        foreach (INode node in p.Nodes)
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
        w.WriteLine($"{Indent(indent + tab)}Fields:");
        foreach (TypeNamePair typeNamePair in structDeclaration.Fields)
            PrintTypeNamePair(typeNamePair, w, indent + tab * 2);

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

    static void PrintFunction(FunctionDeclaration function, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}FunctionDeclaration: {function.TypeName} {function.Name}");
        w.WriteLine($"{Indent(indent + tab)}Arguments:");
        foreach (TypeNamePair typeNamePair in function.Arguments)
            PrintTypeNamePair(typeNamePair, w, indent + tab * 2);
        PrintBlock(function.Statements, w, indent + tab);
    }

    static void PrintBlock(List<IStatement> block, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}BlockSatement:");
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
                if (vd.Init != null)
                    PrintExpression(vd.Init, w, indent + tab);
                break;
            case AssignmentStatement a:
                w.WriteLine($"{Indent(indent)}Assign:");
                PrintExpression(a.Target, w, indent + tab);
                PrintExpression(a.Expression, w, indent + tab);
                break;
            case IfStatement ifStatement:
                w.WriteLine($"{Indent(indent)}If:");
                w.WriteLine($"{Indent(indent + tab)}Condition:");
                PrintExpression(ifStatement.Condition, w, indent + tab * 2);
                w.WriteLine($"{Indent(indent + tab)}Then:");
                PrintBlock(ifStatement.ThenBranch, w, indent + tab * 2);
                if (ifStatement.ElseBranch != null)
                {
                    w.WriteLine($"{Indent(indent + tab)}Else:");
                    PrintBlock(ifStatement.ElseBranch, w, indent + tab * 2);
                }
                break;
            case ReturnStatement r:
                w.WriteLine($"{Indent(indent)}Return: {TryType(r.Expression.ResolvedType)}");
                PrintExpression(r.Expression, w, indent + tab);
                break;
            case ExpressionStatement e:
                w.WriteLine($"{Indent(indent)}Expression:");
                PrintExpression(e.Expression, w, indent + tab);
                w.WriteLine();
                break;

            default:
                w.WriteLine($"{Indent(indent)}<unknown statement '{stmt.GetType().Name}'>");
                w.WriteLine($"{Indent(indent + tab)}<{stmt}'>");
                break;
        }
    }

    static void PrintExpression(IExpression expr, TextWriter w, int indent)
    {
        switch (expr)
        {
            case LiteralExpression lit:
                w.WriteLine($"{Indent(indent)}Literal: {TryType(lit.ResolvedType)}{lit.Lexeme}");
                break;

            case IdentifierExpression id:
                w.WriteLine($"{Indent(indent)}Identifier: {TryType(id.ResolvedType)}{id.Name}");
                break;

            case BinaryExpression bin:
                w.WriteLine($"{Indent(indent)}BinaryExpression: {TryType(bin.ResolvedType)}{bin.Op}");
                PrintExpression(bin.Left, w, indent + tab);
                PrintExpression(bin.Right, w, indent + tab);
                break;
            case CallExpression call:
                w.WriteLine($"{Indent(indent)}Call: {TryType(call.ResolvedType)} {call.CalleeName}");
                w.WriteLine($"{Indent(indent + tab)}Arguments:");

                foreach (IExpression arument in call.Arguments)
                    PrintExpression(arument, w, indent + tab * 2);
                break;
            case UnaryExpression u:
                w.WriteLine($"{Indent(indent)}UnaryExpression: {TryType(u.ResolvedType)}{u.Op}");
                w.WriteLine($"{Indent(indent + tab)}Operand:");
                PrintExpression(u.Operand, w, indent + tab * 2);
                break;
            case MemberAccessExpression mem:
                w.WriteLine($"{Indent(indent)}MemberAccess: {TryType(mem.ResolvedType)}{mem.Target.Name}.{mem.Member.Name}");
                w.WriteLine($"{Indent(indent + tab)}Target: {TryType(mem.Target.ResolvedType)}{mem.Target.Name}");
                w.WriteLine($"{Indent(indent + tab)}Member: {TryType(mem.Member.ResolvedType)}{mem.Member.Name}");
                break;
            case InstantiationExpression isnt:
                w.WriteLine($"{Indent(indent)}Instantiation: {isnt.TypeName}");
                w.WriteLine($"{Indent(indent + tab)}Arguments: (");
                foreach (IExpression arument in isnt.Arguments)
                    PrintExpression(arument, w, indent + tab * 2);

                w.WriteLine($"{Indent(indent + tab)})");
                break;

            default:
                w.WriteLine($"{Indent(indent)}<unknown expression {expr.GetType().Name}>");
                w.WriteLine($"{Indent(indent + tab)}<{expr}>");
                break;
        }
    }
    static string Indent(int n) => new string(' ', n);

    static string TryType(TypeInfo? typeInfo) => string.IsNullOrEmpty(typeInfo?.TypeName) ? "" : $"{typeInfo.TypeName} ";
}
