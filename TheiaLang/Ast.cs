namespace TheiaLang;

public interface IStatement { }
public interface IExpression { }

public sealed record FunctionDeclaration(
    string ReturnType,          // "int", "float", "bool"
    string Name,                // e.g. "main"
    List<Parameter> Parameters, // empty for now
    BlockSatement Body          // the { … } body
);

public sealed record ProgramNode(
    List<FunctionDeclaration> Functions
);

public sealed record Parameter(
    string Type,
    string Name
);

#region  statements
public sealed record BlockSatement(
    List<IStatement> Statements
) : IStatement;

public sealed record VariableDeclarationStatement(
    string VarType,             // "int", "float", "bool"
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
public sealed record BinaryExpression(
    IExpression Left,
    string Op,    // "+", "*", ">", etc.
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