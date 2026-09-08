namespace TheiaLang;
using static TypeKind;

public class Parser(List<Token> tokens)
{
    int pos = 0;

    Scope? globalScope;
    Scope? currentScope;

    string programName = "";

    const int MAX_POSTFIX_DEPTH = 128;

    long acc = 0;

    List<INode> nodes = [];

    public (ProgramNode, Scope) ParseProgram(string ProgramName)
    {
        programName = ProgramName;
        globalScope = new Scope("");
        currentScope = globalScope;   // global scope

        foreach (string builtinType in IRGenerator.BuiltinTypes)
            DeclareBuiltin(builtinType);

        globalScope.Declare(
            new SymbolInfo(
                "__th_allocB",
                new TypeInfo("@void", Pointer, 8, new TypeInfo("void", Void)),
                SymbolKind.Function,
                [new TypeNamePair(new TypeInfo("s64", Scalar), "size", SourePosition.None)]
            ));

        globalScope.Declare(
            new SymbolInfo(
                "__th_reallocB",
                new TypeInfo("@void", Pointer, 8, new TypeInfo("void", Void)),
                SymbolKind.Function,
                [   new TypeNamePair(new TypeInfo("@void", Pointer), "alloc", SourePosition.None),
                    new TypeNamePair(new TypeInfo("s64", Scalar), "newSize", SourePosition.None)]
            ));

        globalScope.Declare(
            new SymbolInfo(
                "__th_free",
                new TypeInfo("void", Void, 0),
                SymbolKind.Function,
                [new TypeNamePair(new TypeInfo("@void", Pointer), "ptr", SourePosition.None)
            ]));

        globalScope.Declare(
            new SymbolInfo(
                "__th_alloc",
                new TypeInfo("@void", Pointer, 8, new TypeInfo("void", Void)),
                SymbolKind.Function,
                [new TypeNamePair(new TypeInfo("s64", Scalar), "size", SourePosition.None)]
            ));

        List<INode> nodes = ParseDeclarations();
        return (new ProgramNode(ProgramName, nodes), globalScope);
    }

    #region Declarations

    List<INode> ParseDeclarations()
    {
        while (!IsAtEnd())
            nodes.Add(ParseDeclaration());

        return nodes;
    }

    IDeclaration ParseDeclaration()
    {
        SourePosition startPosition = Pos();
        if (Peek().TokenType == TokenType.Keyword_struct && PeekNext().TokenType == TokenType.Identifier)
            return ParseStructDeclaration();
        if (Peek().TokenType == TokenType.Keyword_union && PeekNext().TokenType == TokenType.Identifier)
            return ParseUnionDeclaration();
        if (MatchTypeDefinition(out TypeInfo typeInfo)
            && Peek().TokenType == TokenType.Identifier)
        {
            if (PeekNext().TokenType == TokenType.Punctuation_ParenthesisL)
                return ParseFunctionDeclaration(typeInfo, startPosition);

            return ParseVariableDeclaration(typeInfo, startPosition, true);
        }

        throw new Exception($"{startPosition} Can't resolve type '{Peek().Lexeme}'");
    }

    (bool success, IDeclaration result) TryParseDeclaration()
    {
        SourePosition startPosition = Pos();
        if (Peek().TokenType == TokenType.Keyword_struct && PeekNext().TokenType == TokenType.Identifier)
            return (true, ParseStructDeclaration());
        if (Peek().TokenType == TokenType.Keyword_union && PeekNext().TokenType == TokenType.Identifier)
            return (true, ParseUnionDeclaration());
        if (MatchTypeDefinition(out TypeInfo typeInfo)
            && Peek().TokenType == TokenType.Identifier)
        {
            if (PeekNext().TokenType == TokenType.Punctuation_ParenthesisL)
                return (true, ParseFunctionDeclaration(typeInfo, startPosition));

            return (true, ParseVariableDeclaration(typeInfo, startPosition, true));
        }

        return (false, null!);
    }

    VariableDeclaration ParseVariableDeclaration(TypeInfo typeInfo, SourePosition startPosition, bool requireSemicolon)
    {
        IExpression? init = null;
        string name = Consume(TokenType.Identifier).Lexeme;

        if (Match(TokenType.Operator_Equal))
            init = ParseExpression();

        if (requireSemicolon)
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after variable declaration");

        VariableDeclaration variable = new VariableDeclaration(typeInfo.TypeName, name, init, startPosition);
        variable.ResolvedType = typeInfo;

        return variable;
    }

