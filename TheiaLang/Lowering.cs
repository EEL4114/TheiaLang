namespace TheiaLang;

using static CastOp;
using static TypeKind;
using static BuiltinType;

public class Lowering
{
    long tmpvar = 0;
    long tmpscope = 0;

    const int REPEAT_LOOP_THRESHOLD = 10;

    public Scope currentScope;
    public Scope globalScope;
    ProgramNode program;

    ModuleInitialiser moduleInitialiser;

    public void Lower(ProgramNode p, Scope globalScope)
    {   
        program = p;
        tmpvar = 0;
        currentScope = globalScope;
        this.globalScope = globalScope;
        moduleInitialiser = null!;

        for(int i = 0; i < p.Nodes.Count; i++)
        {
            INode node = p.Nodes[i];
            if(node is FunctionDeclaration fn)
                LowerFunction(fn);
            if(node is StructDeclaration sd)
                LowerStruct(sd);
            if(node is UnionDeclaration ud)
                LowerUnion(ud);
            if(node is VariableDeclaration vd)
                LowerGlobalVariableDeclaration(vd);
        }
    }

    #region  Top-Level

    public void LowerStruct(StructDeclaration sd)
    {
        Scope tmp = currentScope;
        currentScope = sd.Scope!;
        foreach(TypeNamePair typeNamePair in sd.Fields)
            LowerTypeNamePair(typeNamePair);
        
        foreach (FunctionDeclaration function in sd.Functions)
            LowerFunction(function);
        currentScope = tmp;
    }

    public void LowerUnion(UnionDeclaration ud)
    {
        Scope tmp = currentScope;
        currentScope = ud.Scope!;
        foreach(TypeNamePair typeNamePair in ud.Variants)
            LowerTypeNamePair(typeNamePair);        
        currentScope = tmp;
    }

    public void LowerTypeNamePair(TypeNamePair typeNamePair)
    {
        return; //for now we don't need to do anything
    }

    public void LowerFunction(FunctionDeclaration fn)
    {
        Scope tmp = currentScope;
        currentScope = fn.Scope!;
        foreach (TypeNamePair typeNamePair in fn.Parameters)
            LowerTypeNamePair(typeNamePair);
        LowerBlock(fn.Statements);
        currentScope = tmp;
    }

    void LowerBlock(List<IStatement> block)
    {
        List<IStatement> newStatements = [];
        foreach (IStatement statement in block)
            newStatements.AddRange(LowerStatement(statement));

        block.Clear();
        block.AddRange(newStatements);
    }

    void LowerGlobalVariableDeclaration(VariableDeclaration vd)
    {
        ModuleInitialiser moduleInitialiser = GetModuleInitialiser();

        Scope tmp = currentScope;
        currentScope = moduleInitialiser.Scope;
        if(vd.Init != null)
        {
            (IExpression expression, List<IStatement> prelude) = LowerExpression(vd.Init!);
            moduleInitialiser.Statements.AddRange(prelude);
            moduleInitialiser.Statements.Add(new AssignmentStatement(NewIdentifierExpression(vd, currentScope), expression));
        }

        currentScope = tmp;
        vd.Init = NewNoInit();
        return;
    }

    #endregion

    #region Statements

    List<IStatement> LowerStatement(IStatement statement)
        => statement switch
        {
            VariableDeclaration         vd                => LowerVariableDeclaration(vd),
            AssignmentStatement         assignment        => LowerAssignmentStatement(assignment),
            IfStatement                 ifStatement       => LowerIfStatement(ifStatement),
            ForStatement                forStatement      => LowerForStatement(forStatement),
            ReturnStatement             returnStatement   => LowerReturnStatement(returnStatement),
            BreakStatement              breakStatement    => [breakStatement],
            ContinueStatement           continueStatement => [continueStatement],
            ExpressionStatement         expression        => LowerExpressionStatement(expression),
            CompoundAssignmentStatement compound          => LowerCompoundAssignmentStatement(compound),
        };

    List<IStatement> LowerVariableDeclaration(VariableDeclaration vd)
    {
        if(vd.Init == null)
            return [vd];

        List<IStatement> statements = [];
        (IExpression init, List<IStatement> prelude) = LowerExpression(vd.Init);
        vd.Init = init;
        statements.AddRange(prelude);
        
        statements.Add(vd);
        
        return statements;
    }

