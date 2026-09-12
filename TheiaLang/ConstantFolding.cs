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
        foreach(IStatement statement in block)
            WalkStatement(statement);
    }

    void WalkStatement(IStatement statement)
    {
        switch (statement)
        {
            case VariableDeclaration vd:
                if(vd.Init != null)
                    vd.Init = WalkExpression(vd.Init);
                break;
            case AssignmentStatement a:
                a.Target = WalkExpression(a.Target);
                a.Expression = WalkExpression(a.Expression);
                break;
            case IfStatement ifStatement:
                ifStatement.Condition = WalkExpression(ifStatement.Condition);
                WalkBlock(ifStatement.ThenBranch);
                if(ifStatement.ElseBranch != null)
                    WalkBlock(ifStatement.ElseBranch);
                break;
            case ForStatement forStatement:
                WalkStatement(forStatement.Initialiser!);
                forStatement.Condition = WalkExpression(forStatement.Condition!);
                WalkStatement(forStatement.Iterator!);
                break;
            case ReturnStatement r:
                if(r.Expression != null)
                    r.Expression = WalkExpression(r.Expression);
                break;
            case ExpressionStatement exprS:
                exprS.Expression = WalkExpression(exprS.Expression);
            break;
            case CompoundAssignmentStatement compound:
                compound.Target = WalkExpression(compound.Target);
                compound.Expression = WalkExpression(compound.Expression);
            break;
        }
    }

    IExpression WalkExpression(IExpression expression)
    {
        switch(expression)
        {
            case BinaryExpression binary:
                binary.Right = WalkExpression(binary.Right);
                return binary;
            case CallExpression call:
                if(call.Target is IdentifierExpression id && id.Name == "TypeSize")
                {
                    TypeLayout typeLayout = Layout.GetLayout(call.Arguments[0].ResolvedType!);
                    expression = new LiteralExpression(typeLayout.Size, typeLayout.Size.ToString());
                    expression.ResolvedType = Builtins.GetTypeInfo(S64);
                }
                else
                    call.Target = WalkExpression(call.Target);
                    return call;
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
