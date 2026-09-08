namespace TheiaLang;

public sealed record ProgramNode(
    string Name,
    List<INode> Nodes
);

public interface INode
{
    SourePosition Pos { get; }
}

public interface IDeclaration : INode, IStatement
{
    string Name { get; }
    TypeInfo ResolvedType { get; set; }
}

public interface IStatement : INode
{
    
}

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
    public readonly List<TypeNamePair> Parameters;
    public readonly List<IStatement> Statements;    // the { … } body

    public SourePosition Pos { get; }


    public FunctionDeclaration(string typeName,     // possibly change this?
                               TypeKind typeKind,
                               string name,
                               List<TypeNamePair> paramaters,
                               List<IStatement> statements,
                               SourePosition pos = default)
    {
        TypeName = typeName;
        Name = name;
        Parameters = paramaters;
        Statements = statements;

        ResolvedType = new TypeInfo(TypeName, typeKind);
    
        Pos = pos;
    }

    public override string ToString()
    {
        string s = $"{TypeName} {Name} (";

        if (Parameters != null)
            for (int i = 0; i < Parameters.Count; i++)
                s += $"{Parameters[i].ResolvedType.TypeName} {Parameters[i].Identifier}";

        s += ")";
        return s;
    }
}

public struct StructDeclaration : IDeclaration
{
    public Scope? Scope;
    public string Name { get; set; }
    public TypeInfo ResolvedType { get; set; }
    public readonly List<TypeNamePair> Fields;
    public readonly List<FunctionDeclaration> Functions;
    public SourePosition Pos { get; }

    public StructDeclaration(string name,
                             List<TypeNamePair> fields,
                             List<FunctionDeclaration> functions,
                             SourePosition pos = default)
    {
        Name = name;
        Fields = fields;
        Functions = functions;

        ResolvedType = new TypeInfo(name, TypeKind.Struct,
                                    fieldNames: fields.Select(f => f.Identifier).ToList(),
                                    fieldTypes: fields.Select(f => f.ResolvedType).ToList());

        Pos = pos;
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
        if(Functions != null)
        {
            s += $"{{\n    {Functions[0]}\n";
            for (int i = 1; i < Functions.Count; i++)
                s += $"    {Functions[i]}\n";
        }
        s+="}";

        return s;
    }
}

public record UnionDeclaration(
    string Name,
    List<TypeNamePair> Variants,
    // List<FunctionDeclaration> Functions,
    TypeInfo ResolvedType,
    SourePosition Pos = default
) : IDeclaration
{
    // public List<FunctionDeclaration> Functions {get; set;} = Functions;
    public TypeInfo ResolvedType { get; set; } = ResolvedType;
    public Scope? Scope { get; set; }
    public SourePosition Pos { get; } = Pos;
}

