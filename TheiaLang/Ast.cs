namespace TheiaLang;

public interface INode { }
public interface IDeclaration : INode
{
    string Name { get; }
    TypeInfo ResolvedType { get; }
}
public interface IStatement : INode { }
public interface IExpression : INode
{
    TypeInfo ResolvedType { get; }
    bool Assignable { get; }
}

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
    public string TypeName { get; }
    public TypeInfo ResolvedType { get; }
    public string Name { get; set; }
    public readonly List<TypeNamePair> Parameters;
    public readonly List<IStatement> Statements;    // the { … } body
    public FunctionDeclaration(string typeName,
                               string name,
                               List<TypeNamePair> paramaters,
                               List<IStatement> statements)
    {
        TypeName = typeName;
        Name = name;
        Parameters = paramaters;
        Statements = statements;
    }
}

public class StructDeclaration : IDeclaration
{
    public Scope? Scope;
    public string Name { get; }
    public TypeInfo ResolvedType { get; }
    public readonly List<TypeNamePair> Fields;
    public readonly List<FunctionDeclaration> Functions;

    public StructDeclaration(string name,
                             List<TypeNamePair> fields,
                             List<FunctionDeclaration> functions)
    {
        Name = name;
        Fields = fields;
        Functions = functions;

        ResolvedType = new TypeInfo(name,
                                 fields.Select(f => f.Name).ToList(),
                                 fields.Select(f => f.TypeName).ToList());
    }
}

public record UnionDeclaration(
    string Name,
    List<TypeNamePair> Variants,
    TypeInfo ResolvedType
) : IDeclaration;

public record VariableDeclaration(
    string TypeName,                  // "int", "float", "bool"
    string Name,
    IExpression? Init           // null if no initializer
) : IDeclaration, IStatement
{
    public TypeInfo? ResolvedType { get; }
}
#endregion
public sealed record TypeNamePair(
    string TypeName,
    string Name
)
{
    public TypeInfo? ResolvedType { get; }
    public bool Assignable => false;
}

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

{
    public TypeInfo? ResolvedType { get; }
    public bool Assignable => true;
}

public sealed record CallExpression(
    string CalleeName,                // both functions and types
    List<IExpression> Arguments       // positional & named args
) : IExpression
{
    public TypeInfo? ResolvedType { get; }
    public bool Assignable => false;
}

public sealed record InstantiationExpression(
    string Type,
    List<IExpression> Arguments
) : IExpression
{
    public TypeInfo? ResolvedType { get; }
    public bool Assignable => false;
}

public sealed record UnaryExpression(
    UnaryOperator Op,
    IExpression Operand
) : IExpression
{
    public TypeInfo? ResolvedType { get; }
    public bool Assignable => false;
}

public sealed record BinaryExpression(
    IExpression Left,
    BinaryOperator Op,    // "+", "*", ">", etc.
    IExpression Right
) : IExpression
{
    public TypeInfo? ResolvedType { get; }
    public bool Assignable => false;
}

public sealed record LiteralExpression(
    object Value,       // boxed int, float, bool
    string Lexeme
) : IExpression
{
    public TypeInfo? ResolvedType { get; }
    public bool Assignable => false;
}

public sealed record IdentifierExpression(
    string Name
) : IExpression
{
    public TypeInfo? ResolvedType { get; }
    public bool Assignable => true;
}
#endregion