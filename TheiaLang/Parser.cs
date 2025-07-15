namespace TheiaLang;

public class Scope : INode
{
    public string Name;
    public string FullName =>
        Parent == null || string.IsNullOrEmpty(Parent.FullName)
            ? Name
            : $"{Parent.FullName}.{Name}";
    public INode? DeclaringNode;
    public Scope? Parent { get; }
    public Dictionary<string, Scope> Children { get; } = new Dictionary<string, Scope>();
    public Dictionary<string, INode> Symbols { get; } = new Dictionary<string, INode>();

    public Scope(string name, INode? declaringNode, Scope? parent)
    {
        Name = name;
        DeclaringNode = declaringNode;
        Parent = parent;
        if (parent == null)
            return;

        if (!parent.Children.ContainsKey(name))
            parent.Children.Add(name, this);
        else
            Log.Error(7, $"Identifier '{name}' already declared in the scope '{Parent!.FullName}'");
    }

    // convenience for parser when you hit a declaration
    public void Declare(string name, INode node)
    {
        if (Symbols.ContainsKey(name))
            Log.Error(6, $"Identifier '{name}' already declared in the scope '{FullName}'");
        Symbols[name] = node;
    }
}

public class Parser(List<Token> tokens)
{
    int pos = 0;
    Scope globalScope;
    Scope currentScope;

    public (ProgramNode, Scope) ParseProgram()
    {
        globalScope = new Scope("", null, null);
        currentScope = globalScope;   // global scope

        List<INode> nodes = new List<INode>();
        while (!IsAtEnd())
            if (Match(TokenType.Keyword_struct))
                nodes.Add(ParseStructDeclaration());
            else
                nodes.Add(ParseFunctionDeclaration());

        return (new ProgramNode(nodes), globalScope);
    }

    #region Declarations
    FunctionDeclaration ParseFunctionDeclaration()
    {
        Token returnTypeToken = ConsumeTypeKeyword();
        Type returnType = TokenTypeToType(returnTypeToken.TokenType);

        Token nameToken = Consume(TokenType.Identifier, "Expected function name");
        string name = nameToken.Lexeme;

        Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after function name");
        List<Parameter> parameters = new List<Parameter>();
        List<IStatement> body = new List<IStatement>();

        FunctionDeclaration functionDeclaration = new FunctionDeclaration(returnType, name, parameters, body);

        if (!Check(TokenType.Punctuation_ParenthesisR))
            Console.WriteLine("We actually do have parameters, how unexpected!");
        // we will not deal with this for now!!!

        Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after parameters");

        // initialise only with name, since we can't really mutate a Record later
        EnterScope(name, null);
        functionDeclaration.Scope = currentScope;
        currentScope.DeclaringNode = functionDeclaration;
        body.AddRange(ParseBlock());

        // fill out AST reference
        ExitScope();
        currentScope.Declare(name, functionDeclaration);

        return functionDeclaration;
    }

    StructDeclaration ParseStructDeclaration()
    {
        // we've already consumed 'struct'
        Token nameToken = Consume(TokenType.Identifier, "Expected struct name");
        string name = nameToken.Lexeme;

        Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after struct name");


        List<Parameter> fields = new List<Parameter>();
        List<FunctionDeclaration> methods = new List<FunctionDeclaration>();

        StructDeclaration structDeclaration = new StructDeclaration(name, fields, methods);
        EnterScope(name);
        structDeclaration.Scope = currentScope;
        currentScope.DeclaringNode = structDeclaration;

        if (!Check(TokenType.Punctuation_ParenthesisR))
        {
            do
            {
                Token typeToken = ConsumeTypeKeyword();
                Type fieldType = TokenTypeToType(typeToken.TokenType);
                Token identifierToken = Consume(TokenType.Identifier, "Expected field name");
                Parameter parameter = new Parameter(fieldType, identifierToken.Lexeme);
                currentScope.Declare(identifierToken.Lexeme, parameter);
                fields.Add(parameter);
            } while (Match(TokenType.Punctuation_Comma));
        }
        Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after struct fields");

        // either semicolon‐form or brace‐form

        if (!Match(TokenType.Punctuation_Semicolon))
        {
            Consume(TokenType.Punctuation_BraceL, "Expected '{' to start struct body");
            // initialise only with name
            while (!Check(TokenType.Punctuation_BraceR) && !IsAtEnd())
            {
                FunctionDeclaration functionDeclaration = ParseFunctionDeclaration();
                methods.Add(functionDeclaration);
            }
            Consume(TokenType.Punctuation_BraceR, "Expected '}' after struct body");
        }

        ExitScope();
        currentScope.Declare(name, structDeclaration);

        return structDeclaration;
    }
    #endregion
    #region Statements

