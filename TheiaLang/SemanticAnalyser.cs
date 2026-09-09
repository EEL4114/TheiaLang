namespace TheiaLang;

using static CastOp;
using static TypeKind;
using static BuiltinType;

public static class SemanticAnalyser
{
    static Scope currentScope = new Scope("");
    static Scope GlobalScope;
    public static (ProgramNode, Scope) AnalyseProgram(ProgramNode program, Scope globalScope)
    {
        currentScope = globalScope;
        GlobalScope = currentScope;

        // Struct types are already fully resolved in the parser,
        // however, their Fields still aren't as they may be structs themselves
        // so we should resolve them here first. 
        // Later, we want to do whatever applies to structs for all composite types.
        foreach (INode node in program.Nodes)
        {
            if(node is StructDeclaration sd)
                ResolveStruct(sd);
            if(node is UnionDeclaration ud)
                ResolveUnion(ud);   
        }

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

                // case UnionDeclaration ud:
                //     ResolveUnion(ud);
                //     break;

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
            // TODO: simplify!!
            currentScope.TryLookupLocal(field.Identifier, out SymbolInfo? fieldInfo, out _);
#if DEBUG
            if (fieldInfo == null)
                throw new Exception($"Could not find struct field '{field.Identifier}' in {currentScope.FullName}");
#endif
            if (fieldInfo.Type != null)
            {
                // Log.Info(fieldInfo.Type.TypeName);   
                fieldInfo.Type = UpdateTypeInfo(fieldInfo.Type);
            }
            else
                fieldInfo.Type = ResolveType(field.ResolvedType.TypeName);
            size += fieldInfo.Type.Size;
            field.ResolvedType = fieldInfo.Type;
        }

        structDeclaration.ResolvedType.FieldTypes = structDeclaration.Fields
                                                        .Select(f => f.ResolvedType)
                                                        .ToList();

