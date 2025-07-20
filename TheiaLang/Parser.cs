namespace TheiaLang;

public enum SymbolKind
{
    Variable,
    Function,
    Type
}

public class SymbolInfo(string name,
                        TypeInfo type,
                        SymbolKind symbolKind,
                        List<TypeNamePair>? parameters)
{
    public string Name { get; init; } = name;
    public TypeInfo Type { get; init; } = type;
    public SymbolKind Kind { get; init; } = symbolKind;
    public List<TypeNamePair>? Parameters { get; init; } = parameters;
}

public class TypeInfo(string type,
                      List<string>? fieldNames,
                      List<string>? fieldTypes)
{
    public string TypeName { get; init; } = type;
    public List<string>? FieldNames { get; set; } = fieldNames;
    public List<string>? FieldTypes { get; set; } = fieldTypes;
}

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
    public Dictionary<string, SymbolInfo> Symbols { get; } = new Dictionary<string, SymbolInfo>();

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
    public void Declare(string name, SymbolInfo symbolInfo)
    {
        if (Symbols.ContainsKey(name))
            Log.Error(6, $"Identifier '{name}' already declared in the scope '{FullName}'");
        Symbols[name] = symbolInfo;
    }

    public bool TryLookup(string name, out SymbolInfo? symbolInfo, out Scope? symbolScope)
    {
        if (Symbols.TryGetValue(name, out symbolInfo))
        {
            symbolScope = this;
            return true;
        }
        else
        {
            if (Parent == null)
            {
                symbolInfo = null;
                symbolScope = null;
                return false;
            }
            else
                return Parent.TryLookup(name, out symbolInfo, out symbolScope);
        }
    }

    public bool GetParentOf(Scope scope, out Scope? parent)
    {
        if (Children.ContainsValue(scope))
        {
            parent = this;
            return true;
        }
        return Parent!.GetParentOf(scope, out parent);
    }

    public TypeInfo ResolveType(string typeName)
    {
        if (!TryLookup(typeName, out SymbolInfo? symbolInfo, out _))
            throw new Exception($"Unknown type '{typeName}' in scope '{FullName}'");

        if (symbolInfo!.Kind != SymbolKind.Type)
            throw new Exception($"'{typeName}' in scope '{FullName}' is not a type");

        return symbolInfo.Type;
    }
}


public class Parser(List<Token> tokens)
{
    int pos = 0;
    Scope? globalScope;
    Scope? currentScope;

    public (ProgramNode, Scope) ParseProgram(string ProgramName)
    {
        globalScope = new Scope("", null, null);
        currentScope = globalScope;   // global scope

        foreach (string builtinType in IRGenerator.BuiltinTypes)
            DeclareBuiltin(builtinType);

        List<INode> nodes = new List<INode>();
        while (!IsAtEnd())
            if (Match(TokenType.Keyword_struct))
                nodes.Add(ParseStructDeclaration());
            else if (Match(TokenType.Keyword_union))
                nodes.Add(ParseUnionDeclaration());
            else
                nodes.Add(ParseFunctionDeclaration());

        return (new ProgramNode(ProgramName, nodes), globalScope);
    }

