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
            // Mark source as moved BEFORE generating the value (for drop flag tracking)
            context.TryMarkExpressionDropMoved(EqualsTo);

            var genedVal = EqualsTo.CodeGen(context);
            var stackPtr = genedVal.StackAllocate(context);

            context.AddToDeepestScope(new NormalLangPath(null, [VariableDefinition.Name]), new ValueRefItem
            {
                Type = genedVal.Type,
                ValueRef = stackPtr
            });

            // Register for drop if the type implements Drop or has droppable fields
            if (genedVal.Type.TypePath != null &&
                (context.IsTypeDrop(genedVal.Type.TypePath) || context.TypeHasDroppableFields(genedVal.Type.TypePath)))
            {
                context.RegisterDroppable(VariableDefinition.Name, genedVal.Type.TypePath, stackPtr);
            }
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

            // Register for drop if the type implements Drop or has droppable fields
            if (type.Type.TypePath != null &&
                (context.IsTypeDrop(type.Type.TypePath) || context.TypeHasDroppableFields(type.Type.TypePath)))
            {
                context.RegisterDroppable(VariableDefinition.Name, type.Type.TypePath, stackPtr);
            }
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
            if (EqualsTo is ChainExpression chain)
                chain.ExpectedReturnType = VariableDefinition.TypePath;
        }

        EqualsTo?.Analyze(analyzer);

        // Resolve qualified associated type paths (e.g., (i32 as Add(i32)).Output → i32)
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

        // Cannot move a non-Copy value out of a dereference into a let binding.
        // *ref on non-Copy → MoveOutOfReferenceException (can't move out of borrow)
        // *box on non-Copy → SemanticException (can't copy non-Copy out of smart pointer)
        if (EqualsTo is DerefExpression { IsNonCopyPlaceDeref: true } deref)
        {
            if (deref.IsNonCopyRefDeref)
            {
                analyzer.AddException(new MoveOutOfReferenceException(
                    deref.TypePath, Token.GetLocationStringRepresentation()));
            }
            else
            {
                analyzer.AddException(new SemanticException(
                    $"Cannot move non-Copy type '{deref.TypePath}' out of dereference. " +
                    $"Use a reference instead: &*expr or access fields: (*expr).field\n{Token.GetLocationStringRepresentation()}"));
            }
        }

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
    internal static string? TraceArgToSource(IExpression arg, SemanticAnalyzer analyzer)
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
    internal static bool IsReferenceType(LangPath? typePath)
    {
        return typePath is NormalLangPath nlp
               && nlp.Contains(RefTypeDefinition.GetRefModule());
    }

    /// <summary>Extract the RefKind from a reference type path.</summary>
    internal static RefKind GetRefKindFromTypePath(LangPath typePath)
    {
        if (typePath is NormalLangPath nlp)
        {
            foreach (RefKind rk in Enum.GetValues(typeof(RefKind)))
            {
                var refName = RefTypeDefinition.GetRefName(rk);
                if (nlp.PathSegments.Any(s => s is NormalLangPath.NormalPathSegment nps && nps.Text == refName))
                    return rk;
            }
        }
        return RefKind.Shared;
    }

    /// <summary>
    /// Extract the variable name from an expression (for borrow tracking).
    /// Returns the root variable name if the expression is a simple variable or field access.
    /// </summary>
    internal static string? GetVariableOrigin(IExpression? expr)
    {
        if (expr is ChainExpression { SimpleVariableName: string varName })
            return varName;
        if (expr is ChainExpression chainWithSteps && chainWithSteps.Steps.Length > 0)
            return chainWithSteps.RootName; // &f.val → origin is "f"
        if (expr is PathExpression pe && pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
            return nlp.PathSegments[0].ToString();
        if (expr is FieldAccessExpression fae)
        {
            IExpression root = fae;
            while (root is FieldAccessExpression f) root = f.Caller;
            return GetVariableOrigin(root);
        }
        // Deref is transparent for borrow tracking — &*b borrows from b,
        // just like &a.field borrows from a.
        if (expr is DerefExpression deref)
            return GetVariableOrigin(deref.Inner);
        return null;
    }

    /// <summary>
    /// Extract borrow sources from an expression using Rust-style lifetime elision.
    /// Returns all variables that the result borrows from.
    /// </summary>
    internal static List<(string sourceName, RefKind refKind)> ExtractBorrowSources(
        IExpression? expr, SemanticAnalyzer analyzer)
    {
        var results = new List<(string, RefKind)>();
        if (expr == null) return results;

        // ChainExpression — delegate to the kind's borrow source extraction
        if (expr is ChainExpression chain && chain.ResolvedKind != null)
        {
            var kindSources = chain.ResolvedKind.GetBorrowSources(analyzer);
            if (kindSources.Count > 0) return kindSources;
        }

        // Case 1: Direct borrow — &x, &mut x, &const x, &uniq x
        if (expr is PointerGetterExpression pge)
        {
            var origin = GetVariableOrigin(pge.PointingTo);
            if (origin != null)
                results.Add((origin, pge.RefKind));
            return results;
        }

        // Case 2: Variable that already holds a reference — propagate its borrow source
        if ((expr is ChainExpression || expr is PathExpression) && IsReferenceType(expr.TypePath))
        {
            var varName = GetVariableOrigin(expr);
            if (varName != null)
            {
                var ultimateSource = analyzer.GetBorrowSource(varName);
                if (ultimateSource != null)
                {
                    var refKind = GetRefKindFromTypePath(expr.TypePath!);
                    results.Add((ultimateSource, refKind));
                }
            }
        }

        // Case 5: Struct creation with reference fields — track borrows from field initializers
        // e.g., Dropper { val = &uniq counter } borrows from counter
        if (expr is StructCreationExpression sce)
        {
            foreach (var field in sce.AssignFields)
            {
                var fieldSources = ExtractBorrowSources(field.EqualsTo, analyzer);
                results.AddRange(fieldSources);
            }
        }

        return results;
    }
}