    FunctionDeclaration ParseFunctionDeclaration(TypeInfo returnTypeInfo, SourePosition startPosition)
    {
        Token nameToken = Consume(TokenType.Identifier, "Expected function name");
        string name = nameToken.Lexeme;

        Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after function name");
        List<TypeNamePair> parameters = [];
        List<IStatement> body = [];

        FunctionDeclaration functionDeclaration = new FunctionDeclaration(returnTypeInfo.TypeName, returnTypeInfo.TypeKind, name, parameters, body, startPosition);

        EnterNewScope(name, functionDeclaration);
        if (!Check(TokenType.Punctuation_ParenthesisR))
        {
            do
            {
                startPosition = Pos();
                if (!MatchTypeDefinition(out TypeInfo parameterInfo))
                    throw new Exception($"{startPosition} Can't resolve type '{Peek().Lexeme}'");

                Token identifierToken = Consume(TokenType.Identifier, "Expected field name");
                TypeNamePair parameter = new TypeNamePair(parameterInfo, identifierToken.Lexeme, startPosition);
                // Log.Info($"Parameter {fieldType} '{identifierToken.Lexeme}' defined in '{currentScope.FullName}'");
                parameters.Add(parameter);
                currentScope!.Declare(new SymbolInfo(parameter.Identifier, parameter.ResolvedType, SymbolKind.Variable, null));
            } while (Match(TokenType.Punctuation_Comma));
        }

        // we will not deal with this for now!!!

        Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after parameters");

        functionDeclaration.Scope = currentScope;
        // currentScope!.DeclaringNode = functionDeclaration;
        body.AddRange(ParseBlock());

        // fill out AST reference
        ExitScope();

        currentScope!.Declare(new SymbolInfo(
                                functionDeclaration.Name,
                                returnTypeInfo,
                                SymbolKind.Function,
                                functionDeclaration.Parameters
                                ));

        return functionDeclaration;
    }

    StructDeclaration ParseStructDeclaration()
    {
        Consume(TokenType.Keyword_struct);

        SourePosition startPos = Previous().Pos;
        Token nameToken = Consume(TokenType.Identifier, "Expected struct name");
        string name = nameToken.Lexeme;
        Consume(TokenType.Punctuation_BraceL, "Expected '{' after struct name");

        List<TypeNamePair> fields = [];
        List<string> fieldNames = [];
        List<TypeInfo> fieldTypes = [];
        List<FunctionDeclaration> functions = [];

        StructDeclaration structDeclaration = new StructDeclaration(name, fields, functions, startPos);
        structDeclaration.Scope = EnterNewScope(name);
        currentScope!.DeclaringNode = structDeclaration;

        if (!Check(TokenType.Punctuation_BraceR))
        {
            do
            {
                IDeclaration declaration = ParseDeclaration();
                if(declaration is VariableDeclaration vd)
                {
                    currentScope.Declare(new SymbolInfo(vd.Name,
                                                        vd.ResolvedType!,
                                                        SymbolKind.Variable,
                                                        null));
                    fields.Add(new TypeNamePair(vd.ResolvedType!, vd.Name, vd.Pos));
                    fieldNames.Add(vd.Name);
                    fieldTypes.Add(vd.ResolvedType!);
                }
                else if (declaration is FunctionDeclaration fd)
                {
                    functions.Add(fd);
                }

            } while (!Check(TokenType.Punctuation_BraceR));
        }
        Consume(TokenType.Punctuation_BraceR, "Expected '}' after struct fields");

        ExitScope();
        structDeclaration.ResolvedType = new TypeInfo(
            name,
            Struct,
            fieldNames: fieldNames,
            fieldTypes: fieldTypes);

        SymbolInfo symbolInfo = new SymbolInfo(
            name,
            structDeclaration.ResolvedType,
            SymbolKind.Type,
            fields);

        currentScope.Declare(symbolInfo);
        return structDeclaration;
    }

