namespace TheiaLang;

using static BuiltinType;

public class ConstantFolding(LayoutCalc layout)
{
    public LayoutCalc Layout = layout;

    public void ConstFold(ProgramNode program)
    {
        foreach(INode node in program.Nodes)
        {
            switch(node)
            {
                case FunctionDeclaration fn:
                    WalkBlock(fn.Statements);
                break;
                case StructDeclaration sd:
                    foreach(FunctionDeclaration function in sd.Functions)
                        WalkBlock(function.Statements);
                break;
                case VariableDeclaration vd:
                    if(vd.Init != null)
                        vd.Init = WalkExpression(vd.Init);
                break;
            }
        }
    }

    void WalkBlock(List<IStatement> block)
    {
        for(int i = 0; i < block.Count; i++)
            block[i] = WalkStatement(block[i]);
    }

    IStatement WalkStatement(IStatement statement)
    {
        switch (statement)
        {
            case VariableDeclaration vd:
                if(vd.Init != null)
                    vd.Init = WalkExpression(vd.Init);
                return vd;
            case AssignmentStatement a:
                a.Target = WalkExpression(a.Target);
                a.Expression = WalkExpression(a.Expression);
                return a;
            case IfStatement ifStatement:
                ifStatement.Condition = WalkExpression(ifStatement.Condition);
                WalkBlock(ifStatement.ThenBranch);
                if(ifStatement.ElseBranch != null)
                    WalkBlock(ifStatement.ElseBranch);
                return ifStatement;
            case ForStatement forStatement:
                forStatement.Initialiser = WalkStatement(forStatement.Initialiser!);
                forStatement.Condition = WalkExpression(forStatement.Condition!);
                forStatement.Iterator = WalkStatement(forStatement.Iterator!);
                WalkBlock(forStatement.Body);
                return forStatement;
            case ReturnStatement r:
                if(r.Expression != null)
                    r.Expression = WalkExpression(r.Expression);
                return r;
            case ExpressionStatement exprS:
                exprS.Expression = WalkExpression(exprS.Expression);
                return exprS;
            case CompoundAssignmentStatement compound:
                compound.Target = WalkExpression(compound.Target);
                compound.Expression = WalkExpression(compound.Expression);
                return compound;
        }

        return statement;
    }

    IExpression WalkExpression(IExpression expression)
    {
        switch(expression)
        {
            case BinaryExpression binary:
                binary.Left  = WalkExpression(binary.Left);
                binary.Right = WalkExpression(binary.Right);
                return binary;
            case CallExpression call:
            {
                if (call.Target is IdentifierExpression id &&
                    id.Name == "TypeSize")
                {
                    TypeLayout typeLayout =
                        Layout.GetLayout(call.Arguments[0].ResolvedType!);

                    return new LiteralExpression(
                        (long)typeLayout.Size,
                        typeLayout.Size.ToString(),
                        call.Pos)
                    {
                        ResolvedType = call.ResolvedType
                            ?? throw new InvalidOperationException(
                                "Resolved TypeSize call has no result type")
                    };
                }

                call.Target = WalkExpression(call.Target);
                for (int i = 0; i < call.Arguments.Count; i++)
                    call.Arguments[i] = WalkExpression(call.Arguments[i]);

                return call;
            }
            case UnaryExpression unary:
                unary.Operand = WalkExpression(unary.Operand);
                return unary;
            case MemberAccessExpression mem:
                mem.Target = WalkExpression(mem.Target);
                return mem;
            case InstantiationExpression inst:
                for(int i = 0; i < inst.Arguments.Count; i++)
                    inst.Arguments[i] = WalkExpression(inst.Arguments[i]);
                return inst;
            case IndexExpression index:
                index.Target = WalkExpression(index.Target);
                index.Index = WalkExpression(index.Index);
                return index;
            case CastExpression cast:
                cast.Target = WalkExpression(cast.Target);
                return cast;
        }

        return expression;
    }
}
