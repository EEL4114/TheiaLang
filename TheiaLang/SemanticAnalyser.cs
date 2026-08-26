using static TheiaLang.CastOp;

namespace TheiaLang;
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
#if DEBUG
            if (variantInfo == null)
                throw new Exception($"Could not find union variant '{variant.Identifier}' in {currentScope.FullName}");
#endif
            if (variantInfo.Type != null)
                variantInfo.Type = UpdateTypeInfo(variantInfo.Type);
            else
                variantInfo.Type = ResolveType(variant.ResolvedType.TypeName);
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
        {
            TypeInfo ptrType = new TypeInfo($"@{structDeclaration.ResolvedType.TypeName}", 8, structDeclaration.ResolvedType);
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

            if(!currentScope.TryLookupLocal(arg.Identifier, out _, out _))
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
                    returnStatement.Expression.ResolvedType = new TypeInfo("void");
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
                    Log.Error(21, $"{identifier.Name} is not defined in {currentScope.Name}");

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
                            throw new Exception($"Unexpected argument in call 'SizeOf': expected identifer, got: {call.Arguments[0].GetType()}");

                        /*TypeInfo typeInfo;
                        if (currentScope.TryLookup(id.Name, out SymbolInfo? symbolInfo, out _))
                            typeInfo = symbolInfo!.Type;
                        else
                            throw new Exception($"Could not resolve {id.Name}");
                        */
                        // Log.Info($"{call.CalleeName} {id.Name} {typeInfo}");

                        uint sizeValue = SizeOf(i.Name);

                        expression = new LiteralExpression((int)sizeValue, sizeValue.ToString(), SourePosition.None);
                        expression.ResolvedType = new TypeInfo("s32", 4);
                    }
                    else if (id.Name == "Alloc")
                    {
                        Log.Info("ALLOC");
                    }
                    else if (IRGenerator.BuiltinTypes.Contains(id.Name)|| id.Name.StartsWith('@'))
                    {
                        for (int i = 0; i < call.Arguments.Count; i++)
                        {
                            IExpression argument = AnalyseExpression(call.Arguments[i]);
                            call.Arguments[i] = argument;
                        }
                        if (IRGenerator.BuiltinTypes.Contains(id.Name))
                        {
                            int targetTypeIndex = IRGenerator.BuiltinTypes.IndexOf(id.Name);
                            int sourceTypeIndex = IRGenerator.BuiltinTypes.IndexOf(call.Arguments[0].ResolvedType!.TypeName);

                            if (targetTypeIndex < 0 || sourceTypeIndex < 0)
                            {
                                if(targetTypeIndex == IRGenerator.BuiltinTypeIndex("s64") && call.Arguments[0].ResolvedType!.TypeName.StartsWith('@') )
                                    sourceTypeIndex = 13;
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
                                for (int i = 0; i < call.Arguments.Count; i++)
                                {
                                    IExpression argument = AnalyseExpression(call.Arguments[i]);
                                    string expected = sInfo.Parameters[i].ResolvedType!.TypeName;
                                    argument.ResolvedType = PromoteIfLiteral(argument.ResolvedType!, expected);
                                    GenerateImplicitCast(argument, sInfo.Parameters[i].ResolvedType!);

                                    call.Arguments[i] = argument;
                                    string actual = argument.ResolvedType!.TypeName;
                                    string name = sInfo.Parameters[i].Identifier;
                                    if (actual != expected)
                                        Log.Error(22, $"Function {sInfo.Name} expects type {expected} for argument {name}, got: {actual}");
                                    call.Arguments[i].ResolvedType = PromoteIfLiteral(argument.ResolvedType!,
                                                                                    sInfo.Parameters[i].ResolvedType!.TypeName);
                                    call.Arguments[i] = GenerateImplicitCast(call.Arguments[i], sInfo.Parameters[i].ResolvedType!);
                                }
                                call.ResolvedType = sInfo.Type;
                                call.Scope = defScope;
                            }
                        }
                        else
                        {
                            if (!currentScope.TryLookup(id.Name, out SymbolInfo? info, out Scope? defScope)
                                || info!.Kind != SymbolKind.Function)
                                throw new Exception($"Unknown function '{id.Name}'");

                            if (call.Arguments.Count != info.Parameters!.Count)
                                throw new Exception($"Function '{info.Name}' expects {info.Parameters.Count}"
                                                    + $" arguments, got {call.Arguments.Count}");

                            for (int i = 0; i < call.Arguments.Count; i++)
                            {
                                IExpression argument = AnalyseExpression(call.Arguments[i]);
                                call.Arguments[i] = argument;
                                string actual = argument.ResolvedType!.TypeName;

                                string expected = info.Parameters[i].ResolvedType!.TypeName;
                                call.Arguments[i].ResolvedType = PromoteIfLiteral(argument.ResolvedType!,
                                                                                info.Parameters[i].ResolvedType!.TypeName);

                                call.Arguments[i] = GenerateImplicitCast(call.Arguments[i], info.Parameters[i].ResolvedType!);
                            }

                            call.ResolvedType = info.Type;
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
                    memberAccess.Member = (IdentifierExpression)AnalyseExpression(memberAccess.Member);
                    currentScope = scope;

                    IExpression addressOfExpression = AnalyseExpression(new UnaryExpression(UnaryOperator.AddressOf, memberAccess.Target, SourePosition.None));

                    call.Arguments.Insert(0, addressOfExpression);
                    call.Target = memberAccess.Member;
                    call.ResolvedType = memberAccess.Member.ResolvedType;
                    call.Scope = defScope;
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
                        throw new Exception($"Could not find {targetEx.ResolvedType.TypeName} in {currentScope.FullName}");
                    EnterScope(targetScope!);
                }
                else if (memberAccess.Target is MemberAccessExpression mem && mem.Scope != null)
                    EnterScope(mem.Scope);  
                else
                    throw new Exception("ff");

                AnalyseExpression(memberAccess.Member);
                memberAccess.ResolvedType = memberAccess.Member.ResolvedType;
                if (!IRGenerator.BuiltinTypes.Contains(memberAccess.Member.ResolvedType!.TypeName))
                {
                    if (!currentScope.TryFindChild(memberAccess.Member.ResolvedType!.TypeName, out Scope? memberScope))
                        throw new Exception($"Could not find {memberAccess.Member.ResolvedType!.TypeName} in {currentScope.FullName}");

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
                            Log.Error(21, $"Can't take address of non-assignable {unary.Operand}");
                        unary.ResolvedType = new TypeInfo("@" + unary.Operand.ResolvedType!.TypeName,
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

                if (!CanTypesInteropScalar(indexExpression.Index.ResolvedType!.TypeName, "int"))
                    throw new Exception($"Invalid index type: '{indexExpression.Index.ResolvedType.TypeName}'");
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
            return new TypeInfo(typeOrName, 8, GetTypeInfo(typeOrName[1..]));

        if(typeOrName.StartsWith(Elaboration.DYNAMIC_ARRAY_PREFIX))
            return new TypeInfo(typeOrName, 24);

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
        else
            return type.Size;

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

    public static readonly uint[] SizeOfBuiltin =
    [
        //  bool    int     s8      s16     s32     s64     s128    s256   float    f16     f32     f64     f128    void
            1,      0,      1,      2,      4,      8,      16,     32,     0,      2,      4,      8,      16,     0
    ];

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
        string sourceType = expression.ResolvedType!.TypeName;
        string targetType = target.TypeName;

        if (!CanImplicitlyCast(sourceType, targetType))
            throw new Exception($"{expression.Pos}: Cannot implicitly convert {sourceType} -> {targetType}");

        if (sourceType != targetType)
        {
            if (IRGenerator.BuiltinTypes.Contains(sourceType)
             && IRGenerator.BuiltinTypes.Contains(targetType))
            {
                CastOp? op = TypeCast[IRGenerator.BuiltinTypeIndex(sourceType),
                                      IRGenerator.BuiltinTypeIndex(targetType)];
                if (op == null)
                    throw new Exception($"Invalid cast: {sourceType} -> {targetType}");
                expression = new CastExpression((CastOp)op, expression, SourePosition.None);
                expression.ResolvedType = target;
            }
        }
        return expression;
    }

    #endregion

    #region Interop
    
    static readonly bool[,] LosslessTypeInterop = new bool[14, 14]
    {
        //from  \  to   bool    int     s8      s16     s32     s64     s128    s256    float   f16     f32     f64     f128    void
        /*bool  */  {   true,   false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false},
        /*int   */  {   false,  true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true,   true ,  false},
        /*s8    */  {   false,  true,   true,   true,   true,   true,   true,   true,   false,  false,  false,  false,  false,  false},
        /*s16   */  {   false,  true,   false,  true,   true,   true,   true,   true,   false,  false,  false,  false,  false,  false},
        /*s32   */  {   false,  true,   false,  false,  true,   true,   true,   true,   false,  false,  false,  false,  false,  false},
        /*s64   */  {   false,  true,   false,  false,  false,  true,   true,   true,   false,  false,  false,  false,  false,  false},
        /*s128  */  {   false,  true,   false,  false,  false,  false,  true,   true,   false,  false,  false,  false,  false,  false},
        /*s256  */  {   false,  true,   false,  false,  false,  false,  false,  true,   false,  false,  false,  false,  false,  false},
        /*float */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   true,   true,   true,   true,   false},
        /*f16   */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   true,   false,  false,  false,  false},
        /*f32   */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   false,  true,   false,  false,  false},
        /*f64   */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   false,  false,  true,   false,  false},
        /*f128  */  {   false,  false,  false,  false,  false,  false,  false,  false,  true,   false,  false,  false,  true,   false},
        /*void  */  {   false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  false,  true },
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

    static readonly CastOp?[,] TypeCast = new CastOp?[14, 14]
    {
        //from   \  to   bool        int         s8          s16         s32         s64         s128        s256        float       f16         f32         f64         f128       @void
        /*bool  */  {   NoOp,       null,       BoolToInt,  BoolToInt,  BoolToInt,  BoolToInt,  BoolToInt,  BoolToInt,  null,       BoolToFP,   BoolToFP,   BoolToFP,   BoolToFP,   null},
        /*int   */  {   null,       NoOp,       null,       null,       null,       null,       null,       null,       null,       null,       null,       null,       null,       null},
        /*s8    */  {   IntToBool,  null,       NoOp,       SExt,       SExt,       SExt,       SExt,       SExt,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     null},
        /*s16   */  {   IntToBool,  null,       Trunc,      NoOp,       SExt,       SExt,       SExt,       SExt,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     null},
        /*s32   */  {   IntToBool,  null,       Trunc,      Trunc,      NoOp,       SExt,       SExt,       SExt,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     null},
        /*s64   */  {   IntToBool,  null,       Trunc,      Trunc,      Trunc,      NoOp,       SExt,       SExt,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     IntToPtr},
        /*s128  */  {   IntToBool,  null,       Trunc,      Trunc,      Trunc,      Trunc,      NoOp,       SExt,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     null},
        /*s256  */  {   IntToBool,  null,       Trunc,      Trunc,      Trunc,      Trunc,      Trunc,      NoOp,       null,       SIToFP,     SIToFP,     SIToFP,     SIToFP,     null},
        /*float */  {   null,       null,       null,       null,       null,       null,       null,       null,       NoOp,       null,       null,       null,       null,       null},
        /*f16   */  {   FPToBool,   null,       FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     null,       NoOp,       FPExt,      FPExt,      FPExt,      null},
        /*f32   */  {   FPToBool,   null,       FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     null,       FPTrunc,    NoOp,       FPExt,      FPExt,      null},
        /*f64   */  {   FPToBool,   null,       FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     null,       FPTrunc,    FPTrunc,    NoOp,       FPExt,      null},
        /*f128  */  {   FPToBool,   null,       FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     FPToSI,     null,       FPTrunc,    FPTrunc,    FPTrunc,    NoOp,       null},
        /*@void */  {   null,       null,       null,       null,       null,       PtrToInt,   null,       null,       null,       null,       null,       null,       null,       NoOp},
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

        if (shared != "bool" && IRGenerator.IsBuiltinType(shared) || shared.StartsWith('@'))
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

        if (typeA.StartsWith('@')
        && (typeB == "s8" || typeB == "s16" || typeB == "s32" || typeB == "s64"))
            return true;

        if (typeB.StartsWith('@')
        && (typeA == "s8" || typeA == "s16" || typeA == "s32" || typeA == "s64"))
            return true;

        if (indexA < 0 || indexB < 0)   // scalar math only allowed for built in types
            return false;

        return ScalarTypeInterop[indexA, indexB];
    }

    static bool CanImplicitlyCast(string fromType, string toType)
    {
        if (fromType.StartsWith('@') && toType.StartsWith('@'))
        {
            return CanImplicitlyCast(fromType.TrimStart('@'),
                                     toType.TrimStart('@'));
        }

        if (fromType == "s64" && toType.StartsWith('@'))    // assigning ptr address to s64
            return true;

        int indexA = IRGenerator.BuiltinTypeIndex(fromType);
        int indexB = IRGenerator.BuiltinTypeIndex(toType);

        if (indexA < 0 || indexB < 0)   // composite: can only assign to same type
            return fromType == toType;

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

        if (typeA.StartsWith('@')
        && (typeB == "s8" || typeB == "s16" || typeB == "s32" || typeB == "s64"))
            result = typeA;

        if (typeB.StartsWith('@')
        && (typeA == "s8" || typeA == "s16" || typeA == "s32" || typeA == "s64"))
            result = typeB;

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

        // TODO do some enum stuff instead??
        int builtinA = IRGenerator.BuiltinTypeIndex(typeInfo.TypeName);
        int builtinB = IRGenerator.BuiltinTypeIndex(expectedType);

        if (builtinA != 1 && builtinA != 8)     // 1 == 'int'; 8 == 'float'
            return typeInfo;

        if (typeInfo.TypeName.StartsWith('@') && builtinB < 8)    // int - ptr
            return new TypeInfo("s64");

        if (expectedType.StartsWith('@') && builtinA < 8)
            return new TypeInfo("s64");

        if (builtinA < 0 || builtinB < 0)
            throw new Exception($"Cannot resolve {typeInfo.TypeName} to {expectedType}");

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