    UnionDeclaration ParseUnionDeclaration()
    {
        Consume(TokenType.Keyword_union);

        Token nameToken = Consume(TokenType.Identifier, "Expected union name");
        string name = nameToken.Lexeme;

        Consume(TokenType.Punctuation_BraceL, "Expected '{' after union name");
        List<string> variantNames = [];
        List<TypeInfo> variantTypes = [];
        List<TypeNamePair> variants = [];

        TypeInfo unionInfo = new TypeInfo(
            name,
            Union,
            fieldNames: variantNames,
            fieldTypes: variantTypes);

        UnionDeclaration unionDeclaration = new UnionDeclaration(name, variants, unionInfo, nameToken.Pos);
        unionDeclaration.Scope = EnterNewScope(name);
        currentScope!.DeclaringNode = unionDeclaration;

        if (!Check(TokenType.Punctuation_BraceR))
        {
            do
            {
                SourePosition startPosition = Pos();
                if (!MatchTypeDefinition(out TypeInfo variantType))
                    throw new Exception($"{startPosition} Can't resolve type '{Peek().Lexeme}'");

                Token identifierToken = Consume(TokenType.Identifier, "Expected variant name");
                TypeNamePair parameter = new TypeNamePair(variantType, identifierToken.Lexeme, startPosition);

                variants.Add(parameter);
                variantNames.Add(identifierToken.Lexeme);
                variantTypes.Add(variantType);

                currentScope.Declare(
                     new SymbolInfo(
                        identifierToken.Lexeme,
                        variantType,
                        SymbolKind.Variable,
                        null
                     ));
            }
            while (Match(TokenType.Punctuation_Semicolon)
               && !Check(TokenType.Punctuation_BraceR));
        }

        Consume(TokenType.Punctuation_BraceR, "Expected '}' after union variants");

        unionDeclaration.ResolvedType = new TypeInfo(
            name,
            Union,
            fieldNames: variantNames,
            fieldTypes: variantTypes);

        ExitScope();
        currentScope!.Declare(new SymbolInfo(
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
        {
            (bool success, IDeclaration result) = TryParseDeclaration();
            if(success)
                statements.Add(result);
            else
                statements.Add(ParseStatement());
        }

        Consume(TokenType.Punctuation_BraceR, "Expected '}' after block");
        return statements;
    }

    IStatement ParseStatement(bool requireSemicolon = true)
    {
        SourePosition startPosition = Pos();

        if (Match(TokenType.Keyword_if))
        {
            Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after if statement");
            IExpression condition = ParseExpression();
            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' to close condition of if statement");

            EnterNewScope($"if_then{pos}");
            //currentScope.Declare(new SymbolInfo($"if_then{pos}", SymbolKind.))
            List<IStatement> thenBranch = ParseBlock();
            Scope thenScope = currentScope!;
            Scope? elseScope = null;
            ExitScope();
            List<IStatement>? elseBranch = null;
            if (Peek().TokenType == TokenType.Keyword_else)
            {
                EnterNewScope($"if_else{pos}");
                elseScope = currentScope;
                Consume(TokenType.Keyword_else, "");
                elseBranch = ParseBlock();
                ExitScope();
            }

            return new IfStatement(condition, thenBranch, elseBranch, thenScope, elseScope, startPosition);
        }

        if (Match(TokenType.Keyword_for))
        {
            EnterNewScope($"for_{pos}");
            Consume(TokenType.Punctuation_ParenthesisL, "Expected '(' after for keyword");
            IStatement initialiser = ParseDeclaration();
            IExpression condition = ParseExpression();
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after loop condition");
            IStatement iterator = ParseStatement(requireSemicolon: false);
            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after loop head");

            Scope bodyScope = currentScope!;
            List<IStatement> body = ParseBlock();
            ExitScope();

            return new ForStatement(initialiser, condition, iterator, body, bodyScope, startPosition);
        }

        if (Match(TokenType.Keyword_return))
        {
            ReturnStatement returnStatement;
            if (Peek().TokenType != TokenType.Punctuation_Semicolon)
                returnStatement = new ReturnStatement(ParseExpression(), startPosition);
            else
                returnStatement = new ReturnStatement(null, startPosition);

            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after return value");
            return returnStatement;
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
                    return new CompoundAssignmentStatement(lhs, rhs, (BinaryOperator)op, startPosition);
                return new AssignmentStatement(lhs, rhs, startPosition);
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
            return new ExpressionStatement(expression, startPosition);
        }

        Log.Error(1, $"Unexpected token {Peek().TokenType} '{Peek().Lexeme}' at {programName}:{Peek().Pos}");
        return null!;
    }

    bool MatchTypeDefinition(out TypeInfo typeInfo)
    {
        int startPos = pos;
        if(Match(TokenType.Keyword_struct))                 // anonymous struct
        {
            SourePosition sourcePos = Previous().Pos;

            Consume(TokenType.Punctuation_BraceL, "Expected '{' after struct keyword");
            List<TypeNamePair> fields = [];
            List<string> fieldNames = [];
            List<TypeInfo> fieldTypes = [];
            List<FunctionDeclaration> functions = [];

            string typeName = $"__anonymousStruct_{currentScope!.Name}_{acc++}";

            StructDeclaration structDeclaration = new StructDeclaration(typeName, fields, functions, sourcePos);
            structDeclaration.Scope = EnterNewScope(typeName);
            currentScope!.DeclaringNode = structDeclaration;

            if (!Check(TokenType.Punctuation_BraceR))
            {
                do
                {
                    IDeclaration declaration = ParseDeclaration();
                    if (declaration is VariableDeclaration vd)
                    {
                        currentScope.Declare(new SymbolInfo(vd.Name,
                                                            vd.ResolvedType!,
                                                            SymbolKind.Variable,
                                                            null));
                        fields.Add(new TypeNamePair(vd.ResolvedType!, vd.Name, vd.Pos));
                        fieldNames.Add(vd.Name);
                        fieldTypes.Add(vd.ResolvedType!);
                    }
                    else if (declaration is FunctionDeclaration fd)
                    {
                        functions.Add(fd);
                    }

                } while (!Check(TokenType.Punctuation_BraceR));
            }

            Consume(TokenType.Punctuation_BraceR, "Expected '}' after struct fields");
            ExitScope();

            structDeclaration.ResolvedType = new TypeInfo(
                typeName,
                Struct,
                fieldNames: fieldNames,
                fieldTypes: fieldTypes);

            SymbolInfo symbolInfo = new SymbolInfo(
                typeName,
                structDeclaration.ResolvedType,
                SymbolKind.Type,
                fields);

            currentScope.Declare(symbolInfo);
            
            typeInfo = structDeclaration.ResolvedType;

            nodes.Add(structDeclaration);
            return true;
        }
        if(Match(TokenType.Keyword_union))                 // anonymous union
        {
            SourePosition sourcePos = Previous().Pos;

            Consume(TokenType.Punctuation_BraceL, "Expected '{' after union keyword");
            List<TypeNamePair> variants = [];
            List<string> variantNames = [];
            List<TypeInfo> variantTypes = [];

            string typeName = $"__anonymousUnion_{currentScope!.Name}_{acc++}";

            TypeInfo unionInfo = new TypeInfo(
                typeName,
                Union,
                fieldNames: variantNames,
                fieldTypes: variantTypes);

            UnionDeclaration unionDeclaration = new UnionDeclaration(typeName, variants, unionInfo, sourcePos);
            unionDeclaration.Scope = EnterNewScope(typeName);
            currentScope!.DeclaringNode = unionDeclaration;

            if (!Check(TokenType.Punctuation_BraceR))
            {
                do
                {
                    IDeclaration declaration = ParseDeclaration();
                    if (declaration is VariableDeclaration vd)
                    {
                        currentScope.Declare(new SymbolInfo(vd.Name,
                                                            vd.ResolvedType!,
                                                            SymbolKind.Variable,
                                                            null));
                        variants.Add(new TypeNamePair(vd.ResolvedType!, vd.Name, vd.Pos));
                        variantNames.Add(vd.Name);
                        variantTypes.Add(vd.ResolvedType!);
                    }
                    else 
                        throw new Exception();

                } while (!Check(TokenType.Punctuation_BraceR));
            }

            Consume(TokenType.Punctuation_BraceR, "Expected '}' after struct fields");
            ExitScope();

            unionDeclaration.ResolvedType = new TypeInfo(
                typeName,
                Union,
                fieldNames: variantNames,
                fieldTypes: variantTypes);

            SymbolInfo symbolInfo = new SymbolInfo(
                typeName,
                unionDeclaration.ResolvedType,
                SymbolKind.Type,
                variants);

            currentScope.Declare(symbolInfo);
            
            typeInfo = unionDeclaration.ResolvedType;

            nodes.Add(unionDeclaration);
            return true;
        }
        if (Match(TokenType.Punctuation_At))                // ptr
        {
            if (!MatchTypeDefinition(out TypeInfo pointeeInfo))
            {
                typeInfo = new TypeInfo("", Void);
                pos = startPos;
                return false;
            }

            typeInfo = new TypeInfo(
                type: $"@{pointeeInfo.TypeName}",
                typeKind: Pointer,
                pointee: pointeeInfo);
            return true;
        }
        else if (Match(TokenType.Punctuation_BracketL))     // array
        {
            uint arrayLength = 0;

            if (Peek().TokenType == TokenType.Literal)
                arrayLength = uint.Parse(Advance().Lexeme);

            Consume(TokenType.Punctuation_BracketR, $"Expected closing ']', got: {Peek().TokenType}");

            if (!MatchTypeDefinition(out TypeInfo elementInfo))
                throw new Exception($"Expected type after array definition");

            typeInfo = new TypeInfo($"[{arrayLength}]{elementInfo.TypeName}",
                                    Array,
                                    elementType: elementInfo,
                                    arrayLength: arrayLength);
            
            return true;
        }
        else if (IsBuiltinType(Peek().TokenType))
        {
            string typeName = TokenTypeToString(Peek().TokenType);
            TypeKind kind = Scalar;
            if(Peek().TokenType == TokenType.Keyword_void)
                kind = Void;

            typeInfo = new TypeInfo($"{typeName}",
                                    kind,
                                    size: SemanticAnalyser.SizeOf(typeName));
            Advance();
            return true;
        }
        else    // composite; we do not want to touch the identifier here, but still check for its existence
            if (Peek().TokenType == TokenType.Identifier && PeekNext().TokenType == TokenType.Identifier)
            {
                Advance();
                // TODO this is janky
                string typeName = TokenTypeToString(Previous().TokenType);
                
                typeInfo = new TypeInfo(typeName, Unresolved);
                return true;
            }

        typeInfo = new TypeInfo("", Void);
        pos = startPos;
        return false;
    }

    AssignmentStatement ParseAssignment(bool requireSemicolon = true)
    {
        Token nameToken = Advance();
        IdentifierExpression identifier = new IdentifierExpression(nameToken.Lexeme, nameToken.Pos);

        Advance();  // consume '='
        IExpression expr = ParseExpression();
        if (requireSemicolon)
            Consume(TokenType.Punctuation_Semicolon, "Expected ';' after assignment");
        
        return new AssignmentStatement(identifier, expr, nameToken.Pos);
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
            left = new BinaryExpression(left, BinaryOperator.OR, right, left.Pos);
        }
        return left;
    }

    IExpression ParseAND()
    {
        IExpression left = ParseEquality();
        while (Match(TokenType.Operator_AND))
        {
            IExpression right = ParseEquality();
            left = new BinaryExpression(left, BinaryOperator.AND, right, left.Pos);
        }
        return left;
    }

    IExpression ParseEquality()
    {
        IExpression left = ParseComparison();
        while (Match(TokenType.Operator_EqualEqual) || Match(TokenType.Operator_NotEqual))
        {
            Token op = Previous();
            BinaryOperator compOperatorType = OperatorTypeToType(op.TokenType);
            IExpression right = ParseComparison();
            left = new BinaryExpression(left, compOperatorType, right, left.Pos);
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
            left = new BinaryExpression(left, binaryOperatorType, right, left.Pos);
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

            expr = new BinaryExpression(expr, binaryOperatorType, right, expr.Pos);
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
            expr = new BinaryExpression(expr, op, right, expr.Pos);
        }
        return expr;
    }

    IExpression ParseUnary()
    {
        SourePosition startPosition = Pos();
        if (Match(TokenType.Operator_Minus))
        {
            // we’ve consumed the ‘-’
            IExpression operand = ParseUnary();
            return new UnaryExpression(UnaryOperator.Negate, operand, startPosition);
        }

        if (Match(TokenType.Operator_Invert))
        {
            IExpression operand = ParseUnary();
            return new UnaryExpression(UnaryOperator.Invert, operand, startPosition);
        }

        if (Match(TokenType.Punctuation_Dollar))
        {
            IExpression operand = ParseUnary();
            return new UnaryExpression(UnaryOperator.Dereference, operand, startPosition, assignable: true);
        }

        if (Peek().TokenType == TokenType.Punctuation_At)
        {
            int save = pos;

            if (MatchTypeDefinition(out TypeInfo typeInfo)
            &&  Peek().TokenType == TokenType.Punctuation_ParenthesisL)
            {
                return ParsePostfix(new IdentifierExpression(typeInfo.TypeName, startPosition));
            }

            pos = save;          // backtrack and parse address-of
            Advance();           // consume '@'
            IExpression operand = ParseUnary();
            return new UnaryExpression(UnaryOperator.AddressOf, operand, startPosition, assignable: false);
        }

        return ParsePrimary();
    }

    IExpression ParsePrimary()
    {
        SourePosition startPosition = Pos();
        IExpression expression = null!;

        if (Match(TokenType.Literal))
        {
            (object Value, string Type, TypeKind kind) lit = Previous().Lexeme switch
            {
                string s when int.TryParse(s, out int i)       => (i, "int", Scalar),
                string s when double.TryParse(s, out double d) => (d, "float", Scalar),
                string s when bool.TryParse(s, out bool b)     => (b, "bool", Scalar),
                _ => throw new Exception("Invalid literal")
            };

            LiteralExpression literal = new LiteralExpression(lit.Value, Previous().Lexeme, startPosition);
            literal.ResolvedType = new TypeInfo(lit.Type, lit.kind, SemanticAnalyser.SizeOf(lit.Type));
            expression = literal;
        }
        else if (Peek().TokenType == TokenType.Identifier)
        {
            expression = ParsePostfix(new IdentifierExpression(Advance().Lexeme, startPosition));
        }
        else if (Match(TokenType.Punctuation_ParenthesisL))
        {
            IExpression inner = ParsePostfix(ParseExpression());
            Consume(TokenType.Punctuation_ParenthesisR, "Expected ')' after expression");
            expression = ParsePostfix(inner);
        }
        else if (MatchTypeDefinition(out TypeInfo typeInfo))
            expression = ParsePostfix(new IdentifierExpression(typeInfo.TypeName, startPosition));

        if (expression != null)
            return expression;

        Log.Error(2, $"Unexpected token {Peek().TokenType} in expression {PrintCurrentPos()}");
        return null!;
    }

    IExpression ParsePostfix(IExpression prefix)
    {
        for (int i = 0; i < MAX_POSTFIX_DEPTH; i++)
        {
            if (Match(TokenType.Punctuation_ParenthesisL))
            {
                List<IExpression> arguments = [];
                if (!Check(TokenType.Punctuation_ParenthesisR))
                    do
                    {
                        arguments.Add(ParseExpression());
                    } while (Match(TokenType.Punctuation_Comma));
                Advance();
                //if (prefix is MemberAccessExpression memberAccess)
                //    prefix = new CallExpression(memberAccess.Member, arguments);
                //else
                prefix = new CallExpression(prefix, arguments, prefix.Pos);
            }
            else if (Match(TokenType.Punctuation_BracketL))
            {
                IExpression index = ParseExpression();
                Consume(TokenType.Punctuation_BracketR);
                prefix = new IndexExpression(prefix, index, prefix.Pos);
            }
            else if (Match(TokenType.Punctuation_BraceL))
            {
                if (prefix is IdentifierExpression identifier)
                {
                    List<IExpression> arguments = [];
                    if (!Check(TokenType.Punctuation_BraceR))
                        do
                        {
                            arguments.Add(ParsePostfix(ParseExpression()));
                        } while (Match(TokenType.Punctuation_Comma)
                             && !Check(TokenType.Punctuation_BraceR));
                    Consume(TokenType.Punctuation_BraceR);
                    prefix = new InstantiationExpression(identifier.Name, arguments, identifier.Pos);
                }
            }
            else if (Match(TokenType.Punctuation_Dot))
            {
                Token name = Consume(TokenType.Identifier, "member");
                prefix = new MemberAccessExpression(prefix, new IdentifierExpression(name.Lexeme, name.Pos), prefix.Pos);
            }
        }
        return prefix;
    }

    #endregion

    #region Conversion

    static BinaryOperator OperatorTypeToType(TokenType tokenType) => tokenType switch
    {
        TokenType.Operator_Plus    => BinaryOperator.Add,
        TokenType.Operator_Minus   => BinaryOperator.Subtract,
        TokenType.Operator_Mult    => BinaryOperator.Multiply,
        TokenType.Operator_Div     => BinaryOperator.Divide,
        TokenType.Operator_Equal   => BinaryOperator.Equal,
        TokenType.Operator_Greater => BinaryOperator.Greater,
        TokenType.Operator_Less    => BinaryOperator.Less,

        TokenType.Operator_EqualEqual => BinaryOperator.EqualEqual,
        TokenType.Operator_NotEqual    => BinaryOperator.NotEqual,

        TokenType.Operator_PlusEqual  => BinaryOperator.PlusEqual,
        TokenType.Operator_MinusEqual => BinaryOperator.MinusEqual,
        TokenType.Operator_MultEqual  => BinaryOperator.MultEqual,
        TokenType.Operator_DivEqual   => BinaryOperator.DivEqual,
        
        _ => throw new Exception($"Can't parse '{tokenType}' as Binary Operator"),
    };

    string TokenTypeToString(TokenType tokenType) => tokenType switch
    {
        TokenType.Keyword_bool => "bool",
        TokenType.Keyword_void => "void",

        TokenType.Keyword_s8   => "s8",
        TokenType.Keyword_s16  => "s16",
        TokenType.Keyword_s32  => "s32",
        TokenType.Keyword_s64  => "s64",
        TokenType.Keyword_s128 => "s128",
        TokenType.Keyword_s256 => "s256",

        TokenType.Keyword_f16  => "f16",
        TokenType.Keyword_f32  => "f32",
        TokenType.Keyword_f64  => "f64",
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

    Token Consume(TokenType type, string message = "")
    {
        if (Check(type))
            return Advance();
        Log.Error(3, $"{programName}.tia {Peek().Pos.Row + 1}:{Peek().Pos.Column}: {message}, got: {Peek().TokenType} '{Peek().Lexeme}'");
        return null!;
    }

    Token ConsumeTypeKeyword()
    {
        Token t = Peek();
        if (IsBuiltinType(t.TokenType) || t.TokenType == TokenType.Identifier) return Advance();
        Log.Error(4, $"Unxpected type keyword '{t.Lexeme}' at {programName} {t.Pos.Row}:{t.Pos.Column}");
        return null!;
    }

    static bool IsBuiltinType(TokenType tokenType) => (int)tokenType >= 500 && (int)tokenType <= 539;

    int Line() => tokens[pos].Pos.Row + 1;
    int Column() => tokens[pos].Pos.Column;
    SourePosition Pos() => tokens[pos].Pos;
    string PrintCurrentPos()
    {
        return $"at {programName}:{Line()}:{Column()}";
    }

    bool Check(TokenType type) => !IsAtEnd() && Peek().TokenType == type;

    Token Advance() => pos < tokens.Count ? tokens[pos++] : tokens[^1];

    bool IsAtEnd() => Peek().TokenType == TokenType.EOF;

    Token Peek() => tokens[pos];

    Token PeekNext() => pos + 1 < tokens.Count ? tokens[pos + 1] : tokens[^1];
    Token PeekNextNext() => pos + 2 < tokens.Count ? tokens[pos + 2] : tokens[^1];

    Token Previous() => tokens[pos - 1];

    public Scope EnterNewScope(string name, INode? declaringNode = null)
    {
        currentScope = new Scope(name, declaringNode, currentScope);
        return currentScope;
    }

    void ExitScope()
    {
        currentScope = currentScope!.Exit();
    }

    void DeclareBuiltin(string typeName)
    {
        TypeKind kind = Scalar;
        if(typeName == "void")
            kind = Void;

        TypeInfo typeInfo = new TypeInfo(typeName, kind);
        globalScope!.Declare(
            new SymbolInfo(
                typeName,
                typeInfo,
                SymbolKind.Type,
                null
            ));
        globalScope!.Declare(
            new SymbolInfo(
                "@" + typeName,
                new TypeInfo("@" + typeName, Pointer, pointee: typeInfo),
                SymbolKind.Type,
                null
            ));
    }

    #endregion
}