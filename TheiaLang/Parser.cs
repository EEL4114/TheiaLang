using LLVMSharp.Interop;

namespace TheiaLang;

public class Parser(List<Token> tokens)
{
    int pos = 0;

    Scope? globalScope;
    Scope? currentScope;

    public (ProgramNode, Scope) ParseProgram(string ProgramName)
    {
        globalScope = new Scope("");
        currentScope = globalScope;   // global scope

        foreach (string builtinType in IRGenerator.BuiltinTypes)
            DeclareBuiltin(builtinType);

        List<INode> nodes = [];
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
        List<TypeNamePair> parameters = [];
        List<IStatement> body = [];

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

        TypeInfo returnTypeInfo = new TypeInfo(functionDeclaration.TypeName);

        currentScope.Declare(name,
                             new SymbolInfo(
                             functionDeclaration.Name,
                             returnTypeInfo,
                             SymbolKind.Function,
                             functionDeclaration.Arguments
                             ));

        return functionDeclaration;
    }

    StructDeclaration ParseStructDeclaration()
    {
        // we've already consumed 'struct'
        Token nameToken = Consume(TokenType.Identifier, "Expected struct name");
        string name = nameToken.Lexeme;

        Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after struct name");

        List<TypeNamePair> fields = [];
        List<string> fieldNames = [];
        List<string> fieldTypes = [];
        List<FunctionDeclaration> methods = [];

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

                TypeInfo typeInfo = new TypeInfo(fieldType);
                currentScope.Declare(identifierToken.Lexeme,
                                     new SymbolInfo(
                                        identifierToken.Lexeme,
                                        typeInfo,
                                        SymbolKind.Variable,
                                        null
                                     ));

                fields.Add(parameter);
                fieldNames.Add(parameter.Name);
                fieldTypes.Add(parameter.TypeName);
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
        structDeclaration.ResolvedType = new TypeInfo(
            name,
            fieldNames: fieldNames,
            fieldTypes: fieldTypes);

        SymbolInfo symbolInfo = new SymbolInfo(
            name,
            structDeclaration.ResolvedType,
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
        List<string> fieldNames = [];
        List<string> fieldTypes = [];
        List<TypeNamePair> variants = [];

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

        TypeInfo unionInfo = new TypeInfo(
            name,
            fieldNames: fieldNames,
            fieldTypes: fieldTypes);

        UnionDeclaration unionDeclaration = new UnionDeclaration(name, variants, unionInfo);
        currentScope!.Declare(name, new SymbolInfo(
                              name,
                              unionInfo,
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
        List<IStatement> statements = [];

        while (!Check(TokenType.Punctuation_BraceR) && !IsAtEnd())
            statements.Add(ParseStatement());

        Consume(TokenType.Punctuation_BraceR, "Expected '}' after block");
        return statements;
    }

    IStatement ParseStatement(bool requireSemicolon = true)
    {
        if (Peek().TokenType == TokenType.Identifier
            && PeekNext().TokenType == TokenType.Identifier)
        {
            Token typeToken = Advance();
            string type = typeToken.Lexeme;     // composite type

            List<int> lengths = [];
            while (Peek().TokenType == TokenType.Punctuation_BracketL)  // array
            {
                Advance();  // '['
                // TODO adapt this for n-Dimensional arrays
                Token lengthToken = Consume(TokenType.Literal, $"Expected array length {PrintCurrentPos()}");
                lengths.Add(int.Parse(lengthToken.Lexeme));
                Consume(TokenType.Punctuation_BracketR, $"Expected ']' {PrintCurrentPos()}");
            }

            string varName = Advance().Lexeme;

            IExpression? init = null;
            if (Match(TokenType.Operator_Equal))
                init = ParseExpression();
            if (requireSemicolon)
                Consume(TokenType.Punctuation_Semicolon, "Expected ';' after variable declaration");

            TypeInfo typeInfo = new TypeInfo(type);
            if (lengths != null)
            {
                foreach (int length in lengths)
                    type += $"[{length}]";

                typeInfo = new TypeInfo($"{typeInfo.TypeName}{type}",
                                           elementType: typeInfo,
                                           arrayLengths: lengths);
            }

            VariableDeclaration variableDeclaration = new VariableDeclaration(type, varName, init);

            currentScope!.Declare(varName, new SymbolInfo(
                varName,
                typeInfo,
                SymbolKind.Variable,
                null
            ));

            return variableDeclaration;
        }

        if (Peek().TokenType == TokenType.Punctuation_At
            && Peek().TokenType == TokenType.Identifier
            && PeekNextNext().TokenType == TokenType.Identifier)
        {
            Token typeToken = Advance();
            string type = typeToken.Lexeme;     // composite type
            string varName = Advance().Lexeme;

            IExpression? init = null;
            if (Match(TokenType.Operator_Equal))
                init = ParseExpression();
            if (requireSemicolon)
                Consume(TokenType.Punctuation_Semicolon, "Expected ';' after variable declaration");
            VariableDeclaration variableDeclaration = new VariableDeclaration("@" + type, varName, init);


            TypeInfo typeInfo = new TypeInfo("@" + type,
                                             pointee: new TypeInfo(type));

            currentScope!.Declare(varName, new SymbolInfo(
                varName,
                typeInfo,
                SymbolKind.Variable,
                null
            ));

            return variableDeclaration;
        }

        if (Match(TokenType.Keyword_if))
        {
            Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after if statement");
            IExpression condition = ParseExpression();
            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' to close condition of if statement");

            EnterScope($"if_then{pos}");
            List<IStatement> thenBranch = ParseBlock();
            Scope thenScope = currentScope!;
            Scope? elseScope = null;
            ExitScope();
            List<IStatement>? elseBranch = null;
            if (Peek().TokenType == TokenType.Keyword_else)
            {
                EnterScope($"if_else{pos}");
                elseScope = currentScope;
                Consume(TokenType.Keyword_else, "");
                elseBranch = ParseBlock();
                ExitScope();
            }

            return new IfStatement(condition, thenBranch, elseBranch, thenScope, elseScope);
        }

        if (Match(TokenType.Keyword_for))
        {
            EnterScope($"for_{pos}");
            Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after for keyword");
            IStatement initialiser = ParseStatement();
            IExpression condition = ParseExpression();
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after loop condition");
            IStatement iterator = ParseStatement(requireSemicolon: false);
            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after loop head");

            Scope bodyScope = currentScope!;
            List<IStatement> body = ParseBlock();
            ExitScope();

            return new ForStatement(initialiser, condition, iterator, body, bodyScope);
        }

        if (Match(TokenType.Keyword_return))
        {
            IExpression expr = ParseExpression();
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after return value");
            return new ReturnStatement(expr);
        }

        // variable declaration?
        if (IsBuiltinTypeKeyword(Peek().TokenType))
        {
            Token nameToken;
            VariableDeclaration variableDeclaration;
            Token typeToken = Advance();

            List<int> lengths = [];
            while (Peek().TokenType == TokenType.Punctuation_BracketL)  // array
            {
                Advance();  // '['
                // TODO adapt this for n-Dimensional arrays
                Token lengthToken = Consume(TokenType.Literal, $"Expected array length {PrintCurrentPos()}");
                lengths.Add(int.Parse(lengthToken.Lexeme));
                Consume(TokenType.Punctuation_BracketR, $"Expected ']' {PrintCurrentPos()}");
            }

            nameToken = Consume(TokenType.Identifier, "Expected variable name");
            IExpression? init = null;
            if (Match(TokenType.Operator_Equal))
                init = ParseExpression();
            if (requireSemicolon)
                Consume(TokenType.Punctuation_Semicolon, "Expected ';' after declaration");

            string type = TokenTypeToString(typeToken.TokenType);

            TypeInfo typeInfo = new TypeInfo(type);
            if (lengths != null)
            {
                foreach (int length in lengths)
                    type += $"[{length}]";

                typeInfo = new TypeInfo($"{type}",
                                        elementType: typeInfo,
                                        arrayLengths: lengths);
            }
            variableDeclaration = new VariableDeclaration(type, nameToken.Lexeme, init);

            variableDeclaration.ResolvedType = typeInfo;

            currentScope!.Declare(nameToken.Lexeme, new SymbolInfo(
                nameToken.Lexeme,
                variableDeclaration.ResolvedType,
                SymbolKind.Variable,
                null
            ));
            return variableDeclaration;
        }

        if (Peek().TokenType == TokenType.Punctuation_At
            && IsBuiltinTypeKeyword(PeekNext().TokenType))
        {
            Advance();  // '@'
            Token typeToken = Advance();
            Token nameToken = Consume(TokenType.Identifier, "Expected variable name");
            IExpression? init = null;
            if (Match(TokenType.Operator_Equal))
                init = ParseExpression();
            if (requireSemicolon)
                Consume(TokenType.Punctuation_Semicolon, "Expected ';' after declaration");

            string type = TokenTypeToString(typeToken.TokenType);

            VariableDeclaration variableDeclaration = new VariableDeclaration("@" + type, nameToken.Lexeme, init);
            TypeInfo typeInfo = new TypeInfo("@" + type,
                                             pointee: new TypeInfo(type));
            variableDeclaration.ResolvedType = typeInfo;


            currentScope!.Declare(nameToken.Lexeme, new SymbolInfo(
                nameToken.Lexeme,
                variableDeclaration.ResolvedType,
                SymbolKind.Variable,
                null
            ));
            // Log.Info(variableDeclaration.ResolvedType.TypeName + " " + variableDeclaration.Name);
            return variableDeclaration;
        }

        // assignment: identifier '=' expr ';'
        if (Peek().TokenType == TokenType.Identifier
            && PeekNext().TokenType == TokenType.Operator_Equal)
        {
            return ParseAssignment(requireSemicolon);
        }

        int ret = pos;
        try
        {
            IExpression? lhs = ParseExpression();
            if (lhs.Assignable)
            {
                BinaryOperator? op = null;
                // Compound assignment
                TokenType next = Peek().TokenType;
                if (next == TokenType.Operator_PlusEqual
                    || next == TokenType.Operator_MinusEqual
                    || next == TokenType.Operator_MultEqual
                    || next == TokenType.Operator_DivEqual)
                {
                    op = OperatorTypeToType(next);
                    Consume(Peek().TokenType, "");
                }
                else
                    Consume(TokenType.Operator_Equal, "expected '=' after expression");
                IExpression? rhs = ParseExpression();
                if (requireSemicolon)
                    Consume(TokenType.Punctuation_Semicolon, "Expected ';' after assignment");
                if (op != null)
                    return new CompoundAssignmentStatement(lhs, rhs, (BinaryOperator)op);
                return new AssignmentStatement(lhs, rhs);
            }
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
            if (requireSemicolon)
                Consume(TokenType.Punctuation_Semicolon, "Expected ';' after call");
            return new ExpressionStatement(expression);
        }

        Log.Error(1, $"Unexpected token {Peek().TokenType} '{Peek().Lexeme}' {PrintCurrentPos()}");
        return null!;
    }

    AssignmentStatement ParseAssignment(bool requireSemicolon = true)
    {
        Token nameToken = Advance();
        // Log.Info(nameToken.Lexeme);

        IdentifierExpression identifier = new IdentifierExpression(nameToken.Lexeme);

        Advance();  // consume '='
        IExpression expr = ParseExpression();
        if (requireSemicolon)
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after assignment");
        return new AssignmentStatement(identifier, expr);
    }
    #endregion

    #region  Expressions
    IExpression ParseExpression() => ParseOR();

    IExpression ParseOR()
    {
        IExpression left = ParseAND();
        while (Match(TokenType.Operator_OR))
        {
            IExpression right = ParseAND();
            left = new BinaryExpression(left, BinaryOperator.OR, right);
        }
        return left;
    }

    IExpression ParseAND()
    {
        IExpression left = ParseEquality();
        while (Match(TokenType.Operator_AND))
        {
            IExpression right = ParseEquality();
            left = new BinaryExpression(left, BinaryOperator.AND, right);
        }
        return left;
    }

    IExpression ParseEquality()
    {
        IExpression left = ParseComparison();
        while (Match(TokenType.Operator_EqualEqual) || Match(TokenType.Operator_Inequal))
        {
            Token op = Previous();
            BinaryOperator compOperatorType = OperatorTypeToType(op.TokenType);
            IExpression right = ParseComparison();
            left = new BinaryExpression(left, compOperatorType, right);
        }
        return left;
    }

    IExpression ParseComparison()
    {
        IExpression left = ParseAdditive();
        while (Match(TokenType.Operator_Greater) || Match(TokenType.Operator_Less))
        {
            Token op = Previous();

            BinaryOperator binaryOperatorType = OperatorTypeToType(op.TokenType);

            IExpression right = ParseAdditive();
            left = new BinaryExpression(left, binaryOperatorType, right);
        }
        return left;
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

        if (Match(TokenType.Operator_Invert))
        {
            IExpression operand = ParseUnary();
            return new UnaryExpression(UnaryOperator.Invert, operand);
        }

        if (Match(TokenType.Punctuation_At))
        {
            IExpression operand = ParseUnary();
            return new UnaryExpression(UnaryOperator.AddressOf, operand, assignable: true);
        }

        return ParsePrimary();
    }

    IExpression ParsePrimary()
    {
        IExpression expression = null!;
        if (Match(TokenType.Keyword_new))
        {
            Token typeToken = Consume(TokenType.Identifier, "Expected type name after 'new'");
            string type = typeToken.Lexeme;   // composite type

            Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after type name");
            List<IExpression> arguments = [];
            if (!Check(TokenType.Punctuation_ParenthesisR))
                do
                {
                    arguments.Add(ParseExpression());
                } while (Match(TokenType.Punctuation_Comma));
            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after arguments");
            expression = new InstantiationExpression(type, arguments);
        }
        else if (Match(TokenType.Literal))
        {
            (object Value, string Type) lit = Previous().Lexeme switch
            {
                string s when int.TryParse(s, out int i) => (i, "int"),
                string s when double.TryParse(s, out double d) => (d, "float"),
                string s when bool.TryParse(s, out bool b) => (b, "bool"),
                _ => throw new Exception("Invalid literal")
            };

            LiteralExpression literal = new LiteralExpression(lit.Value, Previous().Lexeme);
            literal.ResolvedType = new TypeInfo(lit.Type);
            expression = literal;
        }
        else if (Peek().TokenType == TokenType.Identifier
            && PeekNext().TokenType == TokenType.Punctuation_ParenthesisL)
        {
            Token nameToken = Advance();
            Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after method call");

            List<IExpression> arguments = [];
            if (!Check(TokenType.Punctuation_ParenthesisR))
                do
                {
                    arguments.Add(ParseExpression());
                } while (Match(TokenType.Punctuation_Comma));

            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after arguments");
            expression = new CallExpression(nameToken.Lexeme, arguments);
        }
        else if (Peek().TokenType == TokenType.Identifier
            && PeekNext().TokenType == TokenType.Punctuation_Dot)
        {
            IdentifierExpression target = new IdentifierExpression(Advance().Lexeme);
            Consume(TokenType.Punctuation_Dot, "Expected '.' after member access target");
            IdentifierExpression member = new IdentifierExpression(Advance().Lexeme);
            expression = new MemberAccessExpression(target, member);
        }
        else if (Match(TokenType.Identifier))
        {
            string name = Previous().Lexeme;
            IdentifierExpression identifierExpression = new IdentifierExpression(name);

            expression = identifierExpression;
        }
        else if (Match(TokenType.Punctuation_ParenthesisL))
        {
            IExpression inner = ParseExpression();
            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after expression");
            expression = inner;
        }

        while (Match(TokenType.Punctuation_BracketL))
        {
            IExpression index = ParseExpression();
            Consume(TokenType.Punctuation_BracketR, "Expected ']' after array index");
            expression = new IndexExpression(expression, index);
        }

        if (expression != null)
            return expression;

        Log.Error(2, $"Unexpected token {Peek().TokenType} in expression {PrintCurrentPos()}");
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
        TokenType.Operator_Equal => BinaryOperator.Equal,
        TokenType.Operator_Greater => BinaryOperator.Greater,
        TokenType.Operator_Less => BinaryOperator.Less,

        TokenType.Operator_EqualEqual => BinaryOperator.EqualEqual,
        TokenType.Operator_Inequal => BinaryOperator.NotEqual,

        TokenType.Operator_PlusEqual => BinaryOperator.PlusEqual,
        TokenType.Operator_MinusEqual => BinaryOperator.MinusEqual,
        TokenType.Operator_MultEqual => BinaryOperator.MultEqual,
        TokenType.Operator_DivEqual => BinaryOperator.DivEqual,
        _ => throw new Exception($"Can't parse '{tokenType}' as Binary Operator"),
    };

    string TokenTypeToString(TokenType tokenType) => tokenType switch
    {
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

        TokenType.Identifier => Previous().Lexeme,

        _ => throw new Exception($"Unsupported Type '{Previous().Lexeme}' at {PrintCurrentPos()}"),
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
        if (IsBuiltinTypeKeyword(t.TokenType) || t.TokenType == TokenType.Identifier) return Advance();
        Log.Error(4, $"Unxpected type keyword '{t.Lexeme}' at {t.Line}:{t.Column}");
        return null!;
    }

    // 2 == Keyword_bool; 12 == Keyword_f128
    static bool IsBuiltinTypeKeyword(TokenType tokenType) => (int)tokenType >= 2 && (int)tokenType <= 12;

    int Line() => tokens[pos].Line + 1;
    int Column() => tokens[pos].Column;
    string PrintCurrentPos()
    {
        return $"at {Line()}:{Column()}";
    }

    bool Check(TokenType type)
        => !IsAtEnd() && Peek().TokenType == type;

    Token Advance() => pos < tokens.Count ? tokens[pos++] : tokens[^1];

    bool IsAtEnd() => Peek().TokenType == TokenType.EOF;

    Token Peek() => tokens[pos];

    Token PeekNext() => pos + 1 < tokens.Count ? tokens[pos + 1] : tokens[^1];
    Token PeekNextNext() => pos + 2 < tokens.Count ? tokens[pos + 2] : tokens[^1];

    Token Previous() => tokens[pos - 1];

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
        TypeInfo typeInfo = new TypeInfo(typeName);
        globalScope!.Declare(typeName,
            new SymbolInfo(
                typeName,
                typeInfo,
                SymbolKind.Type,
                null
            ));
        globalScope!.Declare("@" + typeName,
            new SymbolInfo(
                "@" + typeName,
                new TypeInfo("@" + typeName, pointee: typeInfo),
                SymbolKind.Type,
                null
            ));
    }

    #endregion
}