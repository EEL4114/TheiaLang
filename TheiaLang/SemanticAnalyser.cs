namespace TheiaLang;

public static class SemanticAnalyser
{
    static Scope currentScope = new Scope("");
    public static (ProgramNode, Scope) AnalyseProgram(ProgramNode program, Scope globalScope)
    {
        currentScope = globalScope;

        // Struct types are already fully resolved in the parser,
        // however, their Fields still aren't as they may be structs themselves
        // so we should resolve them here first. 
        // Later, we want to do whatever applies to structs for all composite types.
        foreach (INode node in program.Nodes)
            if (node is StructDeclaration sd)
                ResolveStruct(sd);

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
                    ResolveUnion(ud);
                    break;

                case FunctionDeclaration fn:
                    ResolveFunctionTypeAndArgs(fn);
                    break;
            }

        // we do not need to deal with variable declarations here since they will always appear
        // before they are used, in contrast to composite fields 

        // Now we can actually resolve the function bodies
        foreach (INode node in program.Nodes)
        {
            if (node is StructDeclaration sd)
                AnalyseStruct(sd);

            if (node is FunctionDeclaration fn)
                AnalyseFunctionBody(fn);
        }

        return (program, globalScope);
    }

    #region Functions, Structs

    static void ResolveStruct(StructDeclaration structDeclaration)
    {
        uint size = 0;
        currentScope = structDeclaration.Scope!;
        foreach (TypeNamePair field in structDeclaration.Fields)
        {
            currentScope.TryLookup(field.Name, out SymbolInfo? fieldInfo, out _);
#if DEBUG
            if (fieldInfo == null)
                throw new Exception($"Could not find struct field '{field.Name}' in {currentScope.FullName}");
#endif
            fieldInfo.Type = ResolveType(field.TypeName);
            size += fieldInfo.Type.Size;
            field.ResolvedType = fieldInfo.Type;
        }

        structDeclaration.ResolvedType.Size = size;
        ExitScope();
    }

    static void ResolveUnion(UnionDeclaration unionDeclaration)
    {
        uint size = 0;
        currentScope = unionDeclaration.Scope!;

        foreach (TypeNamePair variant in unionDeclaration.Variants)
        {
            currentScope.TryLookup(variant.Name, out SymbolInfo? variantInfo, out _);
#if DEBUG
            if (variantInfo == null)
                throw new Exception($"Could not find union variant '{variant.Name}' in {currentScope.FullName}");
#endif
            variantInfo.Type = ResolveType(variant.TypeName);
            size = Math.Max(variantInfo.Type.Size, size);
            variant.ResolvedType = variantInfo.Type;
        }

        unionDeclaration.ResolvedType.Size = size;

        ExitScope();
    }

    // TODO: generalise this a bit more (unions etc.)
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

    #endregion

    #region  Statements

    static void AnalyseStatement(IStatement statement)
    {
        switch (statement)
        {
            case VariableDeclaration variable:
                currentScope.TryLookup(variable.Name, out SymbolInfo? symbolInfo, out _);
                // Log.Info(symbolInfo.Type.TypeName + " " + variable.Name + " " + (symbolInfo.Type.Pointee != null).ToString());
                if (symbolInfo == null)
                    throw new Exception($"Could not find variable '{variable.Name}' in {currentScope.FullName}");
                if (variable.ResolvedType != null)
                    symbolInfo.Type = UpdateTypeInfo(variable.ResolvedType);
                else
                    symbolInfo.Type = ResolveType(variable.TypeName);
                variable.ResolvedType = symbolInfo.Type;

                if (variable.Init != null)
                {
                    variable.Init = AnalyseExpression(variable.Init);
                    variable.Init.ResolvedType = PromoteIfLiteral(variable.Init.ResolvedType!, variable.ResolvedType.TypeName);

                    if (!CanImplicitlyCast(variable.ResolvedType.TypeName,
                                           variable.Init.ResolvedType.TypeName))
                        throw new Exception($"Cannot initialise variable {variable.ResolvedType.TypeName} '{variable.Name}'"
                                            + $" with type '{variable.Init.ResolvedType.TypeName}'"
                                            + " due to incompatible types or possible loss of information");
                }
                break;
            case AssignmentStatement assignment:
                assignment.Target = AnalyseExpression(assignment.Target);
                assignment.Expression = AnalyseExpression(assignment.Expression);

                assignment.Expression.ResolvedType = PromoteIfLiteral(assignment.Expression.ResolvedType!,
                                                                      assignment.Target.ResolvedType!.TypeName);

                if (!CanImplicitlyCast(assignment.Target.ResolvedType.TypeName,
                       assignment.Expression.ResolvedType.TypeName))
                {
                    Log.Info($"{assignment}");
                    throw new Exception($"Cannot implicitly convert between  {assignment.Target.ResolvedType.TypeName}" +
                                        $" and {assignment.Expression.ResolvedType.TypeName}");
                }
                break;
            case CompoundAssignmentStatement compound:
                compound.Target = AnalyseExpression(compound.Target);
                compound.Expression = AnalyseExpression(compound.Expression);

                compound.Expression.ResolvedType = PromoteIfLiteral(compound.Expression.ResolvedType!,
                                                                    compound.Target.ResolvedType!.TypeName);

                if (!CanImplicitlyCast(compound.Target.ResolvedType.TypeName,
                       compound.Expression.ResolvedType.TypeName))
                    throw new Exception($"Cannot implicitly convert between  {compound.Target.ResolvedType.TypeName}" +
                                        $" and {compound.Expression.ResolvedType.TypeName}");
                break;
            case ExpressionStatement expression:
                expression.Expression = AnalyseExpression(expression.Expression);
                break;
            case IfStatement ifStatement:
                ifStatement.Condition = AnalyseExpression(ifStatement.Condition);

                if (ifStatement.Condition.ResolvedType!.TypeName != "bool")
                    throw new Exception($"Condition of if statement must resolve to type 'bool', got: {ifStatement.Condition.ResolvedType.TypeName}");

                EnterScope(ifStatement.ThenScope);
                foreach (IStatement thenStatement in ifStatement.ThenBranch)
                    AnalyseStatement(thenStatement);
                ExitScope();

                if (ifStatement.ElseBranch != null)
                {
                    EnterScope(ifStatement.ElseScope!);
                    foreach (IStatement elseStatement in ifStatement.ElseBranch)
                        AnalyseStatement(elseStatement);
                    ExitScope();
                }
                break;
            case ForStatement forStatement:
                EnterScope(forStatement.Scope);
                AnalyseStatement(forStatement.Initialiser!);
                forStatement.Condition = AnalyseExpression(forStatement.Condition!);

                if (forStatement.Condition.ResolvedType!.TypeName != "bool")
                    throw new Exception($"Condition of for loop must resolve to type 'bool', got: {forStatement.Condition.ResolvedType.TypeName}");

                AnalyseStatement(forStatement.Iterator!);

                foreach (IStatement s in forStatement.Body)
                    AnalyseStatement(s);

                ExitScope();
                break;
            case ReturnStatement returnStatement:
                if (returnStatement.Expression != null)
                    returnStatement.Expression = AnalyseExpression(returnStatement.Expression);
                else
                {
                    returnStatement.Expression = new LiteralExpression(null!, "void");
                    returnStatement.Expression.ResolvedType = new TypeInfo("void");
                }

                if (currentScope.DeclaringNode is FunctionDeclaration function)
                {
                    returnStatement.Expression.ResolvedType = PromoteIfLiteral(returnStatement.Expression.ResolvedType!,
                                                                               function.ResolvedType.TypeName);
                    if (!CanImplicitlyCast(function.ResolvedType.TypeName,
                                           returnStatement.Expression.ResolvedType.TypeName))
                        throw new Exception($"Invalid return type: '{returnStatement.Expression.ResolvedType.TypeName}'" +
                                            $" cannot be implicitly converted to '{function.ResolvedType.TypeName}'");
                }
                break;
            default:
                throw new Exception($"Unknown Statement: {statement.GetType().Name}");
        }
    }

    #endregion

    #region  Expressions

    static IExpression AnalyseExpression(IExpression expression)
    {
        switch (expression)
        {
            case LiteralExpression:
                // these have already been resolved in the Parser
                break;
            case IdentifierExpression identifier:
                TypeInfo identifierInfo = GetTypeInfo(identifier.Name);
                identifier.ResolvedType = identifierInfo;
                if (identifierInfo.Pointee != null)     // ptr variable: dereference is default
                    expression = new UnaryExpression(UnaryOperator.Dereference, identifier, true) { ResolvedType = identifierInfo.Pointee };
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
                    call.Arguments[i].ResolvedType = PromoteIfLiteral(call.Arguments[i].ResolvedType!,
                                                                      function.Parameters[i].ResolvedType!.TypeName);
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
                memberAccess.Member.ResolvedType = memberInfo!.Type;
                memberAccess.ResolvedType = memberInfo!.Type;
                break;
            case UnaryExpression unary:
                unary.Operand = AnalyseExpression(unary.Operand);
                if (unary.Op == UnaryOperator.Negate && unary.Operand is LiteralExpression literal)
                {
                    if (unary.Operand.ResolvedType!.TypeName == "bool")
                        Log.Info("Invalid operation '-' on type 'bool'");
                    else
                    {
                        object v;
                        string lexeme = "-" + literal.Lexeme;
                        if (literal.ResolvedType!.TypeName == "int")
                        {
                            int.TryParse(lexeme, out int j);
                            v = j;
                        }
                        else
                        {
                            double.TryParse(lexeme, out double j);
                            v = j;
                        }

                        LiteralExpression literalExpression = new LiteralExpression(v, lexeme);
                        literalExpression.ResolvedType = literal.ResolvedType;
                        expression = literalExpression;
                        AnalyseExpression(expression);
                    }
                }
                if (unary.Op == UnaryOperator.AddressOf)
                {
                    if (unary.Operand is UnaryExpression operandExpression      // reference of a dereference of a ptr
                        && operandExpression.Op == UnaryOperator.Dereference)   // -> redundant
                        expression = operandExpression.Operand;
                    else
                        unary.ResolvedType = new TypeInfo("@" + unary.Operand.ResolvedType!.TypeName,
                                                          SizeOf("@" + unary.Operand.ResolvedType!.TypeName),
                                                          pointee: unary.Operand.ResolvedType);
                }
                else
                {
                    unary.ResolvedType = unary.Operand.ResolvedType;
                }
                break;
            case BinaryExpression binary:
                binary.Left = AnalyseExpression(binary.Left);
                binary.Right = AnalyseExpression(binary.Right);

                binary.Left.ResolvedType = PromoteIfLiteral(binary.Left.ResolvedType!, binary.Right.ResolvedType!.TypeName);
                binary.Right.ResolvedType = PromoteIfLiteral(binary.Right.ResolvedType, binary.Left.ResolvedType.TypeName);

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
                    instantiation.Arguments[i] = AnalyseExpression(instantiation.Arguments[i]);
                    // check implicit cast from arg type → field type
                    instantiation.Arguments[i].ResolvedType = PromoteIfLiteral(instantiation.Arguments[i].ResolvedType!,
                                                                               typeSymbolInfo.Parameters![i].TypeName);
                    if (!CanImplicitlyCast(instantiation.Arguments[i].ResolvedType!.TypeName!,
                        typeSymbolInfo.Parameters![i].TypeName))
                        throw new Exception("Type mismatch in ctor");
                }
                instantiation.ResolvedType = typeSymbolInfo.Type;
                break;
            case IndexExpression indexExpression:
                indexExpression.Target = AnalyseExpression(indexExpression.Target);
                indexExpression.Index = AnalyseExpression(indexExpression.Index);
                // TODO this is kinda unsafe
                indexExpression.Index.ResolvedType = PromoteIfLiteral(indexExpression.Index.ResolvedType!, "s32");

                if (!CanTypesInteropScalar(indexExpression.Index.ResolvedType!.TypeName, "int"))
                    throw new Exception($"Invalid index type: '{indexExpression.Index.ResolvedType.TypeName}'");
                if (indexExpression.Target.ResolvedType!.ArrayLength == null)
                    throw new Exception($"Expected array type, got: {indexExpression.Target.ResolvedType.TypeName}");
                indexExpression.ResolvedType = indexExpression.Target.ResolvedType.ElementType;
                break;
            default: throw new Exception($"Unsupported expression: {expression.GetType()}");
        }

        return expression;
    }

    #endregion

    #region Helpers

    static TypeInfo UpdateTypeInfo(TypeInfo type)
    {
        if (type.ArrayLength != null)
        {
            type.ElementType = UpdateTypeInfo(type.ElementType!);
            // TODO!!
            type.Size = SizeOf(type);
            return type;
        }
        else if (type.Pointee != null)
        {
            type.Pointee = UpdateTypeInfo(type.Pointee);
            type.Size = SizeOf(type);
            return type;
        }
        else
            return ResolveType(type.TypeName);
    }

    static TypeInfo ResolveType(string typeOrIdentifier)
    {
        TypeInfo typeInfo = GetTypeInfo(typeOrIdentifier);
        typeInfo.Size = SizeOf(typeInfo.TypeName);
        return typeInfo;
    }

    static TypeInfo GetTypeInfo(string typeOrName)
    {
        if (!currentScope.TryLookup(typeOrName, out SymbolInfo? symbolInfo, out _))
            throw new Exception($"Type or Name '{typeOrName}' is not defined in {currentScope.FullName}");

        return symbolInfo!.Type;
    }

    static uint SizeOf(TypeInfo type)
    {
        int i = IRGenerator.BuiltinTypeIndex(type.TypeName);
        if (i >= 0)
            return SizeOfBuiltin[i];
        if (type.TypeName.StartsWith('@'))
            return 8;   // ptrs are 64-bit == 8 B

        if (type.FieldTypes != null)
        {
            uint size = 0;
            foreach (string fieldTypeName in type.FieldTypes!)
            {
                _ = ResolveType(fieldTypeName);
                size += SizeOf(fieldTypeName);
            }

            return size;
        }
        else if (type.ArrayLength is uint length && type.ElementType != null)
            return length! * SizeOf(type.ElementType);

        throw new Exception($"Cannot determine size of type '{type.TypeName}'");
    }

    public static uint SizeOf(string typeName)
    {
        int i = IRGenerator.BuiltinTypeIndex(typeName);
        if (typeName.StartsWith('@'))
            return 8;   // ptrs are 64-bit == 8 B
        if (i >= 0)
            return SizeOfBuiltin[i];

        TypeInfo type = GetTypeInfo(typeName);
        // @Speed this is wasteful and dirt cheap to fix
        return SizeOf(type);
    }

    static readonly uint[] SizeOfBuiltin =
    [
        //  bool    int     s8      s16     s32     s64     s128    s256   float    f16     f32     f64     f128    void
            1,      0,      1,      2,      4,      8,      16,     32,     0,      2,      4,      8,      16,     0
    ];

    static void EnterScope(Scope scope)
    {
        if (currentScope == null)
            throw new Exception("'currentScope' is null!");

        if (!currentScope.Children.ContainsValue(scope))    // verify that we can enter that scope
            Log.Error(14, $"Scope '{scope}' does not exist in '{currentScope}'");

        currentScope = scope;
    }

    static void ExitScope()
    {
        if (currentScope.Parent == null)
            throw new Exception($"Tried to exit scope {currentScope}");

        currentScope = currentScope.Parent;
    }

    #endregion

    #region Interop
    static readonly bool[,] LosslessTypeInterop = new bool[14, 14]
    {
        //from  \  to   bool    int     s8      s16     s32     s64     s128    s256    float   f16     f32     f64     f128    void
        /*bool  */  {   true,   false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true},
        /*int   */  {   false,  true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true ,  true},
        /*s8    */  {   false,  true,   true,   true,   true,   true,   true,   true,   false,  false,  false,  false,  false,  true},
        /*s16   */  {   false,  true,   false,  true,   true,   true,   true,   true,   false,  false,  false,  false,  false,  true},
        /*s32   */  {   false,  true,   false,  false,  true,   true,   true,   true,   false,  false,  false,  false,  false,  true},
        /*s64   */  {   false,  true,   false,  false,  false,  true,   true,   true,   false,  false,  false,  false,  false,  true},
        /*s128  */  {   false,  true,   false,  false,  false,  false,  true,   true,   false,  false,  false,  false,  false,  true},
        /*s256  */  {   false,  true,   false,  false,  false,  false,  false,  true,   false,  false,  false,  false,  false,  true},
        /*float */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   true,   true,   true,   true,   true},
        /*f16   */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   true,   false,  false,  false,  true},
        /*f32   */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   false,  true,   false,  false,  true},
        /*f64   */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   false,  false,  true,   false,  true},
        /*f128  */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   false,  false,  false,  true,   true},
        /*void  */  {   true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true},
    };

    static readonly bool[,] ScalarTypeInterop = new bool[13, 13]
    {
        //  A   \   B   bool    int     s8      s16     s32     s64     s128    s256    float   f16     f32     f64     f128
        /*bool  */  {   true,   false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false },
        /*int   */  {   false,  true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true  },
        /*s8    */  {   false,  true,   true,   true,   true,   true,   true,   true,   false,  false,  false,  false,  false },
        /*s16   */  {   false,  true,   true,   true,   true,   true,   true,   true,   false,  false,  false,  false,  false },
        /*s32   */  {   false,  true,   true,   true,   true,   true,   true,   true,   false,  false,  false,  false,  false },
        /*s64   */  {   false,  true,   true,   true,   true,   true,   true,   true,   false,  false,  false,  false,  false },
        /*s128  */  {   false,  true,   true,   true,   true,   true,   true,   true,   false,  false,  false,  false,  false },
        /*s256  */  {   false,  true,   true,   true,   true,   true,   true,   true,   false,  false,  false,  false,  false },
        /*float */  {   false,  true,   false,  false,  false,  false,  false,  false,  true,   true,   true,   true,   true  },
        /*f16   */  {   false,  true,   false,  false,  false,  false,  false,  false,  true,   true,   false,  false,  false },
        /*f32   */  {   false,  true,   false,  false,  false,  false,  false,  false,  true,   false,  true,   false,  false },
        /*f64   */  {   false,  true,   false,  false,  false,  false,  false,  false,  true,   false,  false,  true,   false },
        /*f128  */  {   false,  true,   false,  false,  false,  false,  false,  false,  true,   false,  false,  false,  true  },
    };

    static readonly string[,] ImplicitPromotion = new string[13, 13]
    {
        // A   \    B   bool    int      s8      s16     s32     s64     s128    s256   float   f16     f32     f64     f128
        /*bool  */  {   "bool", "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     "",     ""     },
        /*int   */  {   "",     "int",  "s8",   "s16",  "s32",  "s64",  "s128", "s256", "float","f16",  "f32",  "f64",  "f128 "},
        /*s8    */  {   "",     "s8",   "s8",   "s16",  "s32",  "s64",  "s128", "s256", "",     "",     "",     "",     ""     },
        /*s16   */  {   "",     "s16",  "s16",  "s16",  "s32",  "s64",  "s128", "s256", "",     "",     "",     "",     ""     },
        /*s32   */  {   "",     "s32",  "s32",  "s32",  "s32",  "s64",  "s128", "s256", "",     "",     "",     "",     ""     },
        /*s64   */  {   "",     "s64",  "s64",  "s64",  "s64",  "s64",  "s128", "s256", "",     "",     "",     "",     ""     },
        /*s128  */  {   "",     "s128", "s128", "s128", "s128", "s128", "s128", "s256", "",     "",     "",     "",     ""     },
        /*s256  */  {   "",     "s256", "s256", "s256", "s256", "s256", "s256", "s256", "",     "",     "",     "",     ""     },
        /*float */  {   "",     "float","",      "",     "",     "",     "",     "",    "float","f16",  "f32",  "f64",  "f128" },
        /*f16   */  {   "",     "f16",  "",      "",     "",     "",     "",     "",    "f16",  "f16",  "",     "",     ""     },
        /*f32   */  {   "",     "f32",  "",      "",     "",     "",     "",     "",    "f32",  "",     "f32",  "",     ""     },
        /*f64   */  {   "",     "f64",  "",      "",     "",     "",     "",     "",    "f64",  "",     "",     "f64",  ""     },
        /*f128  */  {   "",     "f128", "",      "",     "",     "",     "",     "",    "f128", "",     "",     "",     "f128" },
    };

    static string GetBinaryOpReturnType(BinaryOperator binaryOperator, string typeA, string typeB)
    {
        string shared = GetImplicitPromotionType(typeA, typeB)!;

        if (binaryOperator == BinaryOperator.EqualEqual
         || binaryOperator == BinaryOperator.NotEqual)
            return "bool";

        if (typeA == "bool" && typeB == "bool"
         && binaryOperator == BinaryOperator.AND
         || binaryOperator == BinaryOperator.OR)
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
        if (typeA.StartsWith('@') && typeB.StartsWith('@'))
        {
            return CanImplicitlyCast(typeA.TrimStart('@'),
                                     typeB.TrimStart('@'));
        }

        int indexA = IRGenerator.BuiltinTypeIndex(typeA);
        int indexB = IRGenerator.BuiltinTypeIndex(typeB);

        if (indexA < 0 || indexB < 0)   // composite: can only assign to same type
            return typeA == typeB;

        return LosslessTypeInterop[indexA, indexB];
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

    static TypeInfo PromoteIfLiteral(TypeInfo typeInfo, string expectedType)
    {
        if (typeInfo.TypeName.StartsWith('@'))
            return typeInfo;

        int builtinA = IRGenerator.BuiltinTypeIndex(typeInfo.TypeName);
        int builtinB = IRGenerator.BuiltinTypeIndex(expectedType);

        if (builtinA != 1 && builtinA != 8)     // 1 == 'int'; 8 == 'float'
            return typeInfo;

        if (builtinA < 0 || builtinB < 0)
            throw new Exception($"Cannot resolve {typeInfo} to {expectedType}");

        if (LosslessTypeInterop[builtinA, builtinB])
        {
            TypeInfo type = new TypeInfo(expectedType);
            type.Size = SizeOf(type.TypeName);
            return type;    // literals always cast to the more concrete value
        }

        throw new Exception($"Cannot implicitly convert {typeInfo.TypeName} to {expectedType}");
    }

    #endregion
}