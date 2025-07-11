namespace TheiaLang;

public class Parser(List<Token> tokens)
{
    int pos = 0;

    public ProgramNode ParseProgram()
    {
        List<INode> nodes = new List<INode>();
        while (!IsAtEnd())
            nodes.Add(ParseFunction());

        return new ProgramNode(nodes);
    }

    FunctionDeclaration ParseFunction()
    {
        Token retTypeToken = ConsumeTypeKeyword();
        Type returnType = TokenTypeToType(retTypeToken.Type);

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
        if (IsTypeKeyword(Peek().Type))
        {
            Token typeToken = Advance();
            Token nameToken = Consume(TokenType.Identifier, "Expected variable name");
            IExpression? init = null;
            if (Match(TokenType.Operator_Equals))
                init = ParseExpression();
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after declaration");

            Type type = TokenTypeToType(typeToken.Type);

            return new VariableDeclarationStatement(type, nameToken.Lexeme, init);
        }

        // assignment: identifier '=' expr ';'
        if (Peek().Type == TokenType.Identifier && PeekNext().Type == TokenType.Operator_Equals)
        {
            Token nameTok = Advance();
            Advance(); // consume '='
            IExpression expr = ParseExpression();
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after assignment");
            return new AssignmentStatement(nameTok.Lexeme, expr);
        }

        Log.Error($"Unexpected token {Peek().Type} '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");
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

            BinaryOperator binaryOperatorType = OperatorTypeToType(op.Type);

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
            BinaryOperator binaryOperatorType = OperatorTypeToType(op.Type);

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
            var op = Previous().Type == TokenType.Operator_Mult
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

        Log.Error($"Unexpected token {Peek().Type} in expression");
        Environment.Exit(1);
        return null;
    }

    #region Conversion
    static BinaryOperator OperatorTypeToType(TokenType tokenType)
    {
        return tokenType switch
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
    }

    static Type TokenTypeToType(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Literal_s32 => Type.s32,
            TokenType.Keyword_s32 => Type.s32,
            TokenType.Literal_f32 => Type.f32,
            TokenType.Keyword_f32 => Type.f32,
            TokenType.Literal_bool => Type.Bool,
            TokenType.Keyword_bool => Type.Bool,
            _ => throw new Exception($"Unsupported Type '{tokenType}'"),
        };
    }
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
        Log.Error($"{message} at {Peek().Line}:{Peek().Column}");
        Environment.Exit(1);
        return null;
    }

    Token ConsumeTypeKeyword()
    {
        Token t = Peek();
        if (IsTypeKeyword(t.Type)) return Advance();
        Log.Error($"Expected type keyword at {t.Line}:{t.Column}");
        Environment.Exit(1);
        return null;
    }

    static bool IsTypeKeyword(TokenType t)
        => t == TokenType.Keyword_s32
        || t == TokenType.Keyword_f32
        || t == TokenType.Keyword_bool;

    bool Check(TokenType type)
        => !IsAtEnd() && Peek().Type == type;

    Token Advance()
        => pos < tokens.Count ? tokens[pos++] : tokens[^1];

    bool IsAtEnd()
        => Peek().Type == TokenType.EOF;

    Token Peek()
        => tokens[pos];

    Token PeekNext()
        => pos + 1 < tokens.Count ? tokens[pos + 1] : tokens[^1];

    Token Previous()
        => tokens[pos - 1];
    #endregion
}