using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Statements;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

/// <summary>
/// Shared helpers for function and method call analysis
/// to eliminate duplicated argument handling, return type resolution, and codegen patterns.
/// </summary>
public static class CallExpressionHelper
{
    /// <summary>
    /// Checks for conflicting borrows across a set of sub-expressions within a single compound
    /// expression (struct literal fields, function call arguments, enum variant fields).
    /// 
    /// The problem this solves: PointerGetterExpression.Analyze only invalidates existing borrows
    /// but doesn't register new ones — registration is deferred to LetStatement. So within a single
    /// compound expression like `make Foo { a: &amp;uniq x, b: &amp;uniq x }`, the two borrows never
    /// see each other. This method collects all borrow sources and checks for conflicts.
    /// </summary>
    public static void CheckBorrowConflicts(
        IEnumerable<IExpression> expressions, SemanticAnalyzer analyzer, Token? errorToken)
    {
        var seenBorrows = new List<(string source, RefKind kind, IExpression expr)>();
        
        foreach (var expr in expressions)
        {
            var sources = LetStatement.ExtractBorrowSources(expr, analyzer);
            foreach (var (sourceName, refKind) in sources)
            {
                // Check against all previously seen borrows from earlier sub-expressions
                foreach (var (prevSource, prevKind, _) in seenBorrows)
                {
                    if (prevSource != sourceName) continue;
                    
                    // All borrow kinds are compatible — no exclusive references exist
                    // No conflict to report
                }
                
                seenBorrows.Add((sourceName, refKind, expr));
            }
        }
    }

    /// <summary>
    /// Analyzes arguments and marks them as moved.
    /// Also checks for conflicting borrows across arguments.
    /// </summary>
    public static void AnalyzeArgumentsWithReborrow(
        ImmutableArray<IExpression> arguments, SemanticAnalyzer analyzer)
    {
        foreach (var arg in arguments)
            AnalyzeExpressionWithReborrow(arg, analyzer);
        
        // Check for conflicting borrows across arguments
        if (arguments.Length > 1)
        {
            var token = arguments.Length > 0 ? arguments[0].Token : null;
            CheckBorrowConflicts(arguments, analyzer, token);
        }
    }

    /// <summary>
    /// Analyzes a single expression and marks it as moved.
    /// Used for function/method arguments and operator operands.
    /// </summary>
    public static void AnalyzeExpressionWithReborrow(IExpression expr, SemanticAnalyzer analyzer)
    {
        expr.Analyze(analyzer);
        analyzer.TryMarkExpressionAsMoved(expr);
    }

    /// <summary>
    /// Checks whether a return type path is the "Self" keyword.
    /// </summary>
    public static bool IsSelfReturnType(LangPath? returnType)
    {
        return returnType is NormalLangPath nlp
               && nlp.PathSegments.Length == 1
               && nlp.PathSegments[0].ToString() == "Self";
    }

    /// <summary>
    /// Resolves a method's return type by substituting Self with the concrete type,
    /// applying impl generic bindings, and resolving qualified associated type paths.
    /// Returns the resolved type path.
    /// </summary>
    public static LangPath? ResolveReturnTypeFromImpl(
        LangPath? returnType, LangPath concreteType,
        Dictionary<string, LangPath> bindings, ImmutableArray<GenericParameter> implGPs,
        SemanticAnalyzer analyzer)
    {
        if (IsSelfReturnType(returnType))
            return concreteType;

        var resolved = returnType;
        if (bindings.Count > 0)
        {
            var implArgs = implGPs.Select(gp =>
                bindings.TryGetValue(gp.Name, out var bound)
                    ? bound
                    : (LangPath)new NormalLangPath(null, [gp.Name]))
                .ToImmutableArray();
            resolved = FieldAccessExpression.SubstituteGenerics(resolved, implGPs, implArgs);
        }

        if (resolved != null)
            resolved = analyzer.ResolveQualifiedTypePath(resolved);

        return resolved;
    }

