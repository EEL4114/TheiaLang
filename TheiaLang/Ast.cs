namespace TheiaLang;

public interface INode { }
public interface IDeclaration : INode { string Name { get; } }
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
    Struct,
}

#region  Declarations
public class FunctionDeclaration : IDeclaration
{
    public Scope? Scope;
    public readonly string ReturnType;                // "int", "float", "bool"
    public string Name { get; set; }                // e.g. "main"
    public readonly List<TypeNamePair> Parameters;  // empty for now
    public readonly List<IStatement> Statements;    // the { … } body
    public FunctionDeclaration(string returnType,
                               string name,
                               List<TypeNamePair> paramaters,
                               List<IStatement> statements)
    {
        ReturnType = returnType;
        Name = name;
        Parameters = paramaters;
        Statements = statements;
    }
}

public class StructDeclaration : IDeclaration
{
    public Scope? Scope;
    public string Name { get; }
    public readonly List<TypeNamePair> Fields;
    public readonly List<FunctionDeclaration> Functions;

    public StructDeclaration(string name,
                             List<TypeNamePair> fields,
                             List<FunctionDeclaration> functions)
    {
        Name = name;
        Fields = fields;
        Functions = functions;
    }
}

public record UnionDeclaration(
    string Name,
    List<TypeNamePair> Variants
) : IDeclaration;

public record VariableDeclaration(
    string Type,                  // "int", "float", "bool"
    string Name,
    IExpression? Init           // null if no initializer
) : IDeclaration, IStatement;

public sealed record TypeNamePair(
    string Type,
    string Name
) : IDeclaration;
#endregion

public sealed record ProgramNode(
    List<INode> Declarations
);


#region  Statements
/// A “new” expression for any nominal type (struct, union, etc.)
/*
public sealed record CallArgument(
    string? Name,                     // null for positional, or the parameter/field name
    IExpression Value
) : INode;*/

public sealed record ExpressionStatement(
    IExpression Expression
) : IStatement;

public sealed record AssignmentStatement(
    string TargetName,
    IExpression Expression
) : IStatement;

public sealed record ReturnStatement(
    IExpression Expr
) : IStatement;
#endregion

#region  Expressions

public sealed record CallExpression(
    string CalleeName,                // both functions and types
    List<IExpression> Arguments      // positional & named args
) : IExpression;
public sealed record InstantiationExpression(
    string TypeName,
    List<IExpression> Arguments
) : IExpression;

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