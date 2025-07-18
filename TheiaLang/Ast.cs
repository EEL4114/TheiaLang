namespace TheiaLang;

public interface INode { }
public interface IDeclaration : INode
{
    string Name { get; }
    string ReturnType { get; }
}
public interface IStatement : INode { }
public interface IExpression : INode { bool Assignable { get; } }

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
    public string ReturnType { get; }                // "int", "float", "bool"
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
    public string ReturnType { get; }
    public readonly List<TypeNamePair> Fields;
    public readonly List<FunctionDeclaration> Functions;

    public StructDeclaration(string name,
                             List<TypeNamePair> fields,
                             List<FunctionDeclaration> functions)
    {
        Name = name;
        Fields = fields;
        Functions = functions;

        ReturnType = name;
    }
}

public record UnionDeclaration(
    string Name,
    List<TypeNamePair> Variants,
    string returnType
) : IDeclaration
{
    public string ReturnType { get; } = returnType;
}

public record VariableDeclaration(
    string Type,                  // "int", "float", "bool"
    string Name,
    IExpression? Init           // null if no initializer
) : IDeclaration, IStatement
{
    public string ReturnType => Type;
}

public sealed record TypeNamePair(
    string Type,
    string Name
) : IDeclaration
{
    public string ReturnType => Type;
}
#endregion

public sealed record ProgramNode(
    string Name,
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
    IdentifierExpression Target,
    IExpression Expression
) : IStatement;

public sealed record ReturnStatement(
    IExpression Expr
) : IStatement;
#endregion

#region  Expressions

public sealed record MemberAccessExpression(
    IdentifierExpression Target,
    IdentifierExpression Member
) : IExpression
{ public bool Assignable => true; }

public sealed record CallExpression(
    string CalleeName,                // both functions and types
    List<IExpression> Arguments      // positional & named args
) : IExpression
{ public bool Assignable => false; }

public sealed record InstantiationExpression(
    string TypeName,
    List<IExpression> Arguments
) : IExpression
{ public bool Assignable => false; }

public sealed record UnaryExpression(
    UnaryOperator Op,
    IExpression Operand
) : IExpression
{ public bool Assignable => false; }

public sealed record BinaryExpression(
    IExpression Left,
    BinaryOperator Op,    // "+", "*", ">", etc.
    IExpression Right
) : IExpression
{ public bool Assignable => false; }

public sealed record LiteralExpression(
    object Value,       // boxed int, double, bool
    string Lexeme
) : IExpression
{ public bool Assignable => false; }

public sealed record IdentifierExpression(
    string Name
) : IExpression
{ public bool Assignable => true; }

#endregion