    #region Declarations
    FunctionDeclaration ParseFunctionDeclaration()
    {
        Token returnTypeToken = ConsumeTypeKeyword();
        string returnType = TokenTypeToString(returnTypeToken.TokenType);

        Token nameToken = Consume(TokenType.Identifier, "Expected function name");
        string name = nameToken.Lexeme;

        Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after function name");
        List<TypeNamePair> parameters = new List<TypeNamePair>();
        List<IStatement> body = new List<IStatement>();

        FunctionDeclaration functionDeclaration = new FunctionDeclaration(returnType, name, parameters, body);

        EnterScope(name, null);
        if (!Check(TokenType.Punctuation_ParenthesisR))
        {
            do
            {
                Token typeToken = ConsumeTypeKeyword();
                string fieldType = TokenTypeToString(typeToken.TokenType);

                Token identifierToken = Consume(TokenType.Identifier, "Expected field name");
                TypeNamePair parameter = new TypeNamePair(fieldType, identifierToken.Lexeme);

                TypeInfo typeInfo = typeInfo = currentScope!.ResolveType(fieldType);

                currentScope!.Declare(identifierToken.Lexeme, new SymbolInfo(
                    identifierToken.Lexeme,
                    typeInfo,
                    SymbolKind.Variable,
                    null
                ));

                // Log.Info($"Parameter {fieldType} '{identifierToken.Lexeme}' defined in '{currentScope.FullName}'");
                parameters.Add(parameter);
            } while (Match(TokenType.Punctuation_Comma));
        }

        // we will not deal with this for now!!!

        Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after parameters");

        // initialise only with name, since we can't really mutate a Record later
        functionDeclaration.Scope = currentScope;
        currentScope!.DeclaringNode = functionDeclaration;
        body.AddRange(ParseBlock());

        // fill out AST reference
        ExitScope();

        TypeInfo returnTypeInfo = currentScope.ResolveType(functionDeclaration.ReturnType);

        currentScope.Declare(name,
                                new SymbolInfo(
                                functionDeclaration.Name,
                                returnTypeInfo,
                                SymbolKind.Function,
                                functionDeclaration.Parameters
                                ));

        return functionDeclaration;
    }

