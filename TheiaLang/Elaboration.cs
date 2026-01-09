namespace TheiaLang;

static class Elaboration
{
    // the template we use for dynamic arrays
    public const string DYNAMIC_ARRAY_PREFIX = "__dynamic_array";
    public const string DYNAMIC_ARRAY_INDEX = "__index";

    static Dictionary<string, SymbolInfo> DynamicArrayCache = [];
    static Scope currentScope;
    static ProgramNode PreloadAST;
    static ProgramNode ProgramAST;
    static Scope GlobalScope;
    static Scope PreloadScope;

    static readonly string[] builtinFunctions =
    [
        "AllocB",
        "ReallocB",
        "Free",
        "TypeSize",
    ];

    public static (ProgramNode programAST, Scope programScope) Lower(Scope preloadScope, ProgramNode preloadAST,
                                                                     Scope programScope, ProgramNode programAST)
    {
        GlobalScope = programScope;
        ProgramAST = programAST;
        PreloadAST = preloadAST;
        PreloadScope = preloadScope;
        
        currentScope = GlobalScope;

        foreach (string functionName in builtinFunctions)
            IncludeBuiltinFn(functionName);

        for (int i = 0; i < ProgramAST.Nodes.Count; i++)
        {
            INode node = ProgramAST.Nodes[i];

            if (node is StructDeclaration sd)
                AnalyseStruct(sd);

            if (node is FunctionDeclaration fn)
            {
                currentScope = fn.Scope!;
                for (int j = 0; j < fn.Arguments.Count; j++)
                {
                    if(fn.Arguments[j].ResolvedType?.ArrayLength == 0)
                        fn.Arguments[j] = SubstituteDynamicArray(fn.Arguments[j]);
                }
                ExitScope();

                AnalyseFunctionBody(fn);
            }
        }

        return (ProgramAST, GlobalScope);
    }

    static void IncludeBuiltinFn(string name)
    {
        // TODO this is very slow ofc but should work for now
        foreach (INode node in PreloadAST.Nodes)
            if (node is FunctionDeclaration functionDeclaration
             && functionDeclaration.Name == name)
            {
                ProgramAST.Nodes.Add(node);
                if (!PreloadScope.TryLookup(name, out SymbolInfo? symbolInfo, out _))
                    throw new Exception($"Could not find template {name}");
                functionDeclaration.Scope = new Scope(name, node, GlobalScope);
                GlobalScope.Declare(symbolInfo!);
            }
    }

    static void AnalyseStruct(StructDeclaration structDeclaration)
    {
        currentScope = structDeclaration.Scope!;
        foreach (FunctionDeclaration function in structDeclaration.Functions)
            AnalyseFunctionBody(function);

        ExitScope();
    }

    static void AnalyseFunctionBody(FunctionDeclaration function)
    {
        currentScope = function.Scope!;
        AnalyseStatements(function.Statements);

        ExitScope();
    }

    static void AnalyseStatements(List<IStatement> statements)
    {
        /// we could have dynamic arrays occur in the following places
        /// 1 - declared directly:   []s32
        /// 2 - as a pointee:        @[]s32
        /// 3 - as an array element: [4][]s32
        /// 4 - any combination of the above

        /// for each dynamic array type, we need:
        /// - struct type
        /// - array read fn
        /// - array write fn
        /// for the struct type, we can use a void ptr
        /// but for array manipulation, we will need to monomorphise,
        /// so we will need to generate a proper TypeInfo too

        for (int i = 0; i < statements.Count; i++)
        {
            IStatement statement = statements[i];

            /*
            if (statement is VariableDeclaration variable
             && variable.ResolvedType != null)
            {
                LowerType(variable.ResolvedType);
            }
            */
            if (statement is VariableDeclaration varDeclaration
                 && varDeclaration.ResolvedType?.ArrayLength == 0)
            {
                TypeNamePair newVar = SubstituteDynamicArray(new TypeNamePair(varDeclaration.ResolvedType, varDeclaration.Name));
                VariableDeclaration vd = new VariableDeclaration(newVar.ResolvedType.TypeName, newVar.Identifier, null);
                vd.ResolvedType = newVar.ResolvedType;
                statements[i] = vd;
            }
        }
    }