public sealed record VariableDeclaration(
    string TypeName,            // "int", "float", "bool"
    string Name,
    IExpression? Init,          // null if no initializer
    SourePosition Pos = default
) : IDeclaration
{
    public TypeInfo? ResolvedType { get; set; }
    public IExpression? Init { get; set; } = Init;

    public SourePosition Pos { get; } = Pos;

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

public class TypeNamePair(
    TypeInfo resolvedType,
    string identifier,
    SourePosition Pos = default
)
{
    public string Identifier { get; init; } = identifier;
    public TypeInfo ResolvedType { get; set; } = resolvedType;
    public static bool Assignable => false;

    public SourePosition Pos { get; } = Pos;

    public override string ToString()
    {
        return $"{ResolvedType} {Identifier}";
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
    IExpression Expression,
    SourePosition Pos = default
) : IStatement
{ 
    public IExpression Expression { get; set; } = Expression;
    public SourePosition Pos { get; } = Pos;
}

public sealed record AssignmentStatement(
    IExpression Target,
    IExpression Expression,
    SourePosition Pos = default
) : IStatement
{
    public IExpression Target { get; set; } = Target;
    public IExpression Expression { get; set; } = Expression;
 
    public SourePosition Pos { get; } = Pos;
}


public sealed record CompoundAssignmentStatement(
    IExpression Target,
    IExpression Expression,
    BinaryOperator Op,
    SourePosition Pos = default
) : IStatement
{
    public IExpression Target { get; set; } = Target;
    public IExpression Expression { get; set; } = Expression;
 
    public SourePosition Pos { get; } = Pos;
}

public sealed record IfStatement(
    IExpression Condition,
    List<IStatement> ThenBranch,
    List<IStatement>? ElseBranch,
    Scope ThenScope,
    Scope? ElseScope,
    SourePosition Pos = default
) : IStatement
{ 
    public IExpression Condition { get; set; } = Condition; 
 
    public SourePosition Pos { get; } = Pos;
}


public sealed record ForStatement(
    IStatement? Initialiser,
    IExpression? Condition,
    IStatement? Iterator,
    List<IStatement> Body,
    Scope Scope,
    SourePosition Pos = default
) : IStatement
{ 
    public IExpression? Condition { get; set; } = Condition; 

    public SourePosition Pos { get; } = Pos;
}


public sealed record ReturnStatement(
    IExpression? Expression,
    SourePosition Pos = default
) : IStatement
{
    public IExpression? Expression { get; set; } = Expression;

    public SourePosition Pos { get; } = Pos;
}
#endregion

#region  Expressions

public sealed record MemberAccessExpression(
    IExpression Target,
    IdentifierExpression Member,
    SourePosition Pos,
    Scope? Scope = null
) : IExpression

{
    public IExpression Target = Target;
    public IdentifierExpression Member = Member;
    public Scope? Scope = Scope;
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => true;

    public SourePosition Pos { get; } = Pos;
}

public sealed record CallExpression(
    IExpression Target,                // both functions and types
    List<IExpression> Arguments,       // positional & named args
    SourePosition Pos = default,
    Scope? Scope = null
) : IExpression
{
    public IExpression Target { get; set; } = Target;
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;
    public Scope? Scope { get; set; } = Scope;

    public SourePosition Pos { get; } = Pos;
}

public sealed record CastExpression(
    CastOp CastKind,
    IExpression Target,
    SourePosition Pos = default
) : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;
    public CastOp CastKind { get; set; } = CastKind;

    public SourePosition Pos { get; } = Pos;
}

public sealed record InstantiationExpression(
    string TypeName,
    List<IExpression> Arguments,
    SourePosition Pos = default
) : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;

    public SourePosition Pos { get; } = Pos;
}

public sealed record UnaryExpression(
    UnaryOperator Op,
    IExpression Operand,
    SourePosition Pos = default,
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

    public SourePosition Pos { get; } = Pos;
}

public sealed record BinaryExpression(
    IExpression Left,
    BinaryOperator Op,    // "+", "*", ">", etc.
    IExpression Right,
    SourePosition Pos = default
)
 : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;
    public IExpression Left { get; set; } = Left;
    public IExpression Right { get; set; } = Right;

    public SourePosition Pos { get; } = Pos;
}

public sealed record LiteralExpression(
    object Value,       // boxed int, float, bool
    string Lexeme,
    SourePosition Pos = default
) : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;

    public SourePosition Pos { get; } = Pos;
}

public sealed record IdentifierExpression(
    string Name,
    SourePosition Pos = default,
    Scope? Scope = null
) : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public Scope? Scope = Scope;
    public bool Assignable => true;

    public SourePosition Pos { get; } = Pos;
}

public sealed record IndexExpression(
    IExpression Target,
    IExpression Index,
    SourePosition Pos = default
) : IExpression
{
    public IExpression Target { get; set; } = Target;
    public IExpression Index { get; set; } = Index;
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => true;

    public SourePosition Pos { get; } = Pos;
}

#endregion