    StructDeclaration ParseStructDeclaration()
    {
        // we've already consumed 'struct'
        Token nameToken = Consume(TokenType.Identifier, "Expected struct name");
        string name = nameToken.Lexeme;

        Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after struct name");

        List<TypeNamePair> fields = new List<TypeNamePair>();
        List<string> fieldNames = new List<string>();
        List<string> fieldTypes = new List<string>();
        List<FunctionDeclaration> methods = new List<FunctionDeclaration>();

        StructDeclaration structDeclaration = new StructDeclaration(name, fields, methods);

        EnterScope(name);

        structDeclaration.Scope = currentScope;
        currentScope!.DeclaringNode = structDeclaration;

        if (!Check(TokenType.Punctuation_ParenthesisR))
        {
            do
            {
                Token typeToken = ConsumeTypeKeyword();
                string fieldType = TokenTypeToString(typeToken.TokenType);
                Token identifierToken = Consume(TokenType.Identifier, "Expected field name");
                TypeNamePair parameter = new TypeNamePair(fieldType, identifierToken.Lexeme);

                TypeInfo typeInfo = currentScope.ResolveType(fieldType);

                currentScope.Declare(identifierToken.Lexeme,
                                     new SymbolInfo(
                                        identifierToken.Lexeme,
                                        typeInfo,
                                        SymbolKind.Variable,
                                        null
                                     ));
                fields.Add(parameter);
                fieldNames.Add(parameter.Name);
                fieldTypes.Add(parameter.Type);
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
        SymbolInfo symbolInfo = new SymbolInfo(
            name,
            new TypeInfo(
                name,
                fieldNames,
                fieldTypes),
            SymbolKind.Type,
            fields);

        currentScope.Declare(name, symbolInfo);

        return structDeclaration;
    }

    UnionDeclaration ParseUnionDeclaration()
    {
        Token nameTok = Consume(TokenType.Identifier, "Expected union name");
        string name = nameTok.Lexeme;

        Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after union name");
        List<string> fieldNames = new List<string>();
        List<string> fieldTypes = new List<string>();
        List<TypeNamePair> variants = new List<TypeNamePair>();

        if (!Check(TokenType.Punctuation_ParenthesisR))
        {
            do
            {
                Token typeToken = ConsumeTypeKeyword();
                string type = TokenTypeToString(typeToken.TokenType);

                Token identifierToken = Consume(TokenType.Identifier, "Expected variant name");
                fieldNames.Add(identifierToken.Lexeme);
                fieldTypes.Add(type);
                variants.Add(new TypeNamePair(type, identifierToken.Lexeme));
            }
            while (Match(TokenType.Punctuation_Comma));
        }
        Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after variants");
        Consume(TokenType.Punctuation_Semicolon, "Expected ';' after union declaration");

        UnionDeclaration unionDeclaration = new UnionDeclaration(name, variants, name);
        currentScope!.Declare(name, new SymbolInfo(
            name,
            new TypeInfo(
                name,
                fieldNames,
                fieldTypes
            ),
            SymbolKind.Type,
            variants
        ));

        return unionDeclaration;
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
        if (Peek().TokenType == TokenType.Identifier
            && PeekNext().TokenType == TokenType.Identifier)
        {
            Token typeToken = Advance();
            string type = typeToken.Lexeme;     // composite type
            string varName = Advance().Lexeme;

            IExpression? init = null;
            if (Match(TokenType.Operator_Equals))
                init = ParseExpression();
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after variable declaration");
            VariableDeclaration variableDeclaration = new VariableDeclaration(type, varName, init);

            TypeInfo typeInfo = currentScope!.ResolveType(type);

            currentScope!.Declare(varName, new SymbolInfo(
                varName,
                typeInfo,
                SymbolKind.Variable,
                null
            ));
            return variableDeclaration;
        }

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

            string type = TokenTypeToString(typeToken.TokenType);

            VariableDeclaration variableDeclaration = new VariableDeclaration(type, nameToken.Lexeme, init);

            TypeInfo typeInfo = currentScope!.ResolveType(type);

            currentScope!.Declare(nameToken.Lexeme, new SymbolInfo(
                nameToken.Lexeme,
                typeInfo,
                SymbolKind.Variable,
                null
            ));
            return variableDeclaration;
        }

        // assignment: identifier '=' expr ';'
        if (Peek().TokenType == TokenType.Identifier
            && PeekNext().TokenType == TokenType.Operator_Equals)
        {
            return ParseAssignment();
        }

        int ret = pos;
        try
        {
            IExpression? lhs = ParseExpression();
            // Log.Info($"{lhs != null}\n{lhs}\n{Peek()}");

            if (lhs.Assignable && Peek().TokenType == TokenType.Operator_Equals)
                return ParseAssignment();
            else
                pos = ret;
        }
        catch
        {
            pos = ret;
        }

        if (Peek().TokenType == TokenType.Identifier
             && PeekNext().TokenType == TokenType.Punctuation_ParenthesisL)
        {
            IExpression expression = ParseExpression();
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after call");
            return new ExpressionStatement(expression);
        }

        Log.Error(1, $"Unexpected token {Peek().TokenType} '{Peek().Lexeme}' at {Peek().Line + 1}:{Peek().Column}");
        Environment.Exit(1);
        return null;
    }

    AssignmentStatement ParseAssignment()
    {
        Token nameToken = Advance();
        IdentifierExpression identifier = new IdentifierExpression(nameToken.Lexeme);

        Advance(); // consume '='
        IExpression expr = ParseExpression();
        Consume(TokenType.Punctuation_Semicolon, "Expected ';' after assignment");
        return new AssignmentStatement(identifier, expr);
    }
    #endregion

    #region  Expressions
    IExpression ParseExpression() => ParseComparison();

    IExpression ParseComparison()
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

    IExpression ParseAdditive()
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

    IExpression ParseMultiplicative()
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

    IExpression ParseUnary()
    {
        if (Match(TokenType.Operator_Minus))
        {
            // we’ve consumed the ‘-’
            IExpression operand = ParseUnary();
            return new UnaryExpression(UnaryOperator.Negate, operand);
        }
        return ParsePrimary();
    }

    IExpression ParsePrimary()
    {
        if (Match(TokenType.Keyword_new))
        {
            Token typeToken = Consume(TokenType.Identifier, "Expected type name after 'new'");
            string type = typeToken.Lexeme;   // composite type

            Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after type name");
            List<IExpression> arguments = new List<IExpression>();
            if (!Check(TokenType.Punctuation_ParenthesisR))
                do
                {
                    arguments.Add(ParseExpression());
                } while (Match(TokenType.Punctuation_Comma));
            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after arguments");
            return new InstantiationExpression(type, arguments);
        }

        if (Match(TokenType.Literal_integer) || Match(TokenType.Literal_floatingPoint) || Match(TokenType.Literal_Boolean))
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

        if (Peek().TokenType == TokenType.Identifier
            && PeekNext().TokenType == TokenType.Punctuation_ParenthesisL)
        {
            Token nameToken = Advance();
            Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after method call");

            List<IExpression> arguments = new List<IExpression>();
            if (!Check(TokenType.Punctuation_ParenthesisR))
                do
                {
                    arguments.Add(ParseExpression());
                } while (Match(TokenType.Punctuation_Comma));

            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after arguments");
            return new CallExpression(nameToken.Lexeme, arguments);
        }

        if (Peek().TokenType == TokenType.Identifier
            && PeekNext().TokenType == TokenType.Punctuation_Dot)
        {
            IdentifierExpression target = new IdentifierExpression(Advance().Lexeme);
            Consume(TokenType.Punctuation_Dot, "Expected '.' after member access target");
            IdentifierExpression member = new IdentifierExpression(Advance().Lexeme);
            return new MemberAccessExpression(target, member);
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
        return null!;
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

    static string TokenTypeToString(TokenType tokenType) => tokenType switch
    {
        TokenType.Literal_integer => "s32",     // TODO: make integer literals compatible with floating point numbers
        TokenType.Literal_floatingPoint => "f32", // TODO: explicit abstraction from width 
        TokenType.Literal_Boolean => "bool",

        TokenType.Keyword_bool => "bool",

        TokenType.Keyword_s8 => "s8",
        TokenType.Keyword_s16 => "s16",
        TokenType.Keyword_s32 => "s32",
        TokenType.Keyword_s64 => "s64",
        TokenType.Keyword_s128 => "s128",
        TokenType.Keyword_s256 => "s256",

        TokenType.Keyword_f16 => "f16",
        TokenType.Keyword_f32 => "f32",
        TokenType.Keyword_f64 => "f64",
        TokenType.Keyword_f128 => "f128",

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
        Log.Error(3, $"{message} at {Peek().Line + 1}:{Peek().Column}, got: {Peek().TokenType} '{Peek().Lexeme}'");
        Environment.Exit(1);
        return null;
    }

    Token ConsumeTypeKeyword()
    {
        Token t = Peek();
        if (IsTypeKeyword(t.TokenType)) return Advance();
        Log.Error(4, $"Unxpected type keyword '{t.Lexeme}' at {t.Line}:{t.Column}");
        return null!;
    }

    static bool IsTypeKeyword(TokenType tokenType)
        => tokenType == TokenType.Keyword_bool
        || tokenType == TokenType.Keyword_s8
        || tokenType == TokenType.Keyword_s16
        || tokenType == TokenType.Keyword_s32
        || tokenType == TokenType.Keyword_s64
        || tokenType == TokenType.Keyword_s128
        || tokenType == TokenType.Keyword_s256
        || tokenType == TokenType.Keyword_f16
        || tokenType == TokenType.Keyword_f32
        || tokenType == TokenType.Keyword_f64
        || tokenType == TokenType.Keyword_f128;

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
        if (currentScope!.Parent == null)
            throw new InvalidOperationException("Attempted to exit global scope");
        currentScope = currentScope.Parent;
    }

    void DeclareBuiltin(string typeName)
    {
        globalScope!.Declare(typeName,
                    new SymbolInfo(
                        typeName,
                        new TypeInfo(
                            typeName,
                            null,
                            null
                        ),
                        SymbolKind.Type,
                        null
                    ));
    }
    #endregion
}