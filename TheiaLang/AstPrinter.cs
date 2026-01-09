using TheiaLang;

static class AstPrinter
{
    const int tab = 4;
    // TODO this seems redundant?
    public static void Print(ProgramNode program, TextWriter w, bool includeTimestamp = true)
        => PrintProgram(program, w, 0, includeTimestamp);

    static void PrintProgram(ProgramNode p, TextWriter w, int indent, bool includeTimestamp = true)
    {
        w.WriteLine($"Program: {p.Name}");
        if (includeTimestamp)
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

        w.Flush();
    }

    #region Baisc

    static void PrintStruct(StructDeclaration structDeclaration, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}StructDeclaration: {structDeclaration.Name} ({structDeclaration.ResolvedType.Size} B) {TryScope(structDeclaration.Scope!)}");
        w.WriteLine($"{Indent(indent + tab)}Fields:");
        foreach (TypeNamePair typeNamePair in structDeclaration.Fields)
            PrintTypeNamePair(typeNamePair, w, indent + tab * 2);

        if (structDeclaration.Functions.Count == 0)
            w.WriteLine();

        w.WriteLine($"{Indent(indent + tab)}Functions:");
        foreach (FunctionDeclaration function in structDeclaration.Functions)
            PrintFunction(function, w, indent + tab * 2);
    }

    static void PrintUnion(UnionDeclaration unionDeclaration, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}UnionDeclaration: {unionDeclaration.Name} ({unionDeclaration.ResolvedType.Size} B){TryScope(unionDeclaration.Scope!)}");
        foreach (TypeNamePair variant in unionDeclaration.Variants)
            PrintTypeNamePair(variant, w, indent + tab);
        w.WriteLine();
    }

    static void PrintTypeNamePair(TypeNamePair typeNamePair, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}{TryType(typeNamePair.ResolvedType)}{typeNamePair.Identifier}");
    }

    static void PrintFunction(FunctionDeclaration function, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}FunctionDeclaration: {function.TypeName} {function.Name}{TryScope(function.Scope!)}");
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

    #endregion

    #region  Statements

    static void PrintStatement(IStatement statement, TextWriter w, int indent)
    {
        switch (statement)
        {
            case VariableDeclaration vd:
                w.WriteLine($"{Indent(indent)}VariableDeclaration: {TryType(vd.ResolvedType)}{vd.Name}" +
                            (vd.Init is not null ? " =" : ""));
                if (vd.Init != null)
                    PrintExpression(vd.Init, w, indent + tab);
                break;
            case AssignmentStatement a:
                w.WriteLine($"{Indent(indent)}Assignment:");
                PrintExpression(a.Target, w, indent + tab);
                PrintExpression(a.Expression, w, indent + tab);
                break;
            case IfStatement ifStatement:
                w.WriteLine($"{Indent(indent)}If:");
                w.WriteLine($"{Indent(indent + tab)}Condition:");
                PrintExpression(ifStatement.Condition, w, indent + tab * 2);
                w.WriteLine($"{Indent(indent + tab)}Then:{TryScope(ifStatement.ThenScope)}");
                PrintBlock(ifStatement.ThenBranch, w, indent + tab * 2);
                if (ifStatement.ElseBranch != null)
                {
                    w.WriteLine($"{Indent(indent + tab)}Else:{TryScope(ifStatement.ElseScope!)}");
                    PrintBlock(ifStatement.ElseBranch, w, indent + tab * 2);
                }
                break;
            case ForStatement forStatement:
                w.WriteLine($"{Indent(indent)}For:{TryScope(forStatement.Scope)}");
                w.WriteLine($"{Indent(indent + tab)}Initialiser:");
                PrintStatement(forStatement.Initialiser!, w, indent + tab * 2);
                w.WriteLine($"{Indent(indent + tab)}Condition:");
                PrintExpression(forStatement.Condition!, w, indent + tab * 2);
                w.WriteLine($"{Indent(indent + tab)}Iterator:");
                PrintStatement(forStatement.Iterator!, w, indent + tab * 2);
                PrintBlock(forStatement.Body, w, indent + tab * 2);
                break;
            case ReturnStatement r:
                    w.WriteLine($"{Indent(indent)}Return: {TryType(r.Expression?.ResolvedType)}");
                if(r.Expression != null)
                    PrintExpression(r.Expression, w, indent + tab);
                    
                break;
            case ExpressionStatement e:
                w.WriteLine($"{Indent(indent)}Expression:");
                PrintExpression(e.Expression, w, indent + tab);
                w.WriteLine();
                break;
            case CompoundAssignmentStatement compound:
                w.WriteLine($"{Indent(indent)}CompoundAssignment: {compound.Op}");
                PrintExpression(compound.Target, w, indent + tab);
                PrintExpression(compound.Expression, w, indent + tab);
                break;
            default:
                w.WriteLine($"{Indent(indent)}<unknown statement '{statement.GetType().Name}'>");
                w.WriteLine($"{Indent(indent + tab)}<{statement}'>");
                break;
        }
    }

    #endregion

    #region Expressions

    static void PrintExpression(IExpression expr, TextWriter w, int indent)
    {
        switch (expr)
        {
            case LiteralExpression lit:
                w.WriteLine($"{Indent(indent)}Literal: {TryType(lit.ResolvedType)}{lit.Lexeme}");
                break;

            case IdentifierExpression id:
                w.WriteLine($"{Indent(indent)}Identifier: {TryType(id.ResolvedType)}{id.Name}{TryScope(id.Scope)}");
                break;

            case BinaryExpression bin:
                w.WriteLine($"{Indent(indent)}BinaryExpression: {TryType(bin.ResolvedType)}{bin.Op}");
                PrintExpression(bin.Left, w, indent + tab);
                PrintExpression(bin.Right, w, indent + tab);
                break;
            case CallExpression call:
                w.WriteLine($"{Indent(indent)}Call: {TryType(call.ResolvedType)}");
                w.WriteLine($"{Indent(indent + tab)}Target:");
                PrintExpression(call.Target, w, indent + tab * 2);
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
                w.WriteLine($"{Indent(indent)}MemberAccess: {TryType(mem.ResolvedType)}");
                w.WriteLine($"{Indent(indent + tab)}Target: {TryType(mem.Target.ResolvedType)}");
                PrintExpression(mem.Target, w, indent + tab * 2);
                w.WriteLine($"{Indent(indent + tab)}Member: {TryType(mem.Member.ResolvedType)}{mem.Member.Name}");
                break;
            case InstantiationExpression isnt:
                string type = TryType(isnt.ResolvedType);
                type = string.IsNullOrEmpty(type) ? isnt.TypeName : type;
                w.WriteLine($"{Indent(indent)}Instantiation: {type}");
                w.WriteLine($"{Indent(indent + tab)}Arguments:");
                // TODO write the field names maybe?
                foreach (IExpression arument in isnt.Arguments)
                    PrintExpression(arument, w, indent + tab * 2);
                break;
            case IndexExpression index:
                w.WriteLine($"{Indent(indent)}IndexExpression: {TryType(index.ResolvedType)}");
                w.WriteLine($"{Indent(indent + tab)}Target:");
                PrintExpression(index.Target, w, indent + tab * 2);
                w.WriteLine($"{Indent(indent + tab)}Index:");
                PrintExpression(index.Index, w, indent + tab * 2);
                break;
            case CastExpression cast:
                w.WriteLine($"{Indent(indent)}Cast: {TryType(cast.ResolvedType)} {cast.CastKind}");
                w.WriteLine($"{Indent(indent + tab)}Target:");
                PrintExpression(cast.Target, w, indent + tab * 2);
                break;
            default:
                w.WriteLine($"{Indent(indent)}<unknown expression {expr.GetType().Name}>");
                w.WriteLine($"{Indent(indent + tab)}<{expr}>");
                break;
        }
    }

    #endregion

    #region Helpers

    static string Indent(int n) => new string(' ', n);

    static string TryType(TypeInfo? typeInfo) => string.IsNullOrEmpty(typeInfo?.TypeName) ? "" : $"{typeInfo.TypeName} ({typeInfo.Size} B) ";
    static string TryScope(Scope scope) => scope == null ? " | Scope: ---" : $" | Scope: '{scope.Name}' ";
    #endregion
}
