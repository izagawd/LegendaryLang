using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Statements;

public class LetConflictingTypesException : SemanticException
{
    public LetConflictingTypesException(LetStatement statement)
    {
        Statement = statement;
    }

    public LetStatement Statement { get; }

    public override string Message =>
        $"Conflicting types:\nThe declared type '{Statement.VariableDefinition.TypePath}' and assigned expression with type" +
        $" '{Statement.EqualsTo.TypePath}' do not have matching types\n{Statement.LetToken?.GetLocationStringRepresentation()}";
}

public class LetStatement : IStatement
{
    public LetStatement(LetToken letToken, VariableDefinition variableDefinition, IExpression? equalsTo)
    {
        LetToken = letToken;
        EqualsTo = equalsTo;
        VariableDefinition = variableDefinition;
        if (EqualsTo is null && VariableDefinition.TypePath is null) throw new UnknownTypeException(this);
    }

    public LetToken LetToken { get; }
    public IExpression? EqualsTo { get; }

    public VariableDefinition VariableDefinition { get; }

    // Would be set after semantic analysis
    private LangPath? TypePath { get; set; }

    public void ResolvePaths(PathResolver resolver)
    {
        VariableDefinition.TypePath = VariableDefinition.TypePath?.Resolve(resolver);
        foreach (var i in Children.OfType<IPathResolvable>())
        {
            i.ResolvePaths(resolver);
        }
        resolver.AddToDeepestScope(VariableDefinition.Name,
            new NormalLangPath(VariableDefinition.IdentifierToken, [VariableDefinition.Name]));
    }

    public void CodeGen(CodeGenContext context)
    {
        if (EqualsTo is not null)
        {
            var genedVal = EqualsTo.CodeGen(context);
            var stackPtr = genedVal.StackAllocate(context);

            context.AddToDeepestScope(new NormalLangPath(null, [VariableDefinition.Name]), new ValueRefItem
            {
                Type = genedVal.Type,
                ValueRef = stackPtr
            });
        }
        else
        {
            var type = context.GetRefItemFor(VariableDefinition.TypePath) as TypeRefItem;

            var stackPtr = context.Builder.BuildAlloca(type.TypeRef, VariableDefinition.Name);

            context.AddToDeepestScope(new NormalLangPath(null, [VariableDefinition.Name]), new ValueRefItem
            {
                Type = type.Type,
                ValueRef = stackPtr
            });
        }
    }


    public IEnumerable<ISyntaxNode> Children
    {
        get
        {
            if (EqualsTo is not null) yield return EqualsTo;
        }
    }


    public Token Token => LetToken;

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Propagate declared type to RHS for inference before analyzing
        if (VariableDefinition.TypePath != null && EqualsTo != null)
        {
            if (EqualsTo is StructCreationExpression sce)
                sce.DeclaredType = VariableDefinition.TypePath;
            if (EqualsTo is FunctionCallExpression fce)
                fce.ExpectedReturnType = VariableDefinition.TypePath;
        }

        EqualsTo?.Analyze(analyzer);

        // Resolve qualified associated type paths (e.g., <i32 as Add<i32>>::Output → i32)
        if (VariableDefinition.TypePath != null)
            VariableDefinition.TypePath = analyzer.ResolveQualifiedTypePath(VariableDefinition.TypePath);

        if (TypePath is null)
        {
            if (VariableDefinition.TypePath is null && EqualsTo is null)
            {
                analyzer.AddException(new SemanticException(
                    $"Cannot determine type of variable '{VariableDefinition.Name}' — no type annotation and no initializer\n{Token.GetLocationStringRepresentation()}"));
                return;
            }

            if (VariableDefinition.TypePath is null && EqualsTo is not null)
            {
                TypePath = EqualsTo.TypePath;
            }
            else if (VariableDefinition.TypePath is not null && EqualsTo is null)
            {
                TypePath = VariableDefinition.TypePath;
            }
            else if (EqualsTo is not null && VariableDefinition.TypePath is not null)
            {
                if (EqualsTo.TypePath != VariableDefinition.TypePath)
                {
                    analyzer.AddException(new TypeMismatchException(
                        VariableDefinition.TypePath, EqualsTo.TypePath,
                        "Conflicting types in let binding",
                        Token.GetLocationStringRepresentation()));
                    TypePath = VariableDefinition.TypePath;
                }
                else
                {
                    TypePath = VariableDefinition.TypePath;
                }
            }
        }

        if (TypePath is null) return;

        // Shadowing: invalidate borrows from the old binding before replacing it
        analyzer.InvalidateBorrowsFrom(VariableDefinition.Name);

        // Fresh binding — unmark this variable from moved (handles shadowing)
        analyzer.UnmarkMoved(VariableDefinition.Name);

