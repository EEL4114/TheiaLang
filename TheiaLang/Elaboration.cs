namespace TheiaLang;

static class Elaboration
{
    // the template we use for dynamic arrays
    const string DYNAMIC_ARRAY_HOOK = "Dynamic_Array";

    static Dictionary<string, SymbolInfo> DynamicArrayCache = [];
    static SymbolInfo DynamicArrayTemplate;
    static Scope currentScope;
    static ProgramNode PreloadAST;
    static ProgramNode ProgramAST;
    static Scope GlobalScope;
    static Scope PreloadScope;

    public static (ProgramNode programAST, Scope programScope) Lower(Scope preloadScope, ProgramNode preloadAST,
                                                                     Scope programScope, ProgramNode programAST)
    {
        GlobalScope = programScope;
        ProgramAST = programAST;
        PreloadAST = preloadAST;
        PreloadScope = preloadScope;

        if (!preloadScope.TryLookup(DYNAMIC_ARRAY_HOOK, out DynamicArrayTemplate!, out _))
            throw new Exception($"Could not find template {DYNAMIC_ARRAY_HOOK}");

        currentScope = GlobalScope;

        IncludeIntrinsicFn("AllocB");
        IncludeIntrinsicFn("ReallocB");
        IncludeIntrinsicFn("Free");

        for (int i = 0; i < ProgramAST.Nodes.Count; i++)
        {
            INode node = ProgramAST.Nodes[i];

            if (node is StructDeclaration sd)
                AnalyseStruct(sd);

            if (node is FunctionDeclaration fn)
                AnalyseFunctionBody(fn);
        }

        return (ProgramAST, GlobalScope);
    }

    static void IncludeIntrinsicFn(string name)
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
        // we could have dynamic arrays occur in the following places
        // 1 - declared directly:   []s32
        // 2 - as a pointee:        @[]s32
        // 3 - as an array element: [4][]s32
        // or any combination of the above

        // for each dynamic array type, we need:
        // - struct type
        // - array read fn
        // - array write fn
        // for the struct type, we can use a void ptr
        // but for array manipulation, we will need to monomorphise,
        // so we will need to generate a proper TypeInfo too

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
                string typeName = varDeclaration.ResolvedType.ElementType!.TypeName;
                string structName = $"{DYNAMIC_ARRAY_HOOK}_{typeName}";
                // Log.Info(typeName);
                // (1) check if that kind of array is already declared as struct
                if (DynamicArrayCache.TryGetValue(typeName, out SymbolInfo? symbolInfo))
                {
                    statements[i] = new VariableDeclaration(structName,
                                                            varDeclaration.Name,
                                                            null);
                    currentScope!.Symbols[varDeclaration.Name] = symbolInfo;
                }
                // (2) if no -> declare the struct
                else
                {
                    Scope variableScope = currentScope;
                    StructDeclaration sd = CreateStructFromSymbol(DynamicArrayTemplate, structName);
                    sd.Name = structName;
                    sd.Scope = new Scope(structName, sd, GlobalScope);
                    sd.Scope.DeclaringNode = sd;

                    // TODO: make this work with inits once we have them for arrays
                    VariableDeclaration vd = new VariableDeclaration(structName,
                                                                     varDeclaration.Name,
                                                                     null);

                    vd.ResolvedType = sd.ResolvedType;
                    SymbolInfo varInfo = new SymbolInfo(vd.Name,
                                                        sd.ResolvedType,
                                                        SymbolKind.Variable,
                                                        null);
                    currentScope!.Symbols[vd.Name] = varInfo;
                    DynamicArrayCache.Add(typeName, varInfo);
                    statements[i] = vd;

                    ProgramAST.Nodes.Add(sd);
                    GlobalScope.Declare(new SymbolInfo(sd.Name,
                                                       sd.ResolvedType,
                                                       SymbolKind.Type,
                                                       sd.Fields));

                    currentScope = sd.Scope;
                    foreach (TypeNamePair field in sd.Fields)
                        currentScope.Declare(new SymbolInfo(field.Identifier,
                                                            new TypeInfo(field.TypeName),
                                                            SymbolKind.Variable,
                                                            null));
                    currentScope = variableScope;
                }
            }
        }
    }

    #region Helpers

    static StructDeclaration CreateStructFromSymbol(SymbolInfo symbolInfo, string name = "")
    {
        if (name == "")
            name = symbolInfo.Name;

        List<TypeNamePair> fields = [];
        for (int i = 0; i < symbolInfo.Type.FieldNames!.Count; i++)
            fields.Add(new TypeNamePair(symbolInfo.Type.FieldTypes![i],
                                        symbolInfo.Type.FieldNames[i]));

        return new StructDeclaration(name,
                                     fields,
                                     []);
    }

    static void ExitScope()
    {
        if (currentScope.Parent == null)
            throw new Exception($"Tried to exit scope {currentScope}");

        currentScope = currentScope.Parent;
    }

    #endregion

}