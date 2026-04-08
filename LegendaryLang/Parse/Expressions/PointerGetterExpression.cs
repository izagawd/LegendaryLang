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

        var refKind = RefKindParser.Parse(parser);

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
        // Core borrow analysis — shared with comparison operator sugar
        AnalyzeBorrow(PointingTo, _refKind, analyzer);

        // Extra validation specific to explicit & expressions
        if (PointingTo is not FieldAccessExpression && PointingTo is not PathExpression
            && PointingTo is not ChainExpression && PointingTo is not DerefExpression)
        {
            analyzer.AddException(new SemanticException("Reference target must be a field access, variable access, or dereference\n" + Token.GetLocationStringRepresentation()));
        }

        BorrowOriginName = ExtractBorrowOrigin(PointingTo);

        // &*tempExpr where the inner deref is a temporary smart pointer (e.g., &*Box.New(45))
        // materializes an anonymous local "_" at codegen time. That local lives only in the
        // current block, so the reference must not escape it. Treat it as borrowing from "_"
        // in the current scope so the block-escape dangling-reference check fires.
        if (PointingTo is DerefExpression { IsDerefTrait: true } derefTmp
            && derefTmp.Inner.IsTemporary
            && BorrowOriginName == null)
        {
            BorrowOriginName = "_";
            analyzer.TrackScopeVariable("_");
        }

        if (PointingTo is DerefExpression derefExpr)
        {
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
    }

    /// <summary>
    /// Core borrow analysis: suppresses use-while-borrowed checks, analyzes the expression,
    /// extracts the borrow origin, and invalidates conflicting borrows.
    /// Shared between explicit &amp; expressions and comparison operator sugar.
    /// </summary>
    internal static void AnalyzeBorrow(IExpression expr, RefKind refKind, SemanticAnalyzer analyzer)
    {
        var prevSuppress = analyzer.SuppressUseWhileBorrowedChecks;
        analyzer.SuppressUseWhileBorrowedChecks = true;
        expr.Analyze(analyzer);
        analyzer.SuppressUseWhileBorrowedChecks = prevSuppress;

        var borrowOrigin = ExtractBorrowOrigin(expr);
        if (borrowOrigin != null)
            analyzer.InvalidateConflictingBorrows(borrowOrigin, refKind);
    }

    /// <summary>
    /// Walks through deref/field-access chains to find the root variable being borrowed.
    /// Skips raw pointer derefs (heap memory, not local stack).
    /// </summary>
    internal static string? ExtractBorrowOrigin(IExpression expr)
    {
        // Chain with steps: &f.val → root "f"
        if (expr is ChainExpression chain)
            return chain.SimpleVariableName ?? (chain.Steps.Length > 0 ? chain.RootName : null);

        // Simple variable
        var simple = IExpression.TryGetSimpleVariableName(expr);
        if (simple != null) return simple;

        // Deref: &*x → origin is x (but skip raw pointer derefs)
        if (expr is DerefExpression deref)
        {
            // Walk through nested derefs to find root
            IExpression inner = deref.Inner;
            while (inner is DerefExpression de) inner = de.Inner;

            // Skip raw pointer derefs — they point to heap, not local stack
            if (inner.TypePath is NormalLangPath innerNlp
                && innerNlp.Contains(RawPtrTypeDefinition.GetRawPtrModule()))
                return null;

            return IExpression.TryGetSimpleVariableName(inner);
        }

        // Field access: &s.field → origin is s
        if (expr is FieldAccessExpression fae)
        {
            IExpression root = fae;
            while (root is FieldAccessExpression f) root = f.Caller;
            return IExpression.TryGetSimpleVariableName(root);
        }

        return null;
    }

    public bool HasGuaranteedExplicitReturn => PointingTo.HasGuaranteedExplicitReturn;
    public LangPath? TypePath => RefTypeDefinition.GetRefModule()
        .Append(RefTypeDefinition.GetRefName(_refKind))
        .AppendGenerics([PointingTo.TypePath]);
    public bool IsTemporary => true; // creates new reference

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

    public void ResolvePaths(PathResolver resolver)
    {
        // Resolve the inner expression's paths so type lookup works
        // for comptime type args like &const Foo
        if (PointingTo is ChainExpression chain)
            chain.ResolvePaths(resolver);
        else if (PointingTo is PathExpression pe)
            pe.ResolvePaths(resolver);
    }
}
