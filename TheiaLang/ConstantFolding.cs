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
                        WalkExpression(vd.Init);
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
                    WalkExpression(vd.Init);
                break;
        }
    }

    void WalkExpression(IExpression expression)
    {
        switch(expression)
        {
            case BinaryExpression binary:
                WalkExpression(binary.Right);
                break;
            case CallExpression call:
                if(call.Target is IdentifierExpression id && id.Name == "TypeSize")
                {
                    TypeLayout typeLayout = Layout.GetLayout(call.Arguments[0].ResolvedType!);
                    expression = new LiteralExpression(typeLayout.Size, typeLayout.Size.ToString());
                    expression.ResolvedType = Builtins.GetTypeInfo(S64);
                }
                else
                    WalkExpression(call.Target);
                break;
            case UnaryExpression unary:
                WalkExpression(unary.Operand);
                break;
            case MemberAccessExpression mem:
                WalkExpression(mem.Target);
                break;
            case InstantiationExpression inst:
                foreach (IExpression argument in inst.Arguments)
                    WalkExpression(argument);
                break;
            case IndexExpression index:
                WalkExpression(index.Target);
                WalkExpression(index.Index);
                break;
            case CastExpression cast:
                WalkExpression(cast.Target);
                break;
        }
    }
}
