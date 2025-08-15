using System.Data;
using LLVMSharp.Interop;

namespace TheiaLang;

static class Elaboration
{
    // everything with this type placeholder has to be substituted with
    // the concrete type that is being used
    const string monomorphisationHook = "__subst_Type";

    // the template we use for dynamic arrays
    const string DYNAMIC_ARRAY_HOOK = "Dynamic_Array";

    static Dictionary<string, SymbolInfo> DynamicArrayCache = [];
    static SymbolInfo DynamicArrayTemplate;
    static Scope currentScope;
    static ProgramNode ProgramAST;

    public static void Simplify(Scope preloadScope, ProgramNode preloadAST,
                                Scope programScope, ProgramNode programAST)
    {
        ProgramAST = programAST;
        if (!preloadScope.TryLookup(DYNAMIC_ARRAY_HOOK, out DynamicArrayTemplate, out _))
            throw new Exception($"Could not find template {DYNAMIC_ARRAY_HOOK}");

        currentScope = programScope;

        for (int i = 0; i < programAST.Nodes.Count; i++)
        {
            INode node = programAST.Nodes[i];

            if (node is StructDeclaration sd)
                AnalyseStruct(sd);

            if (node is FunctionDeclaration fn)
                AnalyseFunctionBody(fn);
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
        Log.Info(function.ToString());
        currentScope = function.Scope!;
        AnalyseStatements(function.Statements);

        ExitScope();
    }

    static void AnalyseStatements(List<IStatement> statements)
    {
        for (int i = 0; i < statements.Count; i++)
        {
            IStatement statement = statements[i];
            if (statement is VariableDeclaration variableDeclaration
             && variableDeclaration.ResolvedType?.ArrayLengths?.Count == 0)
            {
                string typeName = variableDeclaration.ResolvedType.ElementType!.TypeName;
                Log.Info($"Dynamic Array: {typeName}");
                // (1) check if that kind of array is already declared as struct
                if (DynamicArrayCache.ContainsKey(typeName))
                {

                }
                // (2) if no  -> declare the struct
                else
                {
                    StructDeclaration sd = CreateStructFromSymbol(DynamicArrayTemplate);
                    sd.Scope = new Scope($"{DYNAMIC_ARRAY_HOOK}_{typeName}",
                                         sd,
                                         currentScope);

                    Log.Info(sd.Scope.ToString());

                    sd.Name += $"_{typeName}";
                    ProgramAST.Nodes.Add(sd);
                    currentScope.Declare($"{DYNAMIC_ARRAY_HOOK}_{typeName}",
                                         new SymbolInfo(sd.Name,
                                                        sd.ResolvedType,
                                                        SymbolKind.Type,
                                                        sd.Fields));
                    currentScope = sd.Scope;
                    Log.Info(currentScope.ToString());

                    foreach (TypeNamePair field in sd.Fields)
                        currentScope.Declare(field.Name,
                                             new SymbolInfo(field.TypeName,
                                                            new TypeInfo(field.TypeName),
                                                            SymbolKind.Variable,
                                                            null));

                    ExitScope();

                    Log.Info(sd.ToString());
                }

                // (3) replace the current variable declaration with a struct instantiation



            }
        }
    }

    #region Helpers


    static StructDeclaration CreateStructFromSymbol(SymbolInfo symbolInfo)
    {
        List<TypeNamePair> fields = [];
        for (int i = 0; i < symbolInfo.Type.FieldNames!.Count; i++)
            fields.Add(new TypeNamePair(symbolInfo.Type.FieldTypes![i],
                                        symbolInfo.Type.FieldNames[i]));

        return new StructDeclaration(symbolInfo.Name,
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