using System.Security.Cryptography.X509Certificates;

namespace TheiaLang;

public static class SemanticAnalyser
{
    static Scope GlobalScope;
    static Scope currentScope;
    public static void AnalyseProgram(ProgramNode program, Scope globalScope)
    {
        GlobalScope = globalScope;
        currentScope = globalScope;

        // Struct types are already fully resolved in the parser,
        // however, their Fields still aren't as they may be structs themselves
        // so we should resolve them here first. 
        // Later, we want to do whatever applies to structs for all composite types.
        foreach (INode node in program.Nodes)
            if (node is StructDeclaration sd)
                ResolveStructFields(sd);

        // Now, we want to resolve all function return and argument types so we 
        // don't get into order dependency issues later.
        // This of course includes functions that are defined in the scope of
        // composite types!
        foreach (INode node in program.Nodes)
            switch (node)
            {
                case StructDeclaration sd:
                    ResolveFunctionsInStruct(sd);
                    break;

                case UnionDeclaration ud:
                    // nothing more to do right now
                    break;

                case FunctionDeclaration fn:
                    ResolveFunctionTypeAndArgs(fn);
                    break;
            }

        // Now we can actually resolve the function bodies
        foreach (INode node in program.Nodes)
        {
            if (node is StructDeclaration sd)
                AnalyseStruct(sd);

            if (node is FunctionDeclaration fn)
                AnalyseFunctionBody(fn);
        }
    }

    static void ResolveStructFields(StructDeclaration structDeclaration)
    {
        currentScope = structDeclaration.Scope!;
        foreach (TypeNamePair field in structDeclaration.Fields)
        {
            currentScope.TryLookup(field.Name, out SymbolInfo fieldInfo, out _);
            fieldInfo.Type = GetTypeInfo(field.TypeName);
            field.ResolvedType = fieldInfo.Type;
        }

        ExitScope();
    }

    static void ResolveFunctionsInStruct(StructDeclaration structDeclaration)
    {
        currentScope = structDeclaration.Scope!;
        foreach (FunctionDeclaration function in structDeclaration.Functions)
            ResolveFunctionTypeAndArgs(function);
        ExitScope();
    }

    static void ResolveFunctionTypeAndArgs(FunctionDeclaration function)
    {
        currentScope = function.Scope!;
        currentScope.TryLookup(function.Name, out SymbolInfo? functionInfo, out _);
        functionInfo!.Type = GetTypeInfo(function.ResolvedType.TypeName);
        function.ResolvedType = functionInfo.Type;

        foreach (TypeNamePair arg in function.Arguments)
        {
            arg.ResolvedType = GetTypeInfo(arg.TypeName);
            // function arguments do not get declared in the Parser so we do it here
            currentScope.Declare(arg.Name, new SymbolInfo(arg.Name, arg.ResolvedType, SymbolKind.Variable, null));
        }

        ExitScope();
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
        foreach (IStatement statement in function.Statements)
            AnalyseStatement(statement);

        ExitScope();
    }

