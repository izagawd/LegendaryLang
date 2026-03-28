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
        // This handles:
        //   1. Direct borrows: let r = &x;
        //   2. Lifetime elision through function calls: let r = foo(&x);
        //      If foo returns a reference, apply Rust-style elision rules:
        //      - If exactly one input is a reference, output borrows from that source
        //      - If there's a self parameter, output borrows from self's source
        var borrowInfo = ExtractBorrowSource(EqualsTo, analyzer);
        if (borrowInfo != null)
        {
            var (sourceName, refKind) = borrowInfo.Value;

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
    /// Extract borrow source from an expression using Rust-style lifetime elision.
    /// Handles direct borrows (&amp;x), function calls returning references,
    /// and method calls returning references.
    ///
    /// Elision rules (from Rust):
    /// 1. If there is exactly one reference input parameter, its lifetime is assigned to all output references.
    /// 2. If there is a self parameter that is a reference, its lifetime is assigned to all output references.
    /// </summary>
    private static (string sourceName, RefKind refKind)? ExtractBorrowSource(
        IExpression? expr, SemanticAnalyzer analyzer)
    {
        if (expr == null) return null;

        // Case 1: Direct borrow — &x, &mut x, &const x, &uniq x
        if (expr is PointerGetterExpression pge)
        {
            var origin = GetVariableOrigin(pge.PointingTo);
            if (origin != null)
                return (origin, pge.RefKind);
            return null;
        }

        // Case 2: Function call returning a reference type
        if (expr is FunctionCallExpression fce && IsReferenceType(fce.TypePath))
        {
            var refKind = GetRefKindFromTypePath(fce.TypePath!);

            // Look up the function definition to check declared parameter types
            // This is signature-based, not argument-based — future-proof for Deref coercion
            FunctionDefinition? funcDef = null;
            var lookupPath = fce.FunctionPath;
            // Try direct lookup, then strip generics, then pop
            funcDef = analyzer.GetDefinition(lookupPath) as FunctionDefinition;
            if (funcDef == null && lookupPath.GetFrontGenerics().Length > 0)
                funcDef = analyzer.GetDefinition(lookupPath.PopGenerics()) as FunctionDefinition;
            if (funcDef == null)
                funcDef = analyzer.GetDefinition(lookupPath.Pop()) as FunctionDefinition;

            if (funcDef != null)
            {
                var refArgSources = new List<string>();
                for (int i = 0; i < funcDef.Arguments.Length && i < fce.Arguments.Length; i++)
                {
                    var declaredParamType = funcDef.Arguments[i].TypePath;
                    if (!IsReferenceType(declaredParamType)) continue;

                    // This parameter is declared as a reference type — trace the actual argument
                    var arg = fce.Arguments[i];
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

                    if (origin != null)
                        refArgSources.Add(origin);
                }

                // Elision rule: if exactly one reference parameter, output borrows from it
                if (refArgSources.Count == 1)
                    return (refArgSources[0], refKind);
            }
            else
            {
                // Fallback for trait method calls where FunctionDefinition isn't directly found:
                // use ResolveTraitMethodReturnType path to find the trait method signature
                var traitReturnType = analyzer.ResolveTraitMethodReturnType(fce.FunctionPath);
                if (traitReturnType != null)
                {
                    // Can't inspect trait method params easily, fall back to argument inspection
                    var refArgSources = new List<string>();
                    for (int i = 0; i < fce.Arguments.Length; i++)
                    {
                        var arg = fce.Arguments[i];
                        string? origin = null;
                        if (arg is PointerGetterExpression argPge)
                            origin = GetVariableOrigin(argPge.PointingTo);
                        else if (IsReferenceType(arg.TypePath))
                        {
                            origin = GetVariableOrigin(arg);
                            if (origin != null)
                            {
                                var ultimate = analyzer.GetBorrowSource(origin);
                                if (ultimate != null) origin = ultimate;
                            }
                        }
                        if (origin != null)
                            refArgSources.Add(origin);
                    }
                    if (refArgSources.Count == 1)
                        return (refArgSources[0], refKind);
                }
            }

            return null;
        }

        // Case 3: Method call returning a reference type
        // Elision rule: if self param is a reference, output borrows from self's source
        if (expr is MethodCallExpression mce && IsReferenceType(mce.TypePath))
        {
            var refKind = GetRefKindFromTypePath(mce.TypePath!);

            // The receiver is always the "self" borrow source (Rust elision rule 2)
            var receiverOrigin = GetVariableOrigin(mce.Receiver);
            if (receiverOrigin != null)
            {
                // If receiver itself holds a reference, trace to ultimate source
                if (IsReferenceType(mce.Receiver.TypePath))
                {
                    var ultimate = analyzer.GetBorrowSource(receiverOrigin);
                    if (ultimate != null) receiverOrigin = ultimate;
                }
                return (receiverOrigin, refKind);
            }

            return null;
        }

        // Case 4: Variable that already holds a reference — propagate its borrow source
        if (expr is PathExpression pathExpr && IsReferenceType(pathExpr.TypePath))
        {
            var varName = GetVariableOrigin(pathExpr);
            if (varName != null)
            {
                // Find what this variable borrows from
                var ultimateSource = analyzer.GetBorrowSource(varName);
                if (ultimateSource != null)
                {
                    var refKind = GetRefKindFromTypePath(pathExpr.TypePath!);
                    return (ultimateSource, refKind);
                }
            }
        }

        return null;
    }
}