    static TypeNamePair SubstituteDynamicArray(TypeNamePair sourceVar)
    {
        TypeInfo sourceType = sourceVar.ResolvedType;
        string structName = $"{DYNAMIC_ARRAY_PREFIX}_{sourceType.ElementType.TypeName}";
        // Log.Info(typeName);
        // (1) check if that kind of array is already declared as struct
        if (DynamicArrayCache.TryGetValue(sourceType.TypeName, out SymbolInfo? symbolInfo))
        {
            return new TypeNamePair(symbolInfo.Type, sourceVar.Identifier);
            //currentScope!.Symbols[varDeclaration.Name] = symbolInfo;
        }
        // (2) if no -> declare the struct
        else
        {
            StructDeclaration sd = CreateDynamicArrayStruct(structName, sourceType.ElementType);

            // TODO: make this work with inits once we have them for arrays
            TypeNamePair newVar = new TypeNamePair(sd.ResolvedType, sourceVar.Identifier);
            newVar.ResolvedType = sd.ResolvedType;
            SymbolInfo varInfo = new SymbolInfo(sourceVar.Identifier,
                                                sd.ResolvedType,
                                                SymbolKind.Variable,
                                                null);
            //currentScope!.Symbols[vd.Name] = varInfo;
            DynamicArrayCache.Add(sourceType.TypeName, varInfo);

            ProgramAST.Nodes.Add(sd);
            GlobalScope.Declare(new SymbolInfo(sd.Name,
                                               sd.ResolvedType,
                                               SymbolKind.Type,
                                               sd.Fields));

            return newVar;
        }
    }

    #region Helpers

    static StructDeclaration CreateDynamicArrayStruct(string name, TypeInfo elementType)
    {
        Scope scope = currentScope;
        currentScope = GlobalScope;
        EnterNewScope(name);

        List<TypeNamePair> fields = [
            new TypeNamePair(new TypeInfo("s64", 8), "Length"),
            new TypeNamePair(new TypeInfo($"@{elementType.TypeName}", 8, new TypeInfo("void", 0)), "Data"),
            new TypeNamePair(new TypeInfo("s64", 8), "Size"),
        ];

        List<FunctionDeclaration> functions = [
            new FunctionDeclaration(
                typeName:   elementType.TypeName, 
                name:       DYNAMIC_ARRAY_INDEX,
                paramaters: [new TypeNamePair(new TypeInfo("s64", 8), "index")],
                statements: [new ReturnStatement(new UnaryExpression(UnaryOperator.Dereference,
                                new CallExpression(new IdentifierExpression($"@{elementType.TypeName}"),[
                                    new CallExpression(new IdentifierExpression($"@void"),[
                                        new BinaryExpression(new CallExpression(new IdentifierExpression("s64"),[
                                            new IdentifierExpression("Data")
                                        ]),
                                        BinaryOperator.Add,
                                        new BinaryExpression(new CallExpression(new IdentifierExpression("TypeSize"),[
                                            new IdentifierExpression($"@{elementType.TypeName}")
                                        ]),
                                        BinaryOperator.Multiply,
                                        new IdentifierExpression("index")))
                                    ])
                                ]))
            )])];

        functions[0].Scope = new Scope(functions[0].Name, functions[0], currentScope);

        foreach (TypeNamePair field in fields)
            currentScope.Declare(new SymbolInfo(field.Identifier,
                                                new TypeInfo(field.ResolvedType.TypeName),
                                                SymbolKind.Variable,
                                                null));

        foreach(FunctionDeclaration function in functions)
            currentScope.Declare(new SymbolInfo(
                        function.Name,
                        function.ResolvedType,
                        SymbolKind.Function,
                        function.Arguments));

        StructDeclaration sd = new StructDeclaration(name, fields, functions);
        sd.Scope = currentScope;
        currentScope.DeclaringNode = sd;
        
        currentScope = scope;

        return sd;
    }

    static Scope EnterNewScope(string name, INode? declaringNode = null)
    {
        currentScope = new Scope(name, declaringNode, currentScope);
        return currentScope;
    }

    static void ExitScope()
    {
        currentScope = currentScope.Exit();
    }

    #endregion
}