    List<IStatement> LowerAssignmentStatement(AssignmentStatement assignment)
    {
        List<IStatement> statements = [];
        (IExpression target, List<IStatement> prelude) = LowerExpression(assignment.Target);
        statements.AddRange(prelude);
        assignment.Target = target;
        (IExpression expression, List<IStatement> prelude2) = LowerExpression(assignment.Expression);
        statements.AddRange(prelude2);
        assignment.Expression = expression;
        
        statements.Add(assignment);
        
        return statements;
    }

    List<IStatement> LowerIfStatement(IfStatement ifStatement)
    {
        Scope tmp = currentScope;
        (IExpression newCondition, List<IStatement> prelude) = LowerExpression(ifStatement.Condition);
        
        ifStatement.Condition = newCondition;
        currentScope = ifStatement.ThenScope;
        LowerBlock(ifStatement.ThenBranch);
        if(ifStatement.ElseBranch != null)
        {
            currentScope = ifStatement.ElseScope!;  
            LowerBlock(ifStatement.ElseBranch);
        }
        currentScope = tmp;
        prelude.Add(ifStatement);
        return prelude;
    }

    List<IStatement> LowerForStatement(ForStatement forStatement)
    {
        Scope tmp = currentScope;
        currentScope = forStatement.HeadScope;

        if(forStatement.Initialiser != null)
        {  
            List<IStatement> newInitialiser = [];

            foreach (IStatement statement in forStatement.Initialiser)
                newInitialiser.AddRange(LowerStatement(statement));

            forStatement.Initialiser = newInitialiser;
        }

        if(forStatement.Condition != null)
        {
            (IExpression condExpr,  List<IStatement> condPrelude) = LowerExpression(forStatement.Condition);
            forStatement.Condition = condExpr;
            forStatement.ConditionPrelude = condPrelude;
        }

        if(forStatement.Iterator != null)
        {
            List<IStatement> newIterator = [];

            foreach (IStatement statement in forStatement.Iterator)
                newIterator.AddRange(LowerStatement(statement));

            forStatement.Iterator = newIterator;
        }
        currentScope = forStatement.BodyScope;
        List<IStatement> newBody = [];
        foreach(IStatement statement in forStatement.Body)
            newBody.AddRange(LowerStatement(statement));

        forStatement.Body = newBody;

        currentScope = tmp;
        return [forStatement]; 
    }

    List<IStatement> LowerReturnStatement(ReturnStatement r)
    {
        if (r.Expression == null)
            return [r];
        
        (IExpression expression, List<IStatement> prelude) = LowerExpression(r.Expression);
        r.Expression = expression;

        prelude.Add(r);
        return prelude;
    }

    List<IStatement> LowerExpressionStatement(ExpressionStatement ex)
    {
        (IExpression expression, List<IStatement> prelude) = LowerExpression(ex.Expression);
        ex.Expression = expression;

        prelude.Add(ex);
        return prelude;
    }

    List<IStatement> LowerCompoundAssignmentStatement(CompoundAssignmentStatement compound)
    {
        List<IStatement> statements = [];
        (IExpression target, List<IStatement> prelude) = LowerExpression(compound.Target);
        statements.AddRange(prelude);
        compound.Target = target;

        (IExpression expression, List<IStatement> prelude2) = LowerExpression(compound.Expression);
        statements.AddRange(prelude2);
        compound.Expression = expression;

        statements.Add(compound);

        return statements;
    }

    #endregion

    #region Expressions

    (IExpression, List<IStatement> prelude) LowerExpression(IExpression expression)
    {
        return expression switch
        {
            LiteralExpression              => (expression, []),
            IdentifierExpression           => (expression, []),
            BinaryExpression        binary => LowerBinaryExpression(binary),
            UnaryExpression         unary  => LowerUnaryExpression(unary),
            MemberAccessExpression  mem    => LowerMemberAccessExpression(mem),
            InstantiationExpression inst   => LowerInstantiationExpression(inst),
            IndexExpression         index  => LowerIndexExpression(index),
            CastExpression          cast   => LowerCastExpression(cast),
            CallExpression          call   => LowerCallExpression(call),
            RepeatExpression        repeat => LowerRepeatExpression(repeat),
        };
    }

