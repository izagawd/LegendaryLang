using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
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
                    
                    // &uniq conflicts with everything (including another &uniq)
                    if (refKind == RefKind.Uniq || prevKind == RefKind.Uniq)
                    {
                        var tokenLoc = errorToken?.GetLocationStringRepresentation() ?? "";
                        analyzer.AddException(new BorrowConflictException(
                            sourceName, "(earlier field/argument)", prevKind, refKind, tokenLoc));
                    }
                    // &const and &mut conflict with each other
                    else if ((refKind == RefKind.Const && prevKind == RefKind.Mut) ||
                             (refKind == RefKind.Mut && prevKind == RefKind.Const))
                    {
                        var tokenLoc = errorToken?.GetLocationStringRepresentation() ?? "";
                        analyzer.AddException(new BorrowConflictException(
                            sourceName, "(earlier field/argument)", prevKind, refKind, tokenLoc));
                    }
                }
                
                seenBorrows.Add((sourceName, refKind, expr));
            }
        }
    }

    /// <summary>
    /// Analyzes arguments and marks non-&amp;uniq ones as moved.
    /// &amp;uniq arguments get automatic reborrowing (like Rust's &amp;mut auto-reborrow).
    /// Also checks for conflicting borrows across arguments (e.g., foo(&amp;uniq x, &amp;uniq x)).
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
    /// Analyzes a single expression and marks it as moved unless it's &amp;uniq (auto-reborrow).
    /// Used for function/method arguments and operator operands.
    /// </summary>
    public static void AnalyzeExpressionWithReborrow(IExpression expr, SemanticAnalyzer analyzer)
    {
        expr.Analyze(analyzer);
        if (!RefTypeDefinition.IsUniqRefType(expr.TypePath))
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
}
