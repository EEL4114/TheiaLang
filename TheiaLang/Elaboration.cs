namespace TheiaLang;
using static TypeKind;
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

            if (node is UnionDeclaration ud)
                AnalyseUnion(ud);

            if (node is FunctionDeclaration fn)
            {
                currentScope = fn.Scope!;
                for (int j = 0; j < fn.Parameters.Count; j++)
                {
                    if(fn.Parameters[j].ResolvedType?.ArrayLength == 0)
                    {
                        fn.Parameters[j].ResolvedType = SubstituteDynamicArray(fn.Parameters[j].ResolvedType);
                        currentScope.Symbols[fn.Parameters[j].Identifier].Type = fn.Parameters[j].ResolvedType;
                    }
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
        foreach(TypeNamePair field in structDeclaration.Fields)
            if(CanSubstituteDynamicArray(field.ResolvedType, out TypeInfo newType))
            {
                field.ResolvedType = newType;
                currentScope.Symbols[field.Identifier].Type = field.ResolvedType;   
            }

        foreach (FunctionDeclaration function in structDeclaration.Functions)
            AnalyseFunctionBody(function);

        ExitScope();
    }

    static void AnalyseUnion(UnionDeclaration unionDeclaration)
    {
        currentScope = unionDeclaration.Scope!;
        foreach(TypeNamePair variant in unionDeclaration.Variants)
            if(CanSubstituteDynamicArray(variant.ResolvedType, out TypeInfo newType))
            {
                variant.ResolvedType = newType;
                currentScope.Symbols[variant.Identifier].Type = variant.ResolvedType;   
            }

        // foreach (FunctionDeclaration function in unionDeclaration.Functions)
        //     AnalyseFunctionBody(function);

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

            if (statement is VariableDeclaration varDeclaration)
            {
                _ = CanSubstituteDynamicArray(varDeclaration.ResolvedType!, out TypeInfo newType);

                // Log.Info($"{varDeclaration.ResolvedType!.TypeName} -> {newType.TypeName}");

                VariableDeclaration vd = new VariableDeclaration(newType.TypeName, varDeclaration.Name, varDeclaration.Init, SourePosition.None);
                vd.ResolvedType = newType;
                statements[i] = vd;
            }
        }
    }

    static bool CanSubstituteDynamicArray(TypeInfo sourceType, out TypeInfo newType)
    {
        if(sourceType.ArrayLength == 0)
        {
            if(CanSubstituteDynamicArray(sourceType.ElementType!, out TypeInfo elementType))
                sourceType.ElementType = elementType;
            
            newType = SubstituteDynamicArray(sourceType);
            
            return true;
        }
        else if(sourceType.Pointee != null)
        {
            if(CanSubstituteDynamicArray(sourceType.Pointee, out TypeInfo pointeeType))
            {
                newType = new TypeInfo($"@{pointeeType.TypeName}", Pointer, 8, pointeeType);
                return true;
            }
        }

        newType = sourceType;
        return true;
    }

    static TypeInfo SubstituteDynamicArray(TypeInfo sourceType)
    {
        // Log.Info(typeName);
        // (1) check if that kind of array is already declared as struct
        if (DynamicArrayCache.TryGetValue(sourceType.TypeName, out SymbolInfo? symbolInfo))
        {
            return symbolInfo.Type;
            //currentScope!.Symbols[varDeclaration.Name] = symbolInfo;
        }
        // (2) if no -> declare the struct
        else
        {
            string structName = $"{DYNAMIC_ARRAY_PREFIX}-{sourceType.ElementType.TypeName}";
            StructDeclaration sd = CreateDynamicArrayStruct(structName, sourceType.ElementType);

            // TODO: make this work with inits once we have them for arrays
            TypeInfo newType = sd.ResolvedType;
            SymbolInfo varInfo = new SymbolInfo(sourceType.TypeName,
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
            GlobalScope.Declare(
            new SymbolInfo(
                "@" + sd.Name,
                new TypeInfo("@" + sd.Name, Pointer, pointee: sd.ResolvedType),
                SymbolKind.Type,
                null
            ));

            return newType;
        }
    }

    #region Helpers

    static StructDeclaration CreateDynamicArrayStruct(string name, TypeInfo elementType)
    {
        Scope scope = currentScope;
        currentScope = GlobalScope;
        EnterNewScope(name);

        List<TypeNamePair> fields = [
            new TypeNamePair(new TypeInfo("s64", Scalar, 8), "Length"),
            new TypeNamePair(
                    new TypeInfo($"@{elementType.TypeName}", 
                        Pointer, 
                        8, 
                        elementType), 
                "Data"),
            new TypeNamePair(new TypeInfo("s64", Scalar, 8), "Size"),
        ];

        List<FunctionDeclaration> functions = [
            new FunctionDeclaration(
                typeName:   elementType.TypeName, 
                typeKind:   elementType.TypeKind,
                name:       DYNAMIC_ARRAY_INDEX,
                paramaters: [new TypeNamePair(new TypeInfo("s64", Scalar, 8), "index")],
                statements: [new ReturnStatement(new UnaryExpression(UnaryOperator.Dereference,
                                new CallExpression(new IdentifierExpression($"@{elementType.TypeName}"),[
                                    new CallExpression(new IdentifierExpression($"@void"),[
                                        new BinaryExpression(new CallExpression(new IdentifierExpression("s64"),[
                                            new IdentifierExpression("Data")
                                        ]),
                                        BinaryOperator.Add,
                                        new BinaryExpression(new CallExpression(new IdentifierExpression("TypeSize"),[
                                            new IdentifierExpression($"{elementType.TypeName}")
                                        ]),
                                        BinaryOperator.Multiply,
                                        new IdentifierExpression("index")))
                                    ])
                                ]))
            )])];

        functions[0].Scope = new Scope(functions[0].Name, functions[0], currentScope);

        foreach (TypeNamePair field in fields)
            currentScope.Declare(new SymbolInfo(field.Identifier,
                                                new TypeInfo(field.ResolvedType.TypeName, field.ResolvedType.TypeKind),
                                                SymbolKind.Variable,
                                                null));

        foreach(FunctionDeclaration function in functions)
            currentScope.Declare(new SymbolInfo(
                        function.Name,
                        function.ResolvedType,
                        SymbolKind.Function,
                        function.Parameters));

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