    (IExpression, List<IStatement> prelude) LowerBinaryExpression(BinaryExpression binary)
    {
        List<IStatement> prelude = [];
        (IExpression left, List<IStatement> leftPrelude) = LowerExpression(binary.Left);
        prelude.AddRange(leftPrelude);

        // AND and OR short-circuit
        if(binary.Op != BinaryOperator.AND && binary.Op != BinaryOperator.OR)
        {
            (IExpression r, List<IStatement> rPrelude) = LowerExpression(binary.Right);
            prelude.AddRange(rPrelude);
            binary.Left = left;
            binary.Right = r;
            return (binary, prelude);
        }

        Scope tmp = currentScope;
        Scope thenScope = new Scope($"if_then_lw_{tmpscope++}", true, currentScope);
        
        currentScope = thenScope;
        (IExpression right, List<IStatement> rightPrelude) = LowerExpression(binary.Right);
        currentScope = tmp;
        
        VariableDeclaration tempVar = NewTempVar(Builtins.GetTypeInfo(BOOL), init: left);
        prelude.Add(tempVar);

        AssignmentStatement tmpAssignment = new AssignmentStatement(NewIdentifierExpression(tempVar, currentScope), right);
        
        List<IStatement> thenStatements = [.. rightPrelude, tmpAssignment];

        if(binary.Op == BinaryOperator.AND)
        {
            IfStatement ifStatement = new IfStatement(Condition: NewIdentifierExpression(tempVar, currentScope), thenBranch: thenStatements, null, thenScope, null);
            prelude.Add(ifStatement);
        }
        else if(binary.Op == BinaryOperator.OR)
        {
            UnaryExpression notTmp = new UnaryExpression(UnaryOperator.Invert, NewIdentifierExpression(tempVar, currentScope));
            notTmp.ResolvedType = Builtins.GetTypeInfo(BOOL);
            IfStatement ifStatement = new IfStatement(Condition: notTmp, thenBranch: thenStatements, null, thenScope, null);
            prelude.Add(ifStatement);
        }

        return (NewIdentifierExpression(tempVar, currentScope), prelude);
    }

    (IExpression, List<IStatement> prelude) LowerUnaryExpression(UnaryExpression unary)
    {
        (IExpression operand, List<IStatement> prelude) =
            LowerExpression(unary.Operand);

        unary.Operand = operand;

        return (unary, prelude);
    }

    (IExpression, List<IStatement> prelude) LowerMemberAccessExpression(MemberAccessExpression memberAccess)
    {
        (IExpression target, List<IStatement> prelude) =
            LowerExpression(memberAccess.Target);

        memberAccess.Target = target;

        return (memberAccess, prelude);
    }

    (IExpression, List<IStatement> prelude) LowerInstantiationExpression(InstantiationExpression instantiation)
    {
        List<IStatement> prelude = [];

        for (int i = 0; i < instantiation.Arguments.Count; i++)
        {
            (IExpression arg, List<IStatement> argPrelude) = LowerExpression(instantiation.Arguments[i]);

            prelude.AddRange(argPrelude);
            instantiation.Arguments[i] = arg;
        }

        return (instantiation, prelude);
    }

    (IExpression, List<IStatement> prelude) LowerIndexExpression(IndexExpression index)
    {
        List<IStatement> prelude = [];

        (IExpression target, List<IStatement> targetPrelude) =
            LowerExpression(index.Target);

        prelude.AddRange(targetPrelude);
        index.Target = target;

        (IExpression idx, List<IStatement> indexPrelude) =
            LowerExpression(index.Index);

        prelude.AddRange(indexPrelude);
        index.Index = idx;

        return (index, prelude);
    }

    (IExpression, List<IStatement> prelude) LowerCastExpression(CastExpression cast)
    {
        (IExpression target, List<IStatement> prelude) =
            LowerExpression(cast.Target);

        cast.Target = target;

        return (cast, prelude);
    }

    (IExpression, List<IStatement> prelude) LowerCallExpression(CallExpression call)
    {
        List<IStatement> prelude = [];
        (IExpression target, List<IStatement> targetPrelude) = LowerExpression(call.Target);
        prelude.AddRange(targetPrelude);
        call.Target = target;

        for (int i = 0; i < call.Arguments.Count; i++)
        {
            (IExpression arg, List<IStatement> argPrelude) = LowerExpression(call.Arguments[i]);
            prelude.AddRange(argPrelude);
            call.Arguments[i] = arg;
        }

        return (call, prelude);
    }


