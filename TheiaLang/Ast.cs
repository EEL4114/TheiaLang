namespace TheiaLang;

public sealed record ProgramNode(
    string Name,
    List<INode> Nodes
);

public interface INode { }
public interface IDeclaration : INode
{
    string Name { get; }
    TypeInfo ResolvedType { get; set; }
}

public interface IStatement : INode { }
public interface IExpression : INode
{
    TypeInfo? ResolvedType { get; set; }
    bool Assignable { get; }
}

#region  Operators
public enum UnaryOperator
{
    Negate,     // -1
    Invert,     // !false
    AddressOf,
    Dereference,
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

    EqualEqual,
    NotEqual,
    AND,
    OR,
    PlusEqual,
    MinusEqual,
    MultEqual,
    DivEqual
}
#endregion

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
    public TypeInfo ResolvedType { get; set; }
    public string Name { get; set; }
    public readonly List<TypeNamePair> Arguments;
    public readonly List<IStatement> Statements;    // the { … } body
    public FunctionDeclaration(string typeName,
                               string name,
                               List<TypeNamePair> paramaters,
                               List<IStatement> statements)
    {
        TypeName = typeName;
        Name = name;
        Arguments = paramaters;
        Statements = statements;

        ResolvedType = new TypeInfo(TypeName);
    }
}

public class StructDeclaration : IDeclaration
{
    public Scope? Scope;
    public string Name { get; set; }
    public TypeInfo ResolvedType { get; set; }
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
                                    fieldNames: fields.Select(f => f.Identifier).ToList(),
                                    fieldTypes: fields.Select(f => f.TypeName).ToList());
    }

    public override string ToString()
    {
        string s = $"struct {Name}: (\n";
        if (Fields != null)
        {
            s += $"    {Fields[0]}\n";
            for (int i = 1; i < Fields.Count; i++)
                s += $"    {Fields[i]}\n";
        }
        s += ")";
        return s;
    }
}

public record UnionDeclaration(
    string Name,
    List<TypeNamePair> Variants,
    TypeInfo ResolvedType
) : IDeclaration
{
    public TypeInfo ResolvedType { get; set; } = ResolvedType;
    public Scope? Scope { get; set; }
}

public sealed record VariableDeclaration(
    string TypeName,            // "int", "float", "bool"
    string Name,
    IExpression? Init           // null if no initializer
) : IDeclaration, IStatement
{
    public TypeInfo? ResolvedType { get; set; }
    public IExpression? Init { get; set; } = Init;
    public override string ToString()
    {
        string s = $"VariableDeclaration: {TypeName} {Name}";
        // TODO this can be done better
        if (Init != null)
            s += $"\n    Init: {Init}";
        return s;
    }
}

#endregion
public sealed record TypeNamePair(
    string TypeName,
    string Identifier
)
{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;

    public override string ToString()
    {
        if (ResolvedType != null)
            return $"{ResolvedType} {Identifier}";
        else
            return $"{TypeName} {Identifier}";
    }
}

#region  Statements

/// A “new” expression for any nominal type (struct, union, etc.)
/*
public sealed record CallArgument(
    string? Name,                     // null for positional, or the parameter/field name
    IExpression Value
) : INode;*/

public sealed record ExpressionStatement(
    IExpression Expression
) : IStatement
{ public IExpression Expression { get; set; } = Expression; }

public sealed record AssignmentStatement(
    IExpression Target,
    IExpression Expression
) : IStatement
{
    public IExpression Target { get; set; } = Target;
    public IExpression Expression { get; set; } = Expression;
}


public sealed record CompoundAssignmentStatement(
    IExpression Target,
    IExpression Expression,
    BinaryOperator Op
) : IStatement
{
    public IExpression Target { get; set; } = Target;
    public IExpression Expression { get; set; } = Expression;
}

public sealed record IfStatement(
    IExpression Condition,
    List<IStatement> ThenBranch,
    List<IStatement>? ElseBranch,
    Scope ThenScope,
    Scope? ElseScope
) : IStatement
{ public IExpression Condition { get; set; } = Condition; }


public sealed record ForStatement(
    IStatement? Initialiser,
    IExpression? Condition,
    IStatement? Iterator,
    List<IStatement> Body,
    Scope Scope
) : IStatement
{ public IExpression? Condition { get; set; } = Condition; }


public sealed record ReturnStatement(
    IExpression? Expression
) : IStatement
{
    public IExpression? Expression { get; set; } = Expression;
}
#endregion

#region  Expressions

public sealed record MemberAccessExpression(
    IdentifierExpression Target,
    IdentifierExpression Member
) : IExpression

{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => true;
}

public sealed record CallExpression(
    string CalleeName,                // both functions and types
    List<IExpression> Arguments       // positional & named args
) : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;
    public string CalleeName { get; set; } = CalleeName;
}

public sealed record CastExpression(
    CastOp CastKind,
    IExpression Target
) : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;
    public CastOp CastKind { get; set; } = CastKind;

}


public sealed record InstantiationExpression(
    string TypeName,
    List<IExpression> Arguments
) : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;
}

public sealed record UnaryExpression(
    UnaryOperator Op,
    IExpression Operand,
    bool assignable = false
) : IExpression
{
    public bool Assignable => assignable;
    public IExpression Operand = Operand;
    private TypeInfo? _resolvedType;
    public TypeInfo? ResolvedType
    {
        get => _resolvedType;
        set
        {
            _resolvedType = value;
            // automatically update the literal if that’s what the operand is
            if (Operand is LiteralExpression lit)
                lit.ResolvedType = value;
        }
    }
}

public sealed record BinaryExpression(
    IExpression Left,
    BinaryOperator Op,    // "+", "*", ">", etc.
    IExpression Right
) : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;
    public IExpression Left { get; set; } = Left;
    public IExpression Right { get; set; } = Right;
}

public sealed record LiteralExpression(
    object Value,       // boxed int, float, bool
    string Lexeme
) : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;
}

public sealed record IdentifierExpression(
    string Name
) : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => true;
}

public sealed record IndexExpression(
    IExpression Target,
    IExpression Index
) : IExpression
{
    public IExpression Target { get; set; } = Target;
    public IExpression Index { get; set; } = Index;
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => true;
}

#endregion