    static void AnalyseStatement(IStatement statement)
    {
        switch (statement)
        {
            case VariableDeclaration variable:
                currentScope.TryLookup(variable.Name, out SymbolInfo? symbolInfo, out _);
                symbolInfo.Type = GetTypeInfo(variable.TypeName);
                variable.ResolvedType = symbolInfo.Type;

                if (variable.Init != null)
                {
                    AnalyseExpression(variable.Init);

                    if (!CanImplicitlyCast(variable.ResolvedType.TypeName,
                                           variable.Init.ResolvedType.TypeName))
                        throw new Exception($"Cannot initialise variable '{variable.ResolvedType.TypeName} {variable.Name}'"
                                            + $" with type '{variable.Init.ResolvedType.TypeName}'"
                                            + "due to invompatible types or possible loss of information");
                }
                break;
            case AssignmentStatement assignment:
                assignment.Target.ResolvedType = GetTypeInfo(assignment.Target.Name);

                AnalyseExpression(assignment.Expression);

                if (!CanImplicitlyCast(assignment.Target.ResolvedType.TypeName,
                       assignment.Expression.ResolvedType.TypeName))
                    throw new Exception($"Cannot implicitly convert between '{assignment.Target.ResolvedType}'" +
                                        $" and '{assignment.Expression.ResolvedType.TypeName}'");
                break;
            case ExpressionStatement expression:
                AnalyseExpression(expression.Expression);
                break;
            case ReturnStatement returnStatement:
                AnalyseExpression(returnStatement.Expression);

                if (currentScope.DeclaringNode is FunctionDeclaration function)
                    if (!CanImplicitlyCast(function.ResolvedType.TypeName,
                                           returnStatement.Expression.ResolvedType.TypeName))
                        throw new Exception($"Invalid return type: '{returnStatement.Expression.ResolvedType.TypeName}'" +
                                            $" cannot be implicitly converted to '{function.ResolvedType.TypeName}'");
                break;
        }
    }

    static void AnalyseExpression(IExpression expression)
    {
        switch (expression)
        {
            case LiteralExpression literal:
                // this is very bare-bones for now
                string type = "";
                if (literal.Value is int)
                    type = "s32";
                else if (literal.Value is double)
                    type = "f32";
                else if (literal.Value is bool)
                    type = "bool";
                else
                    throw new Exception($"Unknown literal type {literal.Value}");
                literal.ResolvedType = new TypeInfo(type, null, null);

                break;
            case IdentifierExpression identifier:
                identifier.ResolvedType = GetTypeInfo(identifier.Name);
                break;
            case CallExpression call:
                if (!currentScope.TryLookup(call.CalleeName, out SymbolInfo? function, out _)
                    || function!.Kind != SymbolKind.Function)
                    throw new Exception($"Unknown function '{call.CalleeName}'");

                if (call.Arguments.Count != function.Parameters!.Count)
                    throw new Exception($"Function '{function.Name}' expects {function.Parameters.Count}"
                                        + $" arguments, got {call.Arguments.Count}");

                foreach (IExpression arg in call.Arguments)
                    AnalyseExpression(arg);

                for (int i = 0; i < call.Arguments.Count; i++)
                {
                    string actual = call.Arguments[i].ResolvedType!.TypeName;
                    string expected = function.Parameters[i].ResolvedType!.TypeName;
                    if (!CanImplicitlyCast(actual, expected))
                        throw new Exception(
                          $"Cannot implicitly convert {actual} to {expected}");
                }

                call.ResolvedType = function.Type;
                break;
            case MemberAccessExpression memberAccess:

                AnalyseExpression(memberAccess.Target);
                TypeInfo targetInfo = memberAccess.Target.ResolvedType!;

                if (targetInfo.FieldNames == null)
                    throw new Exception($"{memberAccess.Target.Name} has no fields");

                int memberIndex = targetInfo.FieldNames!.IndexOf(memberAccess.Member.Name);
                if (memberIndex < 0)
                    throw new Exception($"Field '{memberAccess.Member.Name}' is not defined in {targetInfo.TypeName}");

                currentScope.TryLookup(targetInfo.FieldTypes![memberIndex], out SymbolInfo? memberInfo, out _);
                memberAccess.ResolvedType = memberInfo!.Type;
                break;
            case UnaryExpression unary:
                AnalyseExpression(unary.Operand);
                unary.ResolvedType = unary.Operand.ResolvedType;
                break;
            case BinaryExpression binary:
                AnalyseExpression(binary.Left);
                AnalyseExpression(binary.Right);

                binary.ResolvedType = GetTypeInfo(GetBinaryOpReturnType(binary.Op,
                    binary.Left.ResolvedType.TypeName,
                    binary.Right.ResolvedType.TypeName));
                break;
            case InstantiationExpression instantiation:
                if (!currentScope.TryLookup(instantiation.TypeName, out SymbolInfo? typeSymbolInfo, out _)
                    || typeSymbolInfo!.Kind != SymbolKind.Type)
                    throw new Exception($"Unknown type '{instantiation.TypeName}'");

                int fieldCount = typeSymbolInfo.Type.FieldNames!.Count;
                if (instantiation.Arguments.Count != fieldCount)
                    throw new Exception($"Constructor for type '{instantiation.TypeName}' requires {fieldCount} arguments, got:"
                                        + $" {instantiation.Arguments.Count}");

                for (int i = 0; i < instantiation.Arguments.Count; i++)
                {
                    AnalyseExpression(instantiation.Arguments[i]);
                    // check implicit cast from arg type → field type
                    if (!CanImplicitlyCast(instantiation.Arguments[i].ResolvedType.TypeName!,
                        typeSymbolInfo.Parameters![i].TypeName))
                        throw new Exception("Type mismatch in ctor");
                }
                instantiation.ResolvedType = typeSymbolInfo.Type;
                break;
        }
    }