    List<IStatement> ParseBlock()
    {
        Consume(TokenType.Punctuation_BraceL, "Expected '{' to start block");
        List<IStatement> statements = new List<IStatement>();

        while (!Check(TokenType.Punctuation_BraceR) && !IsAtEnd())
            statements.Add(ParseStatement());

        Consume(TokenType.Punctuation_BraceR, "Expected '}' after block");
        return statements;
    }

    IStatement ParseStatement()
    {
        if (Match(TokenType.Keyword_return))
        {
            IExpression expr = ParseExpression();
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after return value");
            return new ReturnStatement(expr);
        }

        // variable declaration?
        if (IsTypeKeyword(Peek().TokenType))
        {
            Token typeToken = Advance();
            Token nameToken = Consume(TokenType.Identifier, "Expected variable name");
            IExpression? init = null;
            if (Match(TokenType.Operator_Equals))
                init = ParseExpression();
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after declaration");

            Type type = TokenTypeToType(typeToken.TokenType);

            VariableDeclaration variableDeclaration = new VariableDeclaration(type, nameToken.Lexeme, init);
            currentScope.Declare(nameToken.Lexeme, variableDeclaration);
            return variableDeclaration;
        }

        // assignment: identifier '=' expr ';'
        if (Peek().TokenType == TokenType.Identifier && PeekNext().TokenType == TokenType.Operator_Equals)
        {
            Token nameTok = Advance();
            Advance(); // consume '='
            IExpression expr = ParseExpression();
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after assignment");
            return new AssignmentStatement(nameTok.Lexeme, expr);
        }

        Log.Error(1, $"Unexpected token {Peek().TokenType} '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");
        Environment.Exit(1);
        return null;
    }
    #endregion

    #region  Expressions
    private IExpression ParseExpression() => ParseComparison();

    private IExpression ParseComparison()
    {
        IExpression expr = ParseAdditive();
        while (Match(TokenType.Operator_Greater) || Match(TokenType.Operator_Less))
        {
            Token op = Previous();

            BinaryOperator binaryOperatorType = OperatorTypeToType(op.TokenType);

            IExpression right = ParseAdditive();
            expr = new BinaryExpression(expr, binaryOperatorType, right);
        }
        return expr;
    }

    private IExpression ParseAdditive()
    {
        IExpression expr = ParseMultiplicative();
        while (Match(TokenType.Operator_Plus) || Match(TokenType.Operator_Minus))
        {
            Token op = Previous();
            BinaryOperator binaryOperatorType = OperatorTypeToType(op.TokenType);

            IExpression right = ParseMultiplicative();
            expr = new BinaryExpression(expr, binaryOperatorType, right);
        }
        return expr;
    }

    private IExpression ParseMultiplicative()
    {
        IExpression expr = ParseUnary();
        while (Match(TokenType.Operator_Mult) || Match(TokenType.Operator_Div))
        {
            BinaryOperator op = Previous().TokenType == TokenType.Operator_Mult
                ? BinaryOperator.Multiply
                : BinaryOperator.Divide;
            IExpression right = ParseUnary();
            expr = new BinaryExpression(expr, op, right);
        }
        return expr;
    }