        // Track this variable in the current scope for lifetime invalidation
        analyzer.TrackScopeVariable(VariableDefinition.Name);

        // Extract borrow source from the RHS expression and register borrow relationship.
        // Handles: direct borrows, lifetime elision through function calls,
        // explicit lifetime annotations linking return to specific params.
        var borrowSources = ExtractBorrowSources(EqualsTo, analyzer);
        foreach (var (sourceName, refKind) in borrowSources)
        {
            // NLL-style: invalidate conflicting borrows
            analyzer.InvalidateConflictingBorrows(sourceName, refKind);
            analyzer.RegisterBorrow(sourceName, VariableDefinition.Name, refKind);

            if (!analyzer.IsFunctionParameter(sourceName))
                analyzer.MarkAsLocalBorrow(VariableDefinition.Name);
        }

        // If RHS is a variable and the type is not Copy, mark the source as moved
        if (EqualsTo is not null)
            analyzer.TryMarkExpressionAsMoved(EqualsTo);

        analyzer.RegisterVariableType(new NormalLangPath(VariableDefinition.IdentifierToken, [VariableDefinition.Name]),
            TypePath);
    }

    public static LetStatement Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not LetToken letToken) throw new ExpectedParserException(parser, [ParseType.Let], gotten);
        var variable = VariableDefinition.Parse(parser);
        var next = parser.Peek();
        if (next is EqualityToken)
        {
            parser.Pop();
            var expr = IExpression.Parse(parser);
            return new LetStatement(letToken, variable, expr);
        }

        return new LetStatement(letToken, variable, null);
    }

    public class UnknownTypeException : SemanticException
    {
        public UnknownTypeException(LetStatement statement)
        {
            Statement = statement;
        }

        public LetStatement Statement { get; }

        public override string Message =>
            $"The type of the let statement is unknown, since theres no equals to expression, and the type wasnt declared" +
            $"\n{Statement.LetToken.GetLocationStringRepresentation()}";
    }

    public class SemanticUnableToDetermineTypeOfLetVarException : SemanticException
    {
        public SemanticUnableToDetermineTypeOfLetVarException(LetStatement statement)
        {
            Statement = statement;
        }

        public LetStatement Statement { get; }

        public override string Message =>
            $"No type was declared and no expression was set for the let statement, so unable to determine the" +
            $" type of the let\n{Statement.LetToken.GetLocationStringRepresentation()}";
    }

    public bool HasGuaranteedExplicitReturn => EqualsTo?.HasGuaranteedExplicitReturn ?? false;

    /// <summary>
    /// Trace an argument expression back to the variable it borrows from.
    /// Handles direct borrows (&amp;x), variable references, and chains.
    /// </summary>
    private static string? TraceArgToSource(IExpression arg, SemanticAnalyzer analyzer)
    {
        string? origin = null;
        if (arg is PointerGetterExpression argPge)
            origin = GetVariableOrigin(argPge.PointingTo);
        else
            origin = GetVariableOrigin(arg);

        // If the argument is a variable holding a reference, trace to ultimate source
        if (origin != null && IsReferenceType(arg.TypePath))
        {
            var ultimate = analyzer.GetBorrowSource(origin);
            if (ultimate != null) origin = ultimate;
        }

        return origin;
    }

    /// <summary>Check if a type path is a reference type (&amp;T).</summary>
    private static bool IsReferenceType(LangPath? typePath)
    {
        return typePath is NormalLangPath nlp
               && nlp.Contains(RefTypeDefinition.GetRefModule());
    }

    /// <summary>Extract the RefKind from a reference type path.</summary>
    private static RefKind GetRefKindFromTypePath(LangPath typePath)
    {
        if (typePath is NormalLangPath nlp)
        {
            foreach (RefKind rk in Enum.GetValues(typeof(RefKind)))
            {
                var refName = RefTypeDefinition.GetRefName(rk);
                if (nlp.PathSegments.Any(s => s.ToString() == refName))
                    return rk;
            }
        }
        return RefKind.Shared;
    }

    /// <summary>
    /// Extract the variable name from an expression (for borrow tracking).
    /// Returns the root variable name if the expression is a simple variable or field access.
    /// </summary>
    private static string? GetVariableOrigin(IExpression? expr)
    {
        if (expr is PathExpression pe && pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
            return nlp.PathSegments[0].ToString();
        if (expr is FieldAccessExpression fae)
        {
            IExpression root = fae;
            while (root is FieldAccessExpression f) root = f.Caller;
            return GetVariableOrigin(root);
        }
        return null;
    }

    /// <summary>
    /// Extract borrow sources from an expression using Rust-style lifetime elision.
    /// Returns all variables that the result borrows from.
    /// </summary>
    private static List<(string sourceName, RefKind refKind)> ExtractBorrowSources(
        IExpression? expr, SemanticAnalyzer analyzer)
    {
        var results = new List<(string, RefKind)>();
        if (expr == null) return results;

        // Case 1: Direct borrow — &x, &mut x, &const x, &uniq x
        if (expr is PointerGetterExpression pge)
        {
            var origin = GetVariableOrigin(pge.PointingTo);
            if (origin != null)
                results.Add((origin, pge.RefKind));
            return results;
        }

        // Case 2: Function call returning a reference type
        if (expr is FunctionCallExpression fce && IsReferenceType(fce.TypePath))
        {
            var refKind = GetRefKindFromTypePath(fce.TypePath!);

            FunctionDefinition? funcDef = null;
            var lookupPath = fce.FunctionPath;
            funcDef = analyzer.GetDefinition(lookupPath) as FunctionDefinition;
            if (funcDef == null && lookupPath.GetFrontGenerics().Length > 0)
                funcDef = analyzer.GetDefinition(lookupPath.PopGenerics()) as FunctionDefinition;
            if (funcDef == null)
                funcDef = analyzer.GetDefinition(lookupPath.Pop()) as FunctionDefinition;

            if (funcDef != null)
            {
                if (funcDef.ReturnLifetime != null)
                {
                    // Explicit lifetimes: only params whose lifetime matches the return
                    for (int i = 0; i < funcDef.Arguments.Length && i < fce.Arguments.Length; i++)
                    {
                        if (!funcDef.ArgumentLifetimes.TryGetValue(i, out var paramLt))
                            continue;
                        if (paramLt != funcDef.ReturnLifetime) continue;

                        var origin = TraceArgToSource(fce.Arguments[i], analyzer);
                        if (origin != null)
                            results.Add((origin, refKind));
                    }
                }
                else
                {
                    // Elision: only if exactly one reference parameter
                    var refArgSources = new List<string>();
                    for (int i = 0; i < funcDef.Arguments.Length && i < fce.Arguments.Length; i++)
                    {
                        if (!IsReferenceType(funcDef.Arguments[i].TypePath)) continue;
                        var origin = TraceArgToSource(fce.Arguments[i], analyzer);
                        if (origin != null)
                            refArgSources.Add(origin);
                    }
                    if (refArgSources.Count == 1)
                        results.Add((refArgSources[0], refKind));
                }
            }
            else
            {
                // Fallback for trait method calls — use signature lifetime info
                var traitSig = analyzer.ResolveTraitMethodSignature(fce.FunctionPath);
                if (traitSig != null && IsReferenceType(traitSig.ReturnTypePath))
                {
                    if (traitSig.ReturnLifetime != null)
                    {
                        // Explicit lifetimes on trait method
                        for (int i = 0; i < traitSig.Parameters.Length && i < fce.Arguments.Length; i++)
                        {
                            if (!traitSig.ArgumentLifetimes.TryGetValue(i, out var paramLt)) continue;
                            if (paramLt != traitSig.ReturnLifetime) continue;
                            var origin = TraceArgToSource(fce.Arguments[i], analyzer);
                            if (origin != null)
                                results.Add((origin, refKind));
                        }
                    }
                    else
                    {
                        // Elision: count ref params from signature
                        var refArgSources = new List<string>();
                        for (int i = 0; i < traitSig.Parameters.Length && i < fce.Arguments.Length; i++)
                        {
                            if (!IsReferenceType(traitSig.Parameters[i].TypePath)) continue;
                            var origin = TraceArgToSource(fce.Arguments[i], analyzer);
                            if (origin != null)
                                refArgSources.Add(origin);
                        }
                        if (refArgSources.Count == 1)
                            results.Add((refArgSources[0], refKind));
                    }
                }
            }

            return results;
        }

        // Case 3: Method call returning a reference type
        if (expr is MethodCallExpression mce && IsReferenceType(mce.TypePath))
        {
            var refKind = GetRefKindFromTypePath(mce.TypePath!);
            var receiverOrigin = GetVariableOrigin(mce.Receiver);
            if (receiverOrigin != null)
            {
                if (IsReferenceType(mce.Receiver.TypePath))
                {
                    var ultimate = analyzer.GetBorrowSource(receiverOrigin);
                    if (ultimate != null) receiverOrigin = ultimate;
                }
                results.Add((receiverOrigin, refKind));
            }
            return results;
        }

        // Case 4: Variable that already holds a reference — propagate its borrow source
        if (expr is PathExpression pathExpr && IsReferenceType(pathExpr.TypePath))
        {
            var varName = GetVariableOrigin(pathExpr);
            if (varName != null)
            {
                var ultimateSource = analyzer.GetBorrowSource(varName);
                if (ultimateSource != null)
                {
                    var refKind = GetRefKindFromTypePath(pathExpr.TypePath!);
                    results.Add((ultimateSource, refKind));
                }
            }
        }

        return results;
    }
}