    (IExpression, List<IStatement> prelude) LowerRepeatExpression(RepeatExpression repeat)
    {
        List<IStatement> prelude = [];
        (IExpression element, List<IStatement> elementPrelude) = LowerExpression(repeat.Expression);
        prelude.AddRange(elementPrelude);
        VariableDeclaration tmpElement = NewTempVar(element.ResolvedType!, element);
        VariableDeclaration tmpArray = NewTempVar(repeat.ResolvedType!, NewNoInit());

        prelude.Add(tmpElement);
        prelude.Add(tmpArray);

        if(repeat.Count < REPEAT_LOOP_THRESHOLD)
        {
            for(int i = 0; i < repeat.Count; i++)
            {
                prelude.Add(new AssignmentStatement(
                                NewIndexExpression(tmpArray, currentScope, i), 
                                NewIdentifierExpression(tmpElement, currentScope)
                ));
            }
        }
        else //loop
        {   
            Scope tmp = currentScope;
            Scope headScope = new Scope($"__loop_head_l_{tmpscope++}", parent: tmp);
            currentScope = headScope;
            VariableDeclaration iteratorDecl = NewTempVar(Builtins.GetTypeInfo(S64), init: NewZeroInit());
            

            LiteralExpression bound = new LiteralExpression((long)repeat.Count, repeat.Count.ToString(), LiteralKind.Lit_Int) {ResolvedType = Builtins.GetTypeInfo(S64)};
            BinaryExpression condition = new BinaryExpression(
                                            NewIdentifierExpression(iteratorDecl, headScope), 
                                            BinaryOperator.Less, 
                                            bound) 
                                            { ResolvedType = Builtins.GetTypeInfo(BOOL) };

            CompoundAssignmentStatement it = new CompoundAssignmentStatement(
                                                NewIdentifierExpression(iteratorDecl, headScope), 
                                                new LiteralExpression(1L, "1", LiteralKind.Lit_Int) {ResolvedType = Builtins.GetTypeInfo(S64)},
                                                BinaryOperator.PlusEqual);

            Scope bodyScope = new Scope($"__loop_body_l_{tmpscope++}", canShadowParent: false, parent: headScope);
            currentScope = bodyScope;

            AssignmentStatement assignment = new AssignmentStatement(
                    NewIndexExpression(
                        tmpArray,
                        tmp,
                        NewIdentifierExpression(iteratorDecl, headScope)),
                    NewIdentifierExpression(tmpElement, tmp));
            
            prelude.Add(new ForStatement([iteratorDecl], condition, [it], [assignment], headScope, bodyScope));
            currentScope = tmp;
        }

        return (NewIdentifierExpression(tmpArray, currentScope), prelude);
    }

    #endregion

    #region Helpers

    ModuleInitialiser GetModuleInitialiser()
    {
        if (moduleInitialiser != null)
            return moduleInitialiser;

        Scope scope =
            new Scope("__module_init", parent: globalScope);

        moduleInitialiser =
            new ModuleInitialiser([], scope);

        program.Nodes.Add(moduleInitialiser);

        return moduleInitialiser;
    }

    LiteralExpression NewNoInit()
    {
        return new LiteralExpression(null!, "", LiteralKind.Lit_NoInit);
    }

    LiteralExpression NewZeroInit()
    {
        return new LiteralExpression(null!, "", LiteralKind.Lit_ZeroInit);
    }

    IndexExpression NewIndexExpression(
        VariableDeclaration target,
        Scope targetScope,
        IExpression index)
    {
        return new IndexExpression(
            NewIdentifierExpression(target, targetScope),
            index)
        {
            ResolvedType = target.ResolvedType!.ElementType
        };
    }

    IndexExpression NewIndexExpression(VariableDeclaration target, Scope targetScope, long index)
    {
        IdentifierExpression id = NewIdentifierExpression(target, targetScope);
        LiteralExpression indexValue = new LiteralExpression(index, index.ToString(), LiteralKind.Lit_Int) {ResolvedType = Builtins.GetTypeInfo(S64) };

        IndexExpression indexExpr = new IndexExpression(id, indexValue);
        indexExpr.ResolvedType = target.ResolvedType!.ElementType;
        return indexExpr;
    }

    VariableDeclaration NewTempVar(TypeInfo typeInfo, IExpression? init = null)
    {
        VariableDeclaration vd = new VariableDeclaration(typeInfo.TypeName, $"__tmp_{tmpvar++}", init) { ResolvedType = typeInfo };

        currentScope.Declare(new SymbolInfo(
                             vd.Name,
                             typeInfo,
                             SymbolKind.Variable,
                             null));
        
        return vd;
    }

    IdentifierExpression NewIdentifierExpression(VariableDeclaration variableDeclaration, Scope scope)
    {
        return new IdentifierExpression(variableDeclaration.Name, Scope: scope) {ResolvedType = variableDeclaration.ResolvedType};
    }

    IdentifierExpression NewIdentifierExpression(string name, TypeInfo typeInfo, Scope scope)
    {
        return new IdentifierExpression(name, Scope: scope) { ResolvedType = typeInfo};        
    }

    #endregion
}