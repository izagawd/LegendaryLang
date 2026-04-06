using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class PointerGetterExpression : IExpression
{
    private readonly RefKind _refKind;
    public RefKind RefKind => _refKind;

    public static PointerGetterExpression Parse(Parser parser)
    {
        var popped = parser.Pop();
        if (popped is not AmpersandToken ampersandToken)
        {
            throw new ExpectedParserException(parser,[ParseType. Ampersand],popped);
        }

        var refKind = RefKind.Shared;
        if (parser.Peek() is MutToken)
        {
            refKind = RefKind.Mut;
            parser.Pop();
        }
        else if (parser.Peek() is IdentifierToken { Identity: "const" })
        {
            refKind = RefKind.Const;
            parser.Pop();
        }
        else if (parser.Peek() is IdentifierToken { Identity: "uniq" })
        {
            refKind = RefKind.Uniq;
            parser.Pop();
        }

        var expr = IExpression.ParsePrimary(parser);
        return new PointerGetterExpression(expr, ampersandToken, refKind);
    }

    
    public PointerGetterExpression(IExpression pointingTo, AmpersandToken token, RefKind refKind)
    {
        _refKind = refKind;
        PointingTo = pointingTo;
        Token = token;
    }
    public IExpression PointingTo { get; }
    public IEnumerable<ISyntaxNode> Children => [PointingTo];
    public Token Token { get; }

    /// <summary>Name of the variable being borrowed, if it's a simple variable.</summary>
    public string? BorrowOriginName { get; private set; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Suppress the "use while borrowed" check — re-borrowing is handled
        // by borrow compatibility rules, not the source-use check.
        var prevSuppress = analyzer.SuppressUseWhileBorrowedChecks;
        analyzer.SuppressUseWhileBorrowedChecks = true;
        PointingTo.Analyze(analyzer);
        analyzer.SuppressUseWhileBorrowedChecks = prevSuppress;
        if (PointingTo is not FieldAccessExpression && PointingTo is not PathExpression
            && PointingTo is not ChainExpression && PointingTo is not DerefExpression)
        {
            analyzer.AddException(new SemanticException("Reference target must be a field access, variable access, or dereference\n" + Token.GetLocationStringRepresentation()));
        }

        // Track borrow origin for lifetime checking
        if (PointingTo is ChainExpression { SimpleVariableName: string chainVar })
        {
            BorrowOriginName = chainVar;
        }
        else if (PointingTo is ChainExpression chainWithSteps && chainWithSteps.Steps.Length > 0)
        {
            // For &f.val — root "f" is the borrow origin
            BorrowOriginName = chainWithSteps.RootName;
        }
        else if (PointingTo is PathExpression pe && pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            BorrowOriginName = nlp.PathSegments[0].ToString();
        }
        else if (PointingTo is DerefExpression derefExpr)
        {
            // For &*boxed_foo, the borrow origin is the variable holding the Box/ref.
            // But NOT for raw pointer derefs — raw pointers point to heap memory,
            // not local stack. &uniq *raw_ptr is not a local borrow.
            IExpression inner = derefExpr.Inner;

            // Skip borrow tracking for raw pointer derefs
            bool isRawPtrDeref = inner.TypePath is NormalLangPath innerNlp
                && innerNlp.Contains(RawPtrTypeDefinition.GetRawPtrModule());

            if (!isRawPtrDeref)
            {
                if (inner is ChainExpression { SimpleVariableName: string dcv })
                    BorrowOriginName = dcv;
                else if (inner is PathExpression dpe && dpe.Path is NormalLangPath dnlp && dnlp.PathSegments.Length == 1)
                    BorrowOriginName = dnlp.PathSegments[0].ToString();
                else if (inner is DerefExpression)
                {
                    IExpression root = inner;
                    while (root is DerefExpression de) root = de.Inner;
                    if (root is ChainExpression { SimpleVariableName: string rcv })
                        BorrowOriginName = rcv;
                    else if (root is PathExpression rpe && rpe.Path is NormalLangPath rnlp && rnlp.PathSegments.Length == 1)
                        BorrowOriginName = rnlp.PathSegments[0].ToString();
                }
            }

            // Check deref capability: can this deref source produce the requested ref kind?
            // Unified for raw pointers, references, AND trait-based deref (Box etc.)
            // Follows the trait hierarchy: Deref(&) / DerefConst(&const,&) / DerefMut(&mut,&) / DerefUniq(all)
            if (derefExpr.SourceDerefKind != null
                && !DerefExpression.CanProduceRefKind(derefExpr.SourceDerefKind.Value, _refKind))
            {
                analyzer.AddException(new SemanticException(
                    $"Cannot create '&{RefTypeDefinition.GetRefName(_refKind)}' reference from " +
                    $"this dereference (source only supports up to " +
                    $"'&{RefTypeDefinition.GetRefName(derefExpr.SourceDerefKind.Value)}')" +
                    $"\n{Token.GetLocationStringRepresentation()}"));
            }
        }
        else if (PointingTo is FieldAccessExpression fae)
        {
            // For &s.field, the origin is s
            IExpression root = fae;
            while (root is FieldAccessExpression f) root = f.Caller;
            if (root is ChainExpression { SimpleVariableName: string fcv })
            {
                BorrowOriginName = fcv;
            }
            else if (root is PathExpression rpe && rpe.Path is NormalLangPath rnlp && rnlp.PathSegments.Length == 1)
            {
                BorrowOriginName = rnlp.PathSegments[0].ToString();
            }
        }

        // Invalidate conflicting borrows even for standalone expressions like `&mut a;`
        if (BorrowOriginName != null)
            analyzer.InvalidateConflictingBorrows(BorrowOriginName, _refKind);
    }

    public bool HasGuaranteedExplicitReturn => PointingTo.HasGuaranteedExplicitReturn;
    public LangPath? TypePath => RefTypeDefinition.GetRefModule()
        .Append(RefTypeDefinition.GetRefName(_refKind))
        .AppendGenerics([PointingTo.TypePath]);

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        // The inner expression's CodeGen returns a ValueRefItem whose ValueRef
        // is already a pointer (alloca) to the value. That pointer IS the reference.
        var innerVal = PointingTo.CodeGen(codeGenContext);

        // Resolve the pointer type
        var ptrTypeRef = codeGenContext.GetRefItemFor(TypePath) as TypeRefItem;
        if (ptrTypeRef?.Type is RefType refType)
        {
            // The inner ValueRef is the address we want to store as the reference value.
            // Allocate a stack slot for the reference (pointer) and store the address.
            var alloca = codeGenContext.Builder.BuildAlloca(refType.TypeRef);
            codeGenContext.Builder.BuildStore(innerVal.ValueRef, alloca);
            return new ValueRefItem
            {
                Type = refType,
                ValueRef = alloca
            };
        }

        // Fallback — shouldn't happen
        return innerVal;
    }

    public void ResolvePaths(PathResolver resolver) { }
}
