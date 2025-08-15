namespace TheiaLang;

static class Elaboration
{
    // the template we use for dynamic arrays
    const string DYNAMIC_ARRAY_HOOK = "Dynamic_Array";

    static Dictionary<string, SymbolInfo> DynamicArrayCache = [];
    static SymbolInfo DynamicArrayTemplate;
    static Scope currentScope;
    static ProgramNode ProgramAST;
    static Scope GlobalScope;

    public static (ProgramNode programAST, Scope programScope) Lower(Scope preloadScope, ProgramNode preloadAST,
                                                                        Scope programScope, ProgramNode programAST)
    {
        GlobalScope = programScope;
        ProgramAST = programAST;
        if (!preloadScope.TryLookup(DYNAMIC_ARRAY_HOOK, out DynamicArrayTemplate!, out _))
            throw new Exception($"Could not find template {DYNAMIC_ARRAY_HOOK}");

        currentScope = GlobalScope;

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
        for (int i = 0; i < statements.Count; i++)
        {
            IStatement statement = statements[i];
            if (statement is VariableDeclaration varDeclaration
             && varDeclaration.ResolvedType?.ArrayLengths?.Count == 0)
            {
                string typeName = varDeclaration.ResolvedType.ElementType!.TypeName;
                string structName = $"{DYNAMIC_ARRAY_HOOK}_{typeName}";
                // (1) check if that kind of array is already declared as struct
                if (DynamicArrayCache.TryGetValue(typeName, out SymbolInfo? symbolInfo))
                {
                    statements[i] = new VariableDeclaration(structName,
                                                            varDeclaration.Name,
                                                            null);
                    currentScope!.Symbols[varDeclaration.Name] = symbolInfo;
                }
                // (2) if no  -> declare the struct
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
                    GlobalScope.Declare(structName,
                                        new SymbolInfo(sd.Name,
                                                       sd.ResolvedType,
                                                       SymbolKind.Type,
                                                       sd.Fields));

                    currentScope = sd.Scope;
                    foreach (TypeNamePair field in sd.Fields)
                        currentScope.Declare(field.Name,
                                             new SymbolInfo(field.TypeName,
                                                            new TypeInfo(field.TypeName),
                                                            SymbolKind.Variable,
                                                            null));
                    currentScope = variableScope;
                }

                // (3) replace the current variable declaration with a struct instantiation



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