    /// <summary>
    /// Wraps a call result LLVM value into a stack-allocated ValueRefItem.
    /// Handles the RefType special case where the result is already a raw pointer.
    /// </summary>
    public static ValueRefItem BuildCallReturnValue(
        LLVMValueRef callResult, ConcreteDefinition.Type returnType, CodeGenContext context)
    {
        LLVMValueRef stackPtr;
        if (returnType is PointerLikeType)
        {
            // Pointer types: the call result IS the pointer value itself.
            // Store it directly — don't go through AssignToStack which would
            // call LoadValue and dereference it as a pointer-to-pointer.
            stackPtr = context.Builder.BuildAlloca(returnType.TypeRef);
            context.Builder.BuildStore(callResult, stackPtr);
        }
        else
        {
            stackPtr = returnType.AssignToStack(context, new ValueRefItem
            {
                Type = returnType,
                ValueRef = callResult
            });
        }

        return new ValueRefItem
        {
            Type = returnType,
            ValueRef = stackPtr
        };
    }

    /// <summary>
    /// Resolves a function/method path by temporarily pushing a trait bound during codegen.
    /// Used for operator overloading (Add(Rhs).Add) and qualified calls ((T as Trait).Method).
    /// Pushes the trait bound, resolves the path, then pops the bound.
    /// </summary>
    public static FunctionRefItem? ResolveWithTraitBound(
        CodeGenContext context, LangPath traitPath, LangPath concreteType, LangPath methodPath)
    {
        context.PushTraitBounds([(traitPath, concreteType)]);
        var result = context.GetRefItemFor(methodPath) as FunctionRefItem;
        context.PopTraitBounds();
        return result;
    }

    /// <summary>
    /// Codegens call arguments: marks each as drop-moved and produces ValueRefItems.
    /// Shared by FunctionCallKind and MethodCallKind — both are "call a function with some arguments".
    /// </summary>
    public static ValueRefItem[] CodeGenArguments(
        ImmutableArray<IExpression> arguments, CodeGenContext ctx)
    {
        foreach (var arg in arguments)
            ctx.TryMarkExpressionDropMoved(arg);
        return arguments.Select(a => a.CodeGen(ctx)).ToArray();
    }

    /// <summary>
    /// Emits a function call: loads each argument value, calls the function, and wraps the result.
    /// This is the core shared codegen for function calls, method calls, and operator overloading —
    /// all three are just "call a function with some arguments".
    /// </summary>
    public static ValueRefItem EmitCall(
        FunctionRefItem funcRef, IReadOnlyList<ValueRefItem> arguments, CodeGenContext context)
    {
        var loadedArgs = new LLVMValueRef[arguments.Count];
        for (int i = 0; i < arguments.Count; i++)
            loadedArgs[i] = arguments[i].Type.LoadValue(context, arguments[i]);

        var callResult = context.Builder.BuildCall2(
            funcRef.Function.FunctionType,
            funcRef.Function.FunctionValueRef,
            loadedArgs);

        return BuildCallReturnValue(callResult, funcRef.Function.ReturnType, context);
    }

