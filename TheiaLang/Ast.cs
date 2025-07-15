using System.Dynamic;

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
public class FunctionDeclaration : INode
{
    public Scope? Scope;
    public readonly Type ReturnType;              // "int", "float", "bool"
    public string Name;                   // e.g. "main"
    public readonly List<TypeNamePair> Parameters;     // empty for now
    public readonly List<IStatement> Statements;     // the { … } body
    public FunctionDeclaration(Type returnType,
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

public class StructDeclaration : INode
{
    public Scope? Scope;
    public readonly string Name;
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
) : INode;

public record VariableDeclaration(
    Type Type,                  // "int", "float", "bool"
    string Name,
    IExpression? Init           // null if no initializer
) : IStatement;
#endregion

public sealed record ProgramNode(
    List<INode> Declarations
);

public sealed record TypeNamePair(
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