namespace TheiaLang;

public sealed class ProgramNode(
    string name,
    List<INode> nodes
)
{
    public string Name = name;
    public List<INode> Nodes = nodes;
};

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
    public TypeInfo ResolvedType { get; set; }
    public string Name { get; set; }
    public readonly List<TypeNamePair> Parameters;
    public readonly List<IStatement> Statements;    // the { … } body

    public SourePosition Pos { get; }


    public FunctionDeclaration(TypeInfo resolvedType,
                               string name,
                               List<TypeNamePair> paramaters,
                               List<IStatement> statements,
                               SourePosition pos = default)
    {
        Name = name;
        Parameters = paramaters;
        Statements = statements;

        ResolvedType = resolvedType;
    
        Pos = pos;
    }

    public override string ToString()
    {
        string s = $"{ResolvedType.TypeName} {Name} (";

        if (Parameters != null)
            for (int i = 0; i < Parameters.Count; i++)
                s += $"{Parameters[i].ResolvedType.TypeName} {Parameters[i].Identifier}";

        s += ")";
        return s;
    }
}

public class StructDeclaration : IDeclaration
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

        ResolvedType = new TypeInfo(name, TypeKind.Struct, null,
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

public class UnionDeclaration(
    string name,
    List<TypeNamePair> variants,
    // List<FunctionDeclaration> Functions,
    TypeInfo ResolvedType,
    SourePosition Pos = default
) : IDeclaration
{
    // public List<FunctionDeclaration> Functions {get; set;} = Functions;
    public string Name {get; set;} = name;
    public List<TypeNamePair> Variants = variants;

    public TypeInfo ResolvedType { get; set; } = ResolvedType;
    public Scope? Scope { get; set; }
    public SourePosition Pos { get; } = Pos;
}

public sealed class VariableDeclaration(
    string typeName,            // "int", "float", "bool"
    string name,
    IExpression? Init,          // null if no initializer
    SourePosition Pos = default
) : IDeclaration
{
    public string Name {get; set;} = name;
    public string TypeName = typeName;
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

public sealed class ExpressionStatement(
    IExpression Expression,
    SourePosition Pos = default
) : IStatement
{ 
    public IExpression Expression  = Expression;
    public SourePosition Pos { get; } = Pos;
}

public sealed class AssignmentStatement(
    IExpression Target,
    IExpression Expression,
    SourePosition Pos = default
) : IStatement
{
    public IExpression Target { get; set; } = Target;
    public IExpression Expression { get; set; } = Expression;
 
    public SourePosition Pos { get; } = Pos;
}


public sealed class CompoundAssignmentStatement(
    IExpression Target,
    IExpression Expression,
    BinaryOperator op,
    SourePosition Pos = default
) : IStatement
{
    public IExpression Target { get; set; } = Target;
    public IExpression Expression { get; set; } = Expression;
    public BinaryOperator Op = op;
 
    public SourePosition Pos { get; } = Pos;
}

public sealed class IfStatement(
    IExpression Condition,
    List<IStatement> thenBranch,
    List<IStatement>? elseBranch,
    Scope thenScope,
    Scope? elseScope,
    SourePosition Pos = default
) : IStatement
{ 
    public IExpression Condition { get; set; } = Condition; 

    public List<IStatement> ThenBranch = thenBranch;
    public List<IStatement>? ElseBranch = elseBranch;
    public Scope ThenScope = thenScope;
    public Scope? ElseScope = elseScope;
 
    public SourePosition Pos { get; } = Pos;
}


public sealed class ForStatement(
    IStatement? initialiser,
    IExpression? Condition,
    IStatement? iterator,
    List<IStatement> body,
    Scope scope,
    SourePosition Pos = default
) : IStatement
{ 

    public IStatement? Initialiser = initialiser;
    public IExpression? Condition { get; set; } = Condition;

    public IStatement? Iterator = iterator;
    public List<IStatement> Body = body;
    public Scope Scope = scope;

    public SourePosition Pos { get; } = Pos;
}


public sealed class ReturnStatement(
    IExpression? Expression,
    SourePosition Pos = default
) : IStatement
{
    public IExpression? Expression { get; set; } = Expression;

    public SourePosition Pos { get; } = Pos;
}
#endregion

#region  Expressions

public sealed class MemberAccessExpression(
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

public sealed class CallExpression(
    IExpression Target,                // both functions and types
    List<IExpression> arguments,       // positional & named args
    SourePosition Pos = default,
    Scope? Scope = null
) : IExpression
{
    public IExpression Target { get; set; } = Target;
    public List<IExpression> Arguments = arguments;
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;
    public Scope? Scope { get; set; } = Scope;

    public SourePosition Pos { get; } = Pos;
}

public sealed class CastExpression(
    CastOp CastKind,
    IExpression target,
    SourePosition Pos = default
) : IExpression
{
    public TypeInfo? ResolvedType { get; set; }
    public IExpression Target = target;
    public bool Assignable => false;
    public CastOp CastKind { get; set; } = CastKind;

    public SourePosition Pos { get; } = Pos;
}

public sealed class InstantiationExpression(
    string typeName,
    List<IExpression> arguments,
    SourePosition Pos = default
) : IExpression
{
    public string TypeName = typeName;
    public List<IExpression> Arguments = arguments;
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;

    public SourePosition Pos { get; } = Pos;
}

public sealed class UnaryExpression(
    UnaryOperator op,
    IExpression Operand,
    SourePosition Pos = default,
    bool assignable = false
) : IExpression
{
    public UnaryOperator Op = op;
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

public sealed class BinaryExpression(
    IExpression Left,
    BinaryOperator op,    // "+", "*", ">", etc.
    IExpression Right,
    SourePosition Pos = default
)
 : IExpression
{
    public BinaryOperator Op = op;
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;
    public IExpression Left { get; set; } = Left;
    public IExpression Right { get; set; } = Right;

    public SourePosition Pos { get; } = Pos;
}

public sealed class LiteralExpression(
    object value,       // boxed int, float, bool
    string lexeme,
    SourePosition Pos = default
) : IExpression
{

    public object Value = value;
    public string Lexeme = lexeme;
    public TypeInfo? ResolvedType { get; set; }
    public bool Assignable => false;

    public SourePosition Pos { get; } = Pos;
}

public sealed class IdentifierExpression(
    string name,
    SourePosition Pos = default,
    Scope? Scope = null
) : IExpression
{
    public string Name = name;
    public TypeInfo? ResolvedType { get; set; }
    public Scope? Scope = Scope;
    public bool Assignable => true;

    public SourePosition Pos { get; } = Pos;
}

public sealed class IndexExpression(
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