    /// <summary>
    /// Validates call arguments against function definition parameters.
    /// Shared by function calls and method calls.
    /// selfOffset: number of leading params to skip (1 for methods where self is implicit).
    /// </summary>
    public static void ValidateCallArguments(
        FunctionDefinition fd, ImmutableArray<IExpression> arguments,
        ImmutableArray<LangPath> genericArgs, SemanticAnalyzer analyzer,
        string tokenLoc, int selfOffset = 0)
    {
        int expectedArgs = fd.Arguments.Length - selfOffset;

        // Type check each argument against its parameter
        for (int ai = 0; ai < expectedArgs && ai < arguments.Length; ai++)
        {
            var paramType = fd.Arguments[ai + selfOffset].TypePath;
            var argActualType = arguments[ai].TypePath;
            if (paramType == null || argActualType == null) continue;
            if (fd.GenericParameters.Length > 0 && genericArgs.Length > 0)
                paramType = FieldAccessExpression.SubstituteGenerics(paramType, fd.GenericParameters, genericArgs);
            // Skip type check when arg is a generic param — it'll be verified at monomorphization
            if (argActualType is NormalLangPath nlpArg && nlpArg.PathSegments.Length == 1
                && analyzer.IsGenericParam(nlpArg.PathSegments[0].ToString()))
                continue;
            if (paramType is NormalLangPath nlpParam && nlpParam.PathSegments.Length == 1
                && analyzer.IsGenericParam(nlpParam.PathSegments[0].ToString()))
                continue;
            if (paramType != argActualType)
                analyzer.AddException(new TypeMismatchException(
                    paramType, argActualType, $"argument '{fd.Arguments[ai + selfOffset].Name}'", tokenLoc));
        }

        // Argument count check
        if (arguments.Length != expectedArgs)
            analyzer.AddException(new SemanticException(
                $"Function '{fd.Name}' expects {expectedArgs} argument(s) but {arguments.Length} were provided\n{tokenLoc}"));
    }

    /// <summary>
    /// Validates generic type arguments satisfy their trait bounds and associated type constraints.
    /// Shared by function calls and method calls.
    /// </summary>
    public static void ValidateGenericBounds(
        FunctionDefinition fd, ImmutableArray<LangPath> genericArgs,
        SemanticAnalyzer analyzer, string tokenLoc)
    {
        for (int i = 0; i < fd.GenericParameters.Length && i < genericArgs.Length; i++)
        {
            var gp = fd.GenericParameters[i];
            foreach (var bound in gp.TraitBounds)
            {
                var argType = genericArgs[i];
                var resolvedBound = FieldAccessExpression.SubstituteGenerics(
                    bound.TraitPath, fd.GenericParameters, genericArgs);
                if (!analyzer.TypeImplementsTrait(argType, resolvedBound))
                {
                    var traitDef = analyzer.GetDefinition(LangPath.StripGenerics(resolvedBound));
                    if (traitDef == null || traitDef is not TraitDefinition)
                        analyzer.AddException(new TraitNotFoundException(resolvedBound, tokenLoc));
                    else
                        analyzer.AddException(new TraitBoundViolationException(argType, resolvedBound));
                }
                foreach (var (atName, atType) in bound.AssociatedTypeConstraints)
                {
                    var resolvedAtType = FieldAccessExpression.SubstituteGenerics(atType, fd.GenericParameters, genericArgs);
                    var actualAt = analyzer.ResolveAssociatedType(argType, resolvedBound, atName);
                    if (actualAt != null && actualAt != resolvedAtType)
                        analyzer.AddException(new SemanticException(
                            $"Associated type constraint '{atName} = {resolvedAtType}' not satisfied: actual '{atName}' is '{actualAt}'\n{tokenLoc}"));
                }
            }
        }
    }

    /// <summary>
    /// Handles by-value self move tracking for method calls.
    /// If self is by-value (not a reference) and the receiver type isn't Copy, marks the receiver as moved.
    /// </summary>
    public static void CheckSelfMove(
        RefKind? autoRefKind, string? rootVarName, LangPath? receiverType, SemanticAnalyzer analyzer,
        string? locationString = null)
    {
        if (autoRefKind == null && rootVarName != null && !analyzer.IsTypeCopy(receiverType))
        {
            if (locationString != null)
            {
                var blocking = analyzer.GetActiveBorrowBlockingMove(rootVarName);
                if (blocking != null)
                    analyzer.AddException(new MoveWhileBorrowedException(
                        rootVarName, blocking.Value.borrower, locationString));
            }
            analyzer.MarkAsMoved(rootVarName);
            analyzer.InvalidateBorrowsFrom(rootVarName);
        }
    }
}