        structDeclaration.ResolvedType.Size = size;
        ExitScope();
    }

    static void ResolveUnion(UnionDeclaration unionDeclaration)
    {
        uint size = 0;
        currentScope = unionDeclaration.Scope!;

        foreach (TypeNamePair variant in unionDeclaration.Variants)
        {
            // TODO: simplify!!
            currentScope.TryLookupLocal(variant.Identifier, out SymbolInfo? variantInfo, out _);
// #if DEBUG
            if (variantInfo == null)
                throw new Exception($"Could not find union variant '{variant.Identifier}' in {currentScope.FullName}");
// #endif
            variantInfo.Type = UpdateTypeInfo(variantInfo.Type);
            size = Math.Max(variantInfo.Type.Size, size);

            variant.ResolvedType = variantInfo.Type;
        }

        unionDeclaration.ResolvedType.FieldTypes = unionDeclaration.Variants
                                                       .Select(v => v.ResolvedType)
                                                       .ToList();

        unionDeclaration.ResolvedType.Size = size;
        ExitScope();
    }

    // TODO: generalise this a bit more (unions etc.)
    static void ResolveFunctionsInStruct(StructDeclaration structDeclaration)
    {
        currentScope = structDeclaration.Scope!;
        foreach (FunctionDeclaration function in structDeclaration.Functions)
        {
            TypeInfo ptrType = new TypeInfo($"@{structDeclaration.ResolvedType.TypeName}", Pointer, PTR, IRGenerator.PTR_SIZE, structDeclaration.ResolvedType);
            function.Parameters.Insert(0, new TypeNamePair(ptrType, "this", SourePosition.None));
            ResolveFunctionTypeAndArgs(function);
        }
        ExitScope();
    }

    static void ResolveFunctionTypeAndArgs(FunctionDeclaration function)
    {
        currentScope = function.Scope!;
        currentScope.TryLookup(function.Name, out SymbolInfo? functionInfo, out _);
        functionInfo!.Type = GetTypeInfo(function.ResolvedType.TypeName);
        function.ResolvedType = functionInfo.Type;

        foreach (TypeNamePair arg in function.Parameters)
        {
            // TODO: simplify!!
            arg.ResolvedType = GetTypeInfo(arg.ResolvedType.TypeName);

            if(currentScope.TryLookupLocal(arg.Identifier, out SymbolInfo? argInfo, out _))
                argInfo!.Type = arg.ResolvedType;
            else
                currentScope.Declare(new SymbolInfo(arg.Identifier, arg.ResolvedType, SymbolKind.Variable, null));
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
                //Log.Info($"{variable.Name}");
                if (currentScope.TryLookupLocal(variable.Name, out _, out _))
                    throw new Exception($"A Variable with the name '{variable.Name}' is already defined in {currentScope.FullName}");

                if (variable.ResolvedType != null)
                    variable.ResolvedType = UpdateTypeInfo(variable.ResolvedType);
                else
                    variable.ResolvedType = ResolveType(variable.TypeName);

                currentScope!.Declare(new SymbolInfo(
                    variable.Name,
                    variable.ResolvedType,
                    SymbolKind.Variable,
                    
                    null
                ));

                if (variable.Init != null)
                {
                    variable.Init = AnalyseExpression(variable.Init);
                    if (variable.Init.ResolvedType == null)
                        Log.Error(23, $"{variable.Name} gets initialized with an expression that didn't resolve to a type correctly");
                    variable.Init.ResolvedType = PromoteIfLiteral(variable.Init.ResolvedType!, variable.ResolvedType.TypeName);

                    variable.Init = GenerateImplicitCast(variable.Init, variable.ResolvedType);
                }
                break;
            case AssignmentStatement assignment:
                assignment.Target = AnalyseExpression(assignment.Target);
                assignment.Expression = AnalyseExpression(assignment.Expression);

                assignment.Expression.ResolvedType = PromoteIfLiteral(assignment.Expression.ResolvedType!,
                                                                      assignment.Target.ResolvedType!.TypeName);

                assignment.Expression = GenerateImplicitCast(assignment.Expression, assignment.Target.ResolvedType);

                break;
            case CompoundAssignmentStatement compound:
                compound.Target = AnalyseExpression(compound.Target);
                compound.Expression = AnalyseExpression(compound.Expression);

                compound.Expression.ResolvedType = PromoteIfLiteral(compound.Expression.ResolvedType!,
                                                                    compound.Target.ResolvedType!.TypeName);

                compound.Expression = GenerateImplicitCast(compound.Expression, compound.Target.ResolvedType);

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
                    returnStatement.Expression = new LiteralExpression(null!, "void", SourePosition.None);
                    returnStatement.Expression.ResolvedType = new TypeInfo("void", Void, VOID);
                }

                if (currentScope.DeclaringNode is FunctionDeclaration function)
                {
                    returnStatement.Expression.ResolvedType = PromoteIfLiteral(returnStatement.Expression.ResolvedType!,
                                                                               function.ResolvedType.TypeName);
                    returnStatement.Expression = GenerateImplicitCast(returnStatement.Expression, function.ResolvedType);
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
                if (!currentScope.TryLookup(identifier.Name, out _, out Scope? symbolScope))
                    Log.Error(21, $"{identifier.Pos} {identifier.Name} is not defined in {currentScope.Name}");

                TypeInfo identifierInfo = GetSymbolType(identifier.Name);
                identifier.ResolvedType = identifierInfo;
                identifier.Scope = symbolScope;
                break;
            case CallExpression call:
                foreach (IExpression arg in call.Arguments)
                    AnalyseExpression(arg);
                if (call.Target is IdentifierExpression id)
                {
                    if (id.Name == "TypeSize")
                    {
                        if (call.Arguments.Count != 1)
                            throw new Exception($"Function 'SizeOf' expects 1 argument, got {call.Arguments.Count}");

                        if (call.Arguments[0] is not IdentifierExpression i)
                            throw new Exception($"Unexpected argument in call 'TypeSize': expected identifer, got: {call.Arguments[0].GetType()}");

                        /*TypeInfo typeInfo;
                        if (currentScope.TryLookup(id.Name, out SymbolInfo? symbolInfo, out _))
                            typeInfo = symbolInfo!.Type;
                        else
                            throw new Exception($"Could not resolve {id.Name}");
                        */
                        // Log.Info($"{call.CalleeName} {id.Name} {typeInfo}");

                        uint sizeValue = SizeOf(i.Name);

                        expression = new LiteralExpression((int)sizeValue, sizeValue.ToString(), SourePosition.None);
                        expression.ResolvedType = new TypeInfo("s32", Scalar, S32, 4);
                    }
                    else if (id.Name == "Alloc")
                    {
                        Log.Info("ALLOC");
                    }
                    else if (StringToBuiltin(id.Name) != null)
                    {
                        BuiltinType builtinType = (BuiltinType)StringToBuiltin(id.Name)!;
                        for (int i = 0; i < call.Arguments.Count; i++)
                        {
                            IExpression argument = AnalyseExpression(call.Arguments[i]);
                            call.Arguments[i] = argument;
                        }
                        if (builtinType != PTR)
                        {
                            
                            int targetTypeIndex = Builtins.BuiltinTypeIndex(builtinType);
                            int sourceTypeIndex = Builtins.BuiltinTypeIndex(call.Arguments[0].ResolvedType!.BuiltinType);

                            if (targetTypeIndex < 0 || sourceTypeIndex < 0)
                            {
                                if(targetTypeIndex == Builtins.BuiltinTypeIndex(S64) && call.Arguments[0].ResolvedType!.BuiltinType == PTR)
                                    sourceTypeIndex = Builtins.BuiltinTypeIndex(VOID);
                                else
                                    throw new Exception($"Invalid cast: {call.Arguments[0].ResolvedType!.TypeName} -> {id.Name}");
                            }                            

                            CastOp? op = TypeCast[sourceTypeIndex, targetTypeIndex];
                            if (op == null)
                                throw new Exception($"Invalid cast: {call.Arguments[0].ResolvedType!.TypeName} -> {id.Name}");
                            if (op == NoOp)
                            {    
                                expression = call.Arguments[0];
                                expression.ResolvedType = GetTypeInfo(id.Name);
                            }
                            else
                            {
                                expression = new CastExpression((CastOp)op, call.Arguments[0], SourePosition.None);
                                expression.ResolvedType = GetTypeInfo(id.Name);
                            }
                        }
                        else if(id.Name.StartsWith('@'))
                        {
                            if(call.Arguments[0].ResolvedType!.Pointee != null)
                            {   
                                expression = call.Arguments[0];
                                expression.ResolvedType = GetTypeInfo(id.Name);
                            }
                            else if(call.Arguments[0].ResolvedType!.TypeName == "s64")
                            {
                                expression = new CastExpression(IntToPtr, call.Arguments[0], SourePosition.None);
                                expression.ResolvedType = GetTypeInfo(id.Name);
                            }
                            else
                                Log.Error(24, $"Invalid tpye cast: {call.Target.ResolvedType.TypeName} -> {id.Name}");
                        }
                        else
                        {
                            call.ResolvedType = GetTypeInfo(id.Name);
                            call.Scope = GlobalScope;
                        }
                    }
                    else
                    {
                        call.Target = AnalyseExpression(call.Target);

                        if (id.Scope != null)
                        {
                            if (id.Scope.TryLookup(id.Name, out SymbolInfo? sInfo, out Scope? defScope)
                             && sInfo!.Kind == SymbolKind.Function)
                            {
                                if (call.Arguments.Count != sInfo.Parameters.Count)
                                    throw new Exception($"Function '{sInfo.Name}' expects {sInfo.Parameters.Count}"
                                        + $" arguments, got {call.Arguments.Count}");
                                AnalyseArguments(call, sInfo);
                                call.Scope = defScope;
                            }
                        }
                        else
                        {
                            if (!currentScope.TryLookup(id.Name, out SymbolInfo? info, out Scope? defScope)
                                || info!.Kind != SymbolKind.Function)
                                throw new Exception($"Unknown function '{id.Name}'");

                            AnalyseArguments(call, info);
                            call.Scope = defScope;
                        }
                    }
                }
                else if (call.Target is MemberAccessExpression memberAccess)    // convert to method call
                {
                    memberAccess.Target = AnalyseExpression(memberAccess.Target);
                    currentScope.TryFindChild(memberAccess.Target.ResolvedType.TypeName, out Scope? defScope);
                    
                    Scope scope = currentScope;
                    EnterScope(defScope!);
                    IdentifierExpression idExpr = (IdentifierExpression)AnalyseExpression(memberAccess.Member);
                    
                    if (!currentScope.TryLookup(idExpr.Name, out SymbolInfo? info, out _)
                                || info!.Kind != SymbolKind.Function)
                                throw new Exception($"Unknown function '{idExpr.Name}'");

                    memberAccess.Member = idExpr;
                    currentScope = scope;

                    IExpression addressOfExpression = AnalyseExpression(new UnaryExpression(UnaryOperator.AddressOf, memberAccess.Target, SourePosition.None));

                    call.Arguments.Insert(0, addressOfExpression);
                    call.Target = memberAccess.Member;
                    call.ResolvedType = memberAccess.Member.ResolvedType;
                    call.Scope = defScope;

                    AnalyseArguments(call, info);
                }
                else
                {
                    Log.Info("DD");
                }
                break;
            case MemberAccessExpression memberAccess:
                Scope s = currentScope;
                AnalyseExpression(memberAccess.Target);

                if (memberAccess.Target is IdentifierExpression targetEx
                 && targetEx.ResolvedType != null)
                {
                    if (!currentScope.TryFindChild(targetEx.ResolvedType.TypeName, out Scope? targetScope))
                        throw new Exception($"{memberAccess.Pos} Could not find {targetEx.ResolvedType.TypeName} in {currentScope.FullName}");
                    SetScope(targetScope!);
                }
                else if (memberAccess.Target is MemberAccessExpression mem && mem.Scope != null)
                    SetScope(mem.Scope);
                else
                    throw new Exception("ff");

                memberAccess.Member = (IdentifierExpression)AnalyseExpression(memberAccess.Member);
                memberAccess.ResolvedType = memberAccess.Member.ResolvedType;

                if (memberAccess.Member.ResolvedType!.BuiltinType == null)
                {
                    if (!currentScope!.TryFindChild(memberAccess.Member.ResolvedType!.TypeName, out Scope? memberScope))
                        throw new Exception($"{memberAccess.Pos} Could not find {memberAccess.Member.ResolvedType!.TypeName} in {currentScope.FullName}");

                    memberAccess.Scope = memberScope;
                }
                currentScope = s;
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

                        LiteralExpression literalExpression = new LiteralExpression(v, lexeme, SourePosition.None);
                        literalExpression.ResolvedType = literal.ResolvedType;
                        expression = literalExpression;
                        AnalyseExpression(expression);
                    }
                }
                else if (unary.Op == UnaryOperator.AddressOf)
                {
                    if (unary.Operand is UnaryExpression operandExpression      // reference of a dereference of a ptr
                        && operandExpression.Op == UnaryOperator.Dereference)   // -> redundant

                        expression = operandExpression.Operand;
                    else
                    {
                        if (unary.Operand.Assignable == false)
                            Log.Error(22, $"Can't take address of non-assignable {unary.Operand}");
                        unary.ResolvedType = new TypeInfo("@" + unary.Operand.ResolvedType!.TypeName,
                                                          Pointer,
                                                          PTR, 
                                                          SizeOf("@" + unary.Operand.ResolvedType!.TypeName),
                                                          pointee: unary.Operand.ResolvedType);
                    }
                }
                else if (unary.Op == UnaryOperator.Dereference)
                {
                    if (unary.Operand.ResolvedType!.Pointee == null)
                        Log.Error(20, $"Can't dereference non-pointer type {unary.Operand.ResolvedType}");
                    unary.ResolvedType = unary.Operand.ResolvedType!.Pointee;
                }
                else
                    unary.ResolvedType = unary.Operand.ResolvedType;
                break;
            case BinaryExpression binary:
                binary.Left = AnalyseExpression(binary.Left);
                binary.Right = AnalyseExpression(binary.Right);

                binary.Left.ResolvedType = PromoteIfLiteral(binary.Left.ResolvedType!, binary.Right.ResolvedType!.TypeName);
                binary.Right.ResolvedType = PromoteIfLiteral(binary.Right.ResolvedType, binary.Left.ResolvedType.TypeName);

                binary.ResolvedType = GetBinaryOpReturnType(binary.Op,
                    binary.Left.ResolvedType,
                    binary.Right.ResolvedType);
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
                    // check implicit cast from arg type -> field type
                    instantiation.Arguments[i].ResolvedType = PromoteIfLiteral(instantiation.Arguments[i].ResolvedType!,
                                                                               typeSymbolInfo.Parameters![i].ResolvedType.TypeName);

                    instantiation.Arguments[i] = GenerateImplicitCast(instantiation.Arguments[i], typeSymbolInfo.Parameters![i].ResolvedType!);
                }
                instantiation.ResolvedType = typeSymbolInfo.Type;
                break;
            case IndexExpression indexExpression:
                indexExpression.Target = AnalyseExpression(indexExpression.Target);
                indexExpression.Index = AnalyseExpression(indexExpression.Index);
                indexExpression.Index.ResolvedType = PromoteIfLiteral(indexExpression.Index.ResolvedType!, "s64");

                if (!CanTypesInteropScalar(indexExpression.Index.ResolvedType!.BuiltinType, INT))
                    throw new Exception($"Invalid index type: '{indexExpression.Index.ResolvedType.TypeName}'");
                // dynamic array
                if (indexExpression.Target.ResolvedType!.ArrayLength == null)
                {
                    if(currentScope.TryFindChild(indexExpression.Target.ResolvedType.TypeName, out Scope? definitionScope))
                    {
                        if(definitionScope.Children.ContainsKey(Elaboration.DYNAMIC_ARRAY_INDEX))
                        {
                            IExpression ex = new CallExpression(
                                                new MemberAccessExpression(
                                                    indexExpression.Target, 
                                                    new IdentifierExpression($"{Elaboration.DYNAMIC_ARRAY_INDEX}", SourePosition.None), SourePosition.None), 
                                                [indexExpression.Index],
                                                SourePosition.None,
                                                definitionScope);
                            expression = AnalyseExpression(ex);
                        }        
                    }
                    else
                        throw new Exception($"Expected array type, got: {indexExpression.Target.ResolvedType.TypeName}");
                }
                else
                    indexExpression.ResolvedType = indexExpression.Target.ResolvedType.ElementType;
                break;
            case CastExpression cast:
                    
                break;
            default: throw new Exception($"Unsupported expression: {expression.GetType()}");
        }
        
        return expression;
    }

    #endregion

    #region Helpers

    static void AnalyseArguments(CallExpression call, SymbolInfo sInfo)
    {
        if (call.Arguments.Count != sInfo.Parameters.Count)
            throw new Exception($"Function '{sInfo.Name}' expects {sInfo.Parameters.Count}"
                + $" arguments, got {call.Arguments.Count}");
        for (int i = 0; i < call.Arguments.Count; i++)
        {
            IExpression argument = AnalyseExpression(call.Arguments[i]);

            call.Arguments[i] = argument;
            call.Arguments[i].ResolvedType = PromoteIfLiteral(argument.ResolvedType!,
                                                            sInfo.Parameters[i].ResolvedType!.TypeName);
            call.Arguments[i] = GenerateImplicitCast(call.Arguments[i], sInfo.Parameters[i].ResolvedType!);
        }
        call.ResolvedType = sInfo.Type;
    }

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
        if (typeOrName.StartsWith('@'))
            return new TypeInfo(typeOrName, Pointer, PTR, IRGenerator.PTR_SIZE, GetTypeInfo(typeOrName[1..]));

        if (!currentScope.TryLookup(typeOrName, out SymbolInfo? symbolInfo, out _))
            throw new Exception($"Type or Name '{typeOrName}' is not defined in {currentScope.FullName}");

        if(symbolInfo!.Kind != SymbolKind.Type)
            throw new Exception($"'{typeOrName}' is not a valid type in {currentScope.FullName}");

        return symbolInfo!.Type;
    }

    static TypeInfo GetSymbolType(string name)
    {
        if (!currentScope.TryLookup(name, out SymbolInfo? symbolInfo, out _))
            throw new Exception($"Type or Name '{name}' is not defined in {currentScope.FullName}");

        return symbolInfo!.Type;
    }

    public static uint SizeOf(TypeInfo type)
    {
        int i = Builtins.BuiltinTypeIndex(type.BuiltinType);
        if (i >= 0)
            return SizeOfBuiltin[i];
        if (type.TypeName.StartsWith('@'))
            return IRGenerator.PTR_SIZE;

        if (type.FieldTypes != null)
        {
            uint size = 0;
            foreach(TypeInfo fieldType in type.FieldTypes!)
            {
                // _ = ResolveType(fieldTypeName);
                size += SizeOf(fieldType);
            }

            return size;
        }
        else if (type.ArrayLength is uint length && type.ElementType != null)
            return length * SizeOf(type.ElementType);
        else
            return type.Size;

        throw new Exception($"Cannot determine size of type '{type.TypeName}'");
    }

    public static uint SizeOf(string typeName)
    {
        int i = Builtins.BuiltinTypeIndex(StringToBuiltin(typeName));

        if (typeName.StartsWith('@'))
            return IRGenerator.PTR_SIZE;
        if (i >= 0)
            return SizeOfBuiltin[i];

        TypeInfo type = GetTypeInfo(typeName);
        // @Speed this is wasteful and dirt cheap to fix
        return SizeOf(type);
    }

    public static readonly uint[] SizeOfBuiltin =
    [
        //  bool    int     s8      s16     s32     s64     s128    s256   float    f16     f32     f64     f128    void    ptr
            1,      0,      1,      2,      4,      8,      16,     32,     0,      2,      4,      8,      16,     0,      8,
    ];

    static Scope SetScope(Scope scope)
    {
        Scope previous = currentScope;
        currentScope   = scope;

        return previous;
    }

    static void EnterScope(Scope scope)
    {
        if (currentScope == null)
            throw new Exception("'currentScope' is null!");

        if(!currentScope.TryFindChild(scope.Name, out _) && currentScope.Parent != scope)
            Log.Error(14, $"Scope '{scope}' does not exist in '{currentScope}'");

        currentScope = scope;
    }

    static void ExitScope()
    {
        currentScope = currentScope.Exit();
    }

    static IExpression GenerateImplicitCast(IExpression expression, TypeInfo target)
    {
        TypeInfo sourceType = expression.ResolvedType!;

        // string sourceType = expression.ResolvedType!.TypeName;
        // string targetType = target.TypeName;

        if (!CanImplicitlyCast(sourceType, target))
            throw new Exception($"{expression.Pos}: Cannot implicitly convert {sourceType.TypeName} -> {target.TypeName}");

        if (sourceType != target)
        {
            if (sourceType.BuiltinType != null
             && target.BuiltinType != null)
            {
                CastOp? op = TypeCast[Builtins.BuiltinTypeIndex(sourceType.BuiltinType),
                                      Builtins.BuiltinTypeIndex(target.BuiltinType)];
                if (op == null)
                    throw new Exception($"Invalid cast: {sourceType.TypeName} -> {target.TypeName}");
                expression = new CastExpression((CastOp)op, expression, SourePosition.None);
                expression.ResolvedType = target;
            }
        }
        return expression;
    }

    #endregion

    #region Interop
    
    static readonly bool[,] LosslessTypeInterop = new bool[15, 15]
    {
        //from  \  to   bool    int     s8      s16     s32     s64     s128    s256    float   f16     f32     f64     f128    void
        /*bool  */  {   true,   false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false},
        /*int   */  {   false,  true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true ,  false,  false},
        /*s8    */  {   false,  true,   true,   true,   true,   true,   true,   true,   false,  false,  false,  false,  false,  false,  false},
        /*s16   */  {   false,  true,   false,  true,   true,   true,   true,   true,   false,  false,  false,  false,  false,  false,  false},
        /*s32   */  {   false,  true,   false,  false,  true,   true,   true,   true,   false,  false,  false,  false,  false,  false,  false},
        /*s64   */  {   false,  true,   false,  false,  false,  true,   true,   true,   false,  false,  false,  false,  false,  false,  false},
        /*s128  */  {   false,  true,   false,  false,  false,  false,  true,   true,   false,  false,  false,  false,  false,  false,  false},
        /*s256  */  {   false,  true,   false,  false,  false,  false,  false,  true,   false,  false,  false,  false,  false,  false,  false},
        /*float */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   true,   true,   true,   true,   false,  false},
        /*f16   */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   true,   false,  false,  false,  false,  false},
        /*f32   */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   false,  true,   false,  false,  false,  false},
        /*f64   */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   false,  false,  true,   false,  false,  false},
        /*f128  */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   false,  false,  false,  true,   false,  false},
        /*void  */  {   false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true,   true },
        /*ptr  */   {   false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true,   true },
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

    static BuiltinType? StringToBuiltin(string s)
    {
        if(s.StartsWith('@'))
            return PTR;
        
        return s switch
        {
            "void" => VOID,

            "bool" => BOOL,

            "int"  => INT,
            "s8"   => S8,
            "s16"  => S16,
            "s32"  => S32,
            "s64"  => S64,
            "s128" => S128,
            "s256" => S256,

            "u8"   => U8,
            "u16"  => U16,
            "u32"  => U32,
            "u64"  => U64,
            "u128" => U128,
            "u256" => U256,

            "float"=> FLOAT,
            "f16"  => F16,
            "f32"  => F32,
            "f64"  => F64,
            "f128" => F128,
            _      => null
        };
    }

    static readonly BuiltinType?[,] ImplicitPromotion = new BuiltinType?[13, 13]
    {
        // A   \    B   bool    int     s8      s16     s32     s64     s128    s256    float   f16     f32     f64     f128
        /*bool  */  {   BOOL,   null,   null,   null,   null,   null,   null,   null,   null,   null,   null,   null,   null },
        /*int   */  {   null,   INT,    S8,     S16,    S32,    S64,    S128,   S256,   FLOAT,  F16,    F32,    F64,    F128 },
        /*s8    */  {   null,   S8,     S8,     S16,    S32,    S64,    S128,   S256,   null,   null,   null,   null,   null },
        /*s16   */  {   null,   S16,    S16,    S16,    S32,    S64,    S128,   S256,   null,   null,   null,   null,   null },
        /*s32   */  {   null,   S32,    S32,    S32,    S32,    S64,    S128,   S256,   null,   null,   null,   null,   null },
        /*s64   */  {   null,   S64,    S64,    S64,    S64,    S64,    S128,   S256,   null,   null,   null,   null,   null },
        /*s128  */  {   null,   S128,   S128,   S128,   S128,   S128,   S128,   S256,   null,   null,   null,   null,   null },
        /*s256  */  {   null,   S256,   S256,   S256,   S256,   S256,   S256,   S256,   null,   null,   null,   null,   null },
        /*float */  {   null,   FLOAT,  null,   null,   null,   null,   null,   null,   FLOAT,  F16,    F32,    F64,    F128 },
        /*f16   */  {   null,   F16,    null,   null,   null,   null,   null,   null,   F16,    F16,    null,   null,   null },
        /*f32   */  {   null,   F32,    null,   null,   null,   null,   null,   null,   F32,    null,   F32,    null,   null },
        /*f64   */  {   null,   F64,    null,   null,   null,   null,   null,   null,   F64,    null,   null,   F64,    null },
        /*f128  */  {   null,   F128,   null,   null,   null,   null,   null,   null,   F128,   null,   null,   null,   F128 },
    };

    static readonly CastOp?[,] TypeCast = new CastOp?[15, 15]
    {
        //from   \  to   bool        int         s8          s16         s32         s64         s128        s256        float       f16         f32         f64         f128       @void
        /*bool  */  {   NoOp,       null,       BoolToInt,  BoolToInt,  BoolToInt,  BoolToInt,  BoolToInt,  BoolToInt,  null,       BoolToFP,   BoolToFP,   BoolToFP,   BoolToFP,   null,       null},
        /*int   */  {   null,       NoOp,       null,       null,       null,       null,       null,       null,       null,       null,       null,       null,       null,       null,       null},
        /*s8    */  {   IntToBool,  null,       NoOp,       SExt,       SExt,       SExt,       SExt,       SExt,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     null,       null},
        /*s16   */  {   IntToBool,  null,       Trunc,      NoOp,       SExt,       SExt,       SExt,       SExt,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     null,       null},
        /*s32   */  {   IntToBool,  null,       Trunc,      Trunc,      NoOp,       SExt,       SExt,       SExt,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     null,       null},
        /*s64   */  {   IntToBool,  null,       Trunc,      Trunc,      Trunc,      NoOp,       SExt,       SExt,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     IntToPtr,   IntToPtr},
        /*s128  */  {   IntToBool,  null,       Trunc,      Trunc,      Trunc,      Trunc,      NoOp,       SExt,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     null,       null},
        /*s256  */  {   IntToBool,  null,       Trunc,      Trunc,      Trunc,      Trunc,      Trunc,      NoOp,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     null,       null},
        /*float */  {   null,       null,       null,       null,       null,       null,       null,       null,       NoOp,       null,       null,       null,       null,       null,       null},
        /*f16   */  {   FPToBool,   null,       FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     null,       NoOp,       FPExt,      FPExt,      FPExt,      null,       null},
        /*f32   */  {   FPToBool,   null,       FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     null,       FPTrunc,    NoOp,       FPExt,      FPExt,      null,       null},
        /*f64   */  {   FPToBool,   null,       FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     null,       FPTrunc,    FPTrunc,    NoOp,       FPExt,      null,       null},
        /*f128  */  {   FPToBool,   null,       FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     null,       FPTrunc,    FPTrunc,    FPTrunc,    NoOp,       null,       null},
        /*@void */  {   null,       null,       null,       null,       null,       PtrToInt,   null,       null,       null,       null,       null,       null,       null,       NoOp,       NoOp},
        /*@void */  {   null,       null,       null,       null,       null,       PtrToInt,   null,       null,       null,       null,       null,       null,       null,       NoOp,       NoOp},
    };

    static TypeInfo GetBinaryOpReturnType(BinaryOperator binaryOperator, TypeInfo typeA, TypeInfo typeB)
    {
        if (binaryOperator == BinaryOperator.EqualEqual
         || binaryOperator == BinaryOperator.NotEqual)
            return Builtins.GetTypeInfo(BOOL);

        if (typeA.BuiltinType == BOOL && typeB.BuiltinType == BOOL
         && binaryOperator == BinaryOperator.AND
         || binaryOperator == BinaryOperator.OR)
            return Builtins.GetTypeInfo(BOOL);

        BuiltinType? shared = GetImplicitPromotionType(typeA, typeB)!;

        if (shared != BOOL && shared != null || shared == PTR)
            if (binaryOperator == BinaryOperator.Greater || binaryOperator == BinaryOperator.Less)
                return Builtins.GetTypeInfo(BOOL);
            else
                return Builtins.GetTypeInfo((BuiltinType)shared)    ;

        throw new Exception($"Operator '{binaryOperator}' is not valid for type {shared}");
    }

    static bool CanTypesInteropScalar(BuiltinType? typeA, BuiltinType? typeB)  // a + b; a * b;
    {
        if(typeA == null || typeB == null)
            return false;

        int indexA = Builtins.BuiltinTypeIndex(typeA);
        int indexB = Builtins.BuiltinTypeIndex(typeB);

        if (typeA == PTR
        && (typeB == S8 || typeB == S16 || typeB == S32 || typeB == S64))
            return true;

        if (typeB == PTR
        && (typeA == S8 || typeA == S16 || typeA == S32 || typeA == S64))
            return true;

        return ScalarTypeInterop[indexA, indexB];
    }

    static bool CanImplicitlyCast(TypeInfo fromType, TypeInfo toType)
    {
        if (fromType.BuiltinType == PTR && toType.BuiltinType == PTR)
            return CanImplicitlyCast(fromType.Pointee!,
                                     toType.Pointee!);

        if (fromType.BuiltinType == S64 && toType.BuiltinType == PTR)    // assigning ptr address to s64
            return true;

        int indexA = Builtins.BuiltinTypeIndex(fromType.BuiltinType);
        int indexB = Builtins.BuiltinTypeIndex(toType.BuiltinType);

        if (indexA < 0 || indexB < 0)   // composite: can only assign to same type
            return fromType == toType;

        return LosslessTypeInterop[indexA, indexB];
    }

    static BuiltinType? GetImplicitPromotionType(TypeInfo fromType, TypeInfo toType)
    {
        BuiltinType? fromBuiltin = fromType.BuiltinType;
        BuiltinType? toBuiltin   = toType.BuiltinType;
        
        int i = Builtins.BuiltinTypeIndex(fromBuiltin);
        int j = Builtins.BuiltinTypeIndex(toBuiltin);

        BuiltinType? result = null;

        if (i >= 0 && j >= 0)       // only promote built-in types
            result = ImplicitPromotion[i, j];
        else
        {
            if (fromType == toType)     // two of the same structs can still interop for comparison
                return fromBuiltin;
        }

        if (fromBuiltin == PTR
        && (toBuiltin == S8 || toBuiltin == S16 || toBuiltin == S32 || toBuiltin == S64))
            result = fromBuiltin;

        if (toBuiltin == PTR
        && (fromBuiltin == S8 || fromBuiltin == S16 || fromBuiltin == S32 || fromBuiltin == S64))
            result = toBuiltin;

        if (result == null)
            Log.Error(13, $"Cannot implicitly convert between {fromType} {fromType.BuiltinType} and {toType} {toType.BuiltinType}");

        return result;
    }

    static TypeInfo PromoteIfLiteral(TypeInfo typeInfo, string expectedType)
    {
        if (typeInfo.BuiltinType == PTR)
            return typeInfo;

        // TODO do some enum stuff instead??
        int builtinA = Builtins.BuiltinTypeIndex(typeInfo.BuiltinType);
        int builtinB = Builtins.BuiltinTypeIndex(StringToBuiltin(expectedType));

        if (builtinA != 1 && builtinA != 8)     // 1 == 'int'; 8 == 'float'
            return typeInfo;

        if (typeInfo.BuiltinType == PTR && Builtins.IsInt(StringToBuiltin(expectedType)))    // int - ,ptr
            return new TypeInfo("s64", Scalar, S64);

        if (StringToBuiltin(expectedType) == PTR && Builtins.IsInt(typeInfo.BuiltinType))
            return new TypeInfo("s64", Scalar, S64);

        if (builtinA < 0 || builtinB < 0)
            throw new Exception($"Cannot resolve {typeInfo.TypeName} to {expectedType}");

        if (LosslessTypeInterop[builtinA, builtinB])
        {
            TypeInfo type = new TypeInfo(expectedType, Scalar, StringToBuiltin(expectedType));
            type.Size = SizeOf(type.TypeName);
            return type;    // literals always cast to the more concrete value
        }

        throw new Exception($"Cannot implicitly convert {typeInfo.TypeName} to {expectedType}");
    }

    #endregion
}

public enum CastOp
{
    NoOp,
    SExt,
    Trunc,

    FPExt,
    FPTrunc,

    SIToFP,
    FPToSI,

    PtrToInt,
    IntToPtr,
    PtrToBool,

    BoolToInt,
    IntToBool,
    BoolToFP,
    FPToBool
}