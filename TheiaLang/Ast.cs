namespace TheiaLang;

public interface IStatement { }
public interface IExpression { }

public enum UnaryOperator
{
    Negate
}

public enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Greater,
    Less,
    Equal,
    NotEqual
}

public enum Type
{
    s32,
    f32,
    Bool,
}

public sealed record FunctionDeclaration(
    Type ReturnType,          // "int", "float", "bool"
    string Name,                // e.g. "main"
    List<Parameter> Parameters, // empty for now
    BlockSatement Body          // the { … } body
);

public sealed record ProgramNode(
    List<FunctionDeclaration> Functions
);

public sealed record Parameter(
    Type Type,
    string Name
);

#region  statements
public sealed record BlockSatement(
    List<IStatement> Statements
) : IStatement;

public sealed record VariableDeclarationStatement(
    Type Type,             // "int", "float", "bool"
    string Name,
    IExpression? Init           // null if no initializer
) : IStatement;

public sealed record AssignmentStatement(
    string TargetName,
    IExpression Expression
) : IStatement;

public sealed record ReturnStatement(
    IExpression Expr
) : IStatement;
#endregion

#region  expressions

public sealed record UnaryExpr(
    UnaryOperator Op,
    IExpression Operand
) : IExpression;

public sealed record BinaryExpression(
    IExpression Left,
    BinaryOperator Op,    // "+", "*", ">", etc.
    IExpression Right
) : IExpression;

public sealed record LiteralExpression(
    object Value,       // boxed int, double, bool
    string Lexeme
) : IExpression;

public sealed record IdentifierExpression(
    string Name
) : IExpression;
#endregion