    #region Helpers

    static TypeInfo GetTypeInfo(string typeOrName)
    {
        if (!currentScope.TryLookup(typeOrName, out SymbolInfo? symbolInfo, out _))
            throw new Exception($"Type '{typeOrName}' is not defined in {currentScope.FullName}");

        return symbolInfo!.Type;
    }

    static void ExitScope()
    {
        if (currentScope.Parent == null)
            throw new Exception($"Tried to exit scope {currentScope.FullName}");

        currentScope = currentScope.Parent;
    }

    #endregion

    #region Interoperability
    static readonly bool[,] LoslessTypeInterop = new bool[11, 11]
    {
        //from  \  to   bool    s8      s16     s32     s64     s128    s256    f16     f32     f64     f128
        /*bool  */  {   true,   false,  false,  false,  false,  false,  false,  false,  false,  false,  false },
        /*s8    */  {   false,  true,   true,   true,   true,   true,   true,   false,  false,  false,  false },
        /*s16   */  {   false,  false,  true,   true,   true,   true,   true,   false,  false,  false,  false },
        /*s32   */  {   false,  false,  false,  true,   true,   true,   true,   false,  false,  false,  false },
        /*s64   */  {   false,  false,  false,  false,  true,   true,   true,   false,  false,  false,  false },
        /*s128  */  {   false,  false,  false,  false,  false,  true,   true,   false,  false,  false,  false },
        /*s256  */  {   false,  false,  false,  false,  false,  false,  true,   false,  false,  false,  false },
        /*f16   */  {   false,  false,  false,  false,  false,  false,  false,  true,   false,  false,  false },
        /*f32   */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   false,  false },
        /*f64   */  {   false,  false,  false,  false,  false,  false,  false,  false,  false,  true,   false },
        /*f128  */  {   false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true  },
    };

    static readonly bool[,] ScalarTypeInterop = new bool[11, 11]
    {
        //              bool    s8      s16     s32     s64     s128    s256    f16     f32     f64     f128
        /*bool  */  {   true,   false,  false,  false,  false,  false,  false,  false,  false,  false,  false },
        /*s8    */  {   false,  true,   true,   true,   true,   true,   true,   false,  false,  false,  false },
        /*s16   */  {   false,  true,   true,   true,   true,   true,   true,   false,  false,  false,  false },
        /*s32   */  {   false,  true,   true,   true,   true,   true,   true,   false,  false,  false,  false },
        /*s64   */  {   false,  true,   true,   true,   true,   true,   true,   false,  false,  false,  false },
        /*s128  */  {   false,  true,   true,   true,   true,   true,   true,   false,  false,  false,  false },
        /*s256  */  {   false,  true,   true,   true,   true,   true,   true,   false,  false,  false,  false },
        /*f16   */  {   false,  false,  false,  false,  false,  false,  false,  true,   false,  false,  false },
        /*f32   */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   false,  false },
        /*f64   */  {   false,  false,  false,  false,  false,  false,  false,  false,  false,  true,   false },
        /*f128  */  {   false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true  },
    };

    static readonly string[,] ImplicitPromotion = new string[11, 11]
    {
        // A   \    B   bool    s8      s16     s32     s64     s128    s256    f16     f32     f64     f128
        /*bool  */  {   "bool", "",     "",     "",     "",     "",     "",     "",     "",     "",     ""     },
        /*s8    */  {   "",     "s8",   "s16",  "s32",  "s64",  "s128", "s256", "",     "",     "",     ""     },
        /*s16   */  {   "",    "s16",   "s16",  "s32",  "s64",  "s128", "s256", "",     "",     "",     ""     },
        /*s32   */  {   "",    "s32",   "s32",  "s32",  "s64",  "s128", "s256", "",     "",     "",     ""     },
        /*s64   */  {   "",    "s64",   "s64",  "s64",  "s64",  "s128", "s256", "",     "",     "",     ""     },
        /*s128  */  {   "",    "s128",  "s128", "s128", "s128", "s128", "s256", "",     "",     "",     ""     },
        /*s256  */  {   "",    "s256",  "s256", "s256", "s256", "s256", "s256", "",     "",     "",     ""     },
        /*f16   */  {   "",    "",      "",     "",     "",     "",     "",     "f16",  "",     "",     ""     },
        /*f32   */  {   "",    "",      "",     "",     "",     "",     "",     "",     "f32",  "",     ""     },
        /*f64   */  {   "",    "",      "",     "",     "",     "",     "",     "",     "",     "f64",  ""     },
        /*f128  */  {   "",    "",      "",     "",     "",     "",     "",     "",     "",     "",     "f128" },
    };

    static string GetBinaryOpReturnType(BinaryOperator binaryOperator, string typeA, string typeB)
    {
        string shared = GetImplicitPromotionType(typeA, typeB)!;

        if (binaryOperator == BinaryOperator.Equal
            || binaryOperator == BinaryOperator.NotEqual)
            return "bool";

        if (shared != "bool" && IRGenerator.IsBuiltinType(shared))
            if (binaryOperator == BinaryOperator.Greater || binaryOperator == BinaryOperator.Less)
                return "bool";
            else
                return shared;

        throw new Exception($"Operator '{binaryOperator}' is not valid for type {shared}");
    }

    static bool CanTypesInteropScalar(string typeA, string typeB)  // a + b; a * b;
    {
        int indexA = IRGenerator.BuiltinTypeIndex(typeA);
        int indexB = IRGenerator.BuiltinTypeIndex(typeB);

        if (indexA < 0 || indexB < 0)   // scalar math only allowed for built in types
            return false;

        return ScalarTypeInterop[indexA, indexB];
    }

    static bool CanImplicitlyCast(string typeA, string typeB)  // a + b; a * b;
    {
        int indexA = IRGenerator.BuiltinTypeIndex(typeA);
        int indexB = IRGenerator.BuiltinTypeIndex(typeB);

        if (indexA < 0 || indexB < 0)   // composite: can only assign to same type
            return typeA == typeB;

        return LoslessTypeInterop[indexA, indexB];
    }

    static string? GetImplicitPromotionType(string typeA, string typeB)
    {
        int i = IRGenerator.BuiltinTypeIndex(typeA);
        int j = IRGenerator.BuiltinTypeIndex(typeB);

        string? result = null;

        if (i >= 0 && j >= 0)       // only promote built-in types
            result = ImplicitPromotion[i, j];
        else
        {
            if (typeA == typeB)     // two of the same structs can still interop for comparison
                result = typeA;
        }

        if (string.IsNullOrEmpty(result))
            result = null;

        if (result == null)
            Log.Error(13, $"Cannot implicitly convert between {typeA} and {typeB}");

        return result;
    }
    #endregion
}