    private IExpression ParseUnary()
    {
        if (Match(TokenType.Operator_Minus))
        {
            // we’ve consumed the ‘-’
            IExpression operand = ParseUnary();
            return new UnaryExpression(UnaryOperator.Negate, operand);
        }
        return ParsePrimary();
    }

    private IExpression ParsePrimary()
    {
        if (Match(TokenType.Literal_s32) || Match(TokenType.Literal_f32) || Match(TokenType.Literal_bool))
        {
            object v;

            v = Previous().Lexeme switch
            {
                string s when int.TryParse(s, out int i) => i,
                string s when double.TryParse(s, out double d) => d,
                string s when bool.TryParse(s, out bool b) => b,
                _ => throw new Exception("Invalid literal")
            };

            return new LiteralExpression(v, Previous().Lexeme);
        }

        if (Match(TokenType.Identifier))
        {
            string name = Previous().Lexeme;
            IdentifierExpression identifierExpression = new IdentifierExpression(name);

            return identifierExpression;
        }

        if (Match(TokenType.Punctuation_ParenthesisL))
        {
            IExpression inner = ParseExpression();
            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after expression");
            return inner;
        }

        Log.Error(2, $"Unexpected token {Peek().TokenType} in expression");
        Environment.Exit(1);
        return null;
    }
    #endregion

    #region Conversion
    static BinaryOperator OperatorTypeToType(TokenType tokenType) => tokenType switch
    {
        TokenType.Operator_Plus => BinaryOperator.Add,
        TokenType.Operator_Minus => BinaryOperator.Subtract,
        TokenType.Operator_Mult => BinaryOperator.Multiply,
        TokenType.Operator_Div => BinaryOperator.Divide,
        TokenType.Operator_Equals => BinaryOperator.Equal,
        TokenType.Operator_Greater => BinaryOperator.Greater,
        TokenType.Operator_Less => BinaryOperator.Less,
        _ => throw new Exception($"Can't parse '{tokenType}' as Binary Operator"),
    };

    static Type TokenTypeToType(TokenType tokenType) => tokenType switch
    {
        TokenType.Literal_s32 => Type.s32,
        TokenType.Keyword_s32 => Type.s32,
        TokenType.Literal_f32 => Type.f32,
        TokenType.Keyword_f32 => Type.f32,
        TokenType.Literal_bool => Type.Bool,
        TokenType.Keyword_bool => Type.Bool,
        _ => throw new Exception($"Unsupported Type '{tokenType}'"),
    };
    #endregion

    #region Helpers
    bool Match(TokenType type)
    {
        if (Check(type)) { Advance(); return true; }
        return false;
    }

    Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        Log.Error(3, $"{message} at {Peek().Line}:{Peek().Column}");
        Environment.Exit(1);
        return null;
    }

    Token ConsumeTypeKeyword()
    {
        Token t = Peek();
        if (IsTypeKeyword(t.TokenType)) return Advance();
        Log.Error(4, $"Unxpected type keyword '{t.Lexeme}' at {t.Line}:{t.Column}");
        return null;
    }

    static bool IsTypeKeyword(TokenType t)
        => t == TokenType.Keyword_s32
        || t == TokenType.Keyword_f32
        || t == TokenType.Keyword_bool;

    bool Check(TokenType type)
        => !IsAtEnd() && Peek().TokenType == type;

    Token Advance()
        => pos < tokens.Count ? tokens[pos++] : tokens[^1];

    bool IsAtEnd()
        => Peek().TokenType == TokenType.EOF;

    Token Peek()
        => tokens[pos];

    Token PeekNext()
        => pos + 1 < tokens.Count ? tokens[pos + 1] : tokens[^1];

    Token Previous()
        => tokens[pos - 1];

    void EnterScope(string name, INode? declaringNode = null)
    {
        currentScope = new Scope(name, declaringNode, currentScope);
    }

    void ExitScope()
    {
        if (currentScope.Parent == null)
            throw new InvalidOperationException("Attempted to exit global scope");
        currentScope = currentScope.Parent;
    }
    #endregion
}