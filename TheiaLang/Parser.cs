namespace TheiaLang;

public class Parser(List<Token> tokens)
{
    int pos = 0;

    public ProgramNode ParseProgram()
    {
        List<INode> nodes = new List<INode>();
        while (!IsAtEnd())
            if (Match(TokenType.Keyword_struct))
                nodes.Add(ParseStructDeclaration());
            else
                nodes.Add(ParseFunctionDeclaration());

        return new ProgramNode(nodes);
    }

    FunctionDeclaration ParseFunctionDeclaration()
    {
        Token returnTypeToken = ConsumeTypeKeyword();
        Type returnType = TokenTypeToType(returnTypeToken.TokenType);

        Token nameToken = Consume(TokenType.Identifier, "Expected function name");
        string name = nameToken.Lexeme;

        Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after function name");
        List<Parameter> parameters = new List<Parameter>();

        if (!Check(TokenType.Punctuation_ParenthesisR))
            Console.WriteLine("We actually do have parameters, how unexpected!");

        Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after parameters");

        BlockSatement body = ParseBlock();

        return new FunctionDeclaration(returnType, name, parameters, body);
    }

    StructDeclaration ParseStructDeclaration()
    {
        // we've already consumed 'struct'
        var nameTok = Consume(TokenType.Identifier, "Expected struct name");
        var name = nameTok.Lexeme;

        Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after struct name");

        var fields = new List<Parameter>();
        if (!Check(TokenType.Punctuation_ParenthesisR))
        {
            do
            {
                // reuse your func‐param logic
                var typeTok = ConsumeTypeKeyword();
                var fieldType = TokenTypeToType(typeTok.TokenType);
                var idTok = Consume(TokenType.Identifier, "Expected field name");
                fields.Add(new Parameter(fieldType, idTok.Lexeme));
            } while (Match(TokenType.Punctuation_Comma));
        }
        Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after struct fields");

        // either semicolon‐form or brace‐form
        var methods = new List<FunctionDeclaration>();
        if (Match(TokenType.Punctuation_Semicolon))
        {
            // no methods
        }
        else
        {
            Consume(TokenType.Punctuation_BraceL, "Expected '{' to start struct body");
            while (!Check(TokenType.Punctuation_BraceR) && !IsAtEnd())
                methods.Add(ParseFunctionDeclaration());
            Consume(TokenType.Punctuation_BraceR, "Expected '}' after struct body");
        }

        return new StructDeclaration(name, fields, methods);
    }

    BlockSatement ParseBlock()
    {
        Consume(TokenType.Punctuation_BraceL, "Expected '{' to start block");
        List<IStatement> statements = new List<IStatement>();

        while (!Check(TokenType.Punctuation_BraceR) && !IsAtEnd())
            statements.Add(ParseStatement());

        Consume(TokenType.Punctuation_BraceR, "Expected '}' after block");
        return new BlockSatement(statements);
    }

    IStatement ParseStatement()
    {
        if (Match(TokenType.Keyword_return))
        {
            var expr = ParseExpression();
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

            return new VariableDeclarationStatement(type, nameToken.Lexeme, init);
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

    private IExpression ParseExpression() => ParseComparison();

    private IExpression ParseComparison()
    {
        var expr = ParseAdditive();
        while (Match(TokenType.Operator_Greater) || Match(TokenType.Operator_Less))
        {
            Token op = Previous();

            BinaryOperator binaryOperatorType = OperatorTypeToType(op.TokenType);

            var right = ParseAdditive();
            expr = new BinaryExpression(expr, binaryOperatorType, right);
        }
        return expr;
    }

    private IExpression ParseAdditive()
    {
        var expr = ParseMultiplicative();
        while (Match(TokenType.Operator_Plus) || Match(TokenType.Operator_Minus))
        {
            Token op = Previous();
            BinaryOperator binaryOperatorType = OperatorTypeToType(op.TokenType);

            var right = ParseMultiplicative();
            expr = new BinaryExpression(expr, binaryOperatorType, right);
        }
        return expr;
    }

    private IExpression ParseMultiplicative()
    {
        var expr = ParseUnary();
        while (Match(TokenType.Operator_Mult) || Match(TokenType.Operator_Div))
        {
            var op = Previous().TokenType == TokenType.Operator_Mult
                ? BinaryOperator.Multiply
                : BinaryOperator.Divide;
            var right = ParseUnary();
            expr = new BinaryExpression(expr, op, right);
        }
        return expr;
    }

    private IExpression ParseUnary()
    {
        if (Match(TokenType.Operator_Minus))
        {
            // we’ve consumed the ‘-’
            var operand = ParseUnary();
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
                string s when int.TryParse(s, out var i) => i,
                string s when double.TryParse(s, out var d) => d,
                string s when bool.TryParse(s, out var b) => b,
                _ => throw new Exception("Invalid literal")
            };

            return new LiteralExpression(v, Previous().Lexeme);
        }

        if (Match(TokenType.Identifier))
            return new IdentifierExpression(Previous().Lexeme);

        if (Match(TokenType.Punctuation_ParenthesisL))
        {
            var inner = ParseExpression();
            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after expression");
            return inner;
        }

        Log.Error(2, $"Unexpected token {Peek().TokenType} in expression");
        Environment.Exit(1);
        return null;
    }

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
    #endregion
}