namespace TheiaLang;

public interface INode { }
public interface IStatement : INode { }
public interface IExpression : INode { }

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

#region  Declarations
public sealed record FunctionDeclaration(
    Type ReturnType,                // "int", "float", "bool"
    string Name,                    // e.g. "main"
    List<Parameter> Parameters,     // empty for now
    List<IStatement> Statements     // the { … } body
) : INode;

public sealed record StructDeclaration(
    string Name,
    List<Parameter> Fields,
    List<FunctionDeclaration> Methods   // empty if “;”‐form
) : INode;

public sealed record VariableDeclaration(
    Type Type,                  // "int", "float", "bool"
    string Name,
    IExpression? Init           // null if no initializer
) : IStatement;
#endregion

public sealed record ProgramNode(
    List<INode> Declarations
);

public sealed record Parameter(
    Type Type,
    string Name
) : INode;

#region  Statements
public sealed record AssignmentStatement(
    string TargetName,
    IExpression Expression
) : IStatement;

public sealed record ReturnStatement(
    IExpression Expr
) : IStatement;
#endregion

#region  Expressions

public sealed record UnaryExpression(
    UnaryOperator Op,
    IExpression Operand
) : IExpression, INode;

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