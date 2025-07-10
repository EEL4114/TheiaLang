namespace TheiaLang;

public class Parser(List<Token> tokens)
{
    int pos = 0;

    public ProgramNode ParseProgram()
    {
        List<FunctionDeclaration> functions = new List<FunctionDeclaration>();
        while (!IsAtEnd())
            functions.Add(ParseFunction());

        return new ProgramNode(functions);
    }

    FunctionDeclaration ParseFunction()
    {
        Token retTypeToken = ConsumeTypeKeyword();
        string returnType = retTypeToken.Lexeme;

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
            Token typeTok = Advance();
            Token nameTok = Consume(TokenType.Identifier, "Expected variable name");
            IExpression? init = null;
            if (Match(TokenType.Operator_Equals))
                init = ParseExpression();
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after declaration");
            return new VariableDeclarationStatement(typeTok.Lexeme, nameTok.Lexeme, init);
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

        Log.Error($"Unexpected token {Peek().Type} at {Peek().Line}:{Peek().Column}");
        Environment.Exit(1);
        return null;
    }

    private IExpression ParseExpression() => ParseComparison();

    private IExpression ParseComparison()
    {
        var expr = ParseAdditive();
        while (Match(TokenType.Operator_Greater) || Match(TokenType.Operator_Less))
        {
            string op = Previous().Lexeme;
            var right = ParseAdditive();
            expr = new BinaryExpression(expr, op, right);
        }
        return expr;
    }

    private IExpression ParseAdditive()
    {
        var expr = ParseMultiplicative();
        while (Match(TokenType.Operator_Plus) /*|| Match(TokenType.Operator_Minus)*/)
        {
            string op = Previous().Lexeme;
            var right = ParseMultiplicative();
            expr = new BinaryExpression(expr, op, right);
        }
        return expr;
    }

    private IExpression ParseMultiplicative()
    {
        var expr = ParsePrimary();
        while (Match(TokenType.Operator_Mult) /*|| Match(TokenType.Operator_Slash)*/)
        {
            string op = Previous().Lexeme;
            var right = ParsePrimary();
            expr = new BinaryExpression(expr, op, right);
        }
        return expr;
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


    private bool Match(TokenType type)
    {
        if (Check(type)) { Advance(); return true; }
        return false;
    }

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        Log.Error($"{message} at {Peek().Line}:{Peek().Column}");
        Environment.Exit(1);
        return null;
    }

    private Token ConsumeTypeKeyword()
    {
        Token t = Peek();
        if (IsTypeKeyword(t.Type)) return Advance();
        Log.Error($"Expected type keyword at {t.Line}:{t.Column}");
        Environment.Exit(1);
        return null;
    }

    private static bool IsTypeKeyword(TokenType t)
        => t == TokenType.Keyword_int
        || t == TokenType.Keyword_float
        || t == TokenType.Keyword_bool;

    private bool Check(TokenType type)
        => !IsAtEnd() && Peek().Type == type;

    private Token Advance()
        => pos < tokens.Count ? tokens[pos++] : tokens[^1];

    private bool IsAtEnd()
        => Peek().Type == TokenType.EOF;

    private Token Peek()
        => tokens[pos];

    private Token PeekNext()
        => pos + 1 < tokens.Count ? tokens[pos + 1] : tokens[^1];

    private Token Previous()
        => tokens[pos - 1];
}