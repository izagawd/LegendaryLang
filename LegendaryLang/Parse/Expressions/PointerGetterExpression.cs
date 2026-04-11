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

    /// <summary>
    /// True for &amp;raw / &amp;raw mut expressions that produce raw pointers (*shared T / *mut T)
    /// instead of references (&amp;T / &amp;mut T). Raw pointer casts skip borrow checking.
    /// </summary>
    public bool IsRaw { get; }

    public static PointerGetterExpression Parse(Parser parser)
    {
        var popped = parser.Pop();
        if (popped is not AmpersandToken ampersandToken)
        {
            throw new ExpectedParserException(parser,[ParseType. Ampersand],popped);
        }

        // Check for &raw / &raw mut → produces raw pointers instead of references
        bool isRaw = false;
        if (parser.Peek() is RawToken)
        {
            parser.Pop();
            isRaw = true;
        }

        var refKind = RefKindParser.Parse(parser);

        var expr = IExpression.ParsePrimary(parser);
        return new PointerGetterExpression(expr, ampersandToken, refKind, isRaw);
    }

    
    public PointerGetterExpression(IExpression pointingTo, AmpersandToken token, RefKind refKind, bool isRaw = false)
    {
        _refKind = refKind;
        PointingTo = pointingTo;
        Token = token;
        IsRaw = isRaw;
    }
    public IExpression PointingTo { get; }
    public IEnumerable<ISyntaxNode> Children => [PointingTo];
    public Token Token { get; }

    /// <summary>Name of the variable being borrowed, if it's a simple variable.</summary>
    public string? BorrowOriginName { get; private set; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        if (IsRaw)
        {
            // Raw pointer cast: no borrow checking, just analyze the inner expression
            PointingTo.Analyze(analyzer);

            // Validate target
            if (PointingTo is not FieldAccessExpression && PointingTo is not PathExpression
                && PointingTo is not ChainExpression && PointingTo is not DerefExpression)
            {
                analyzer.AddException(new SemanticException(
                    "Raw pointer target must be a field access, variable access, or dereference\n" +
                    Token.GetLocationStringRepresentation()));
            }
            return;
        }

        // Core borrow analysis — shared with comparison operator sugar
        AnalyzeBorrow(PointingTo, _refKind, analyzer);

        // Extra validation specific to explicit & expressions
        if (PointingTo is not FieldAccessExpression && PointingTo is not PathExpression
            && PointingTo is not ChainExpression && PointingTo is not DerefExpression)
        {
            analyzer.AddException(new SemanticException("Reference target must be a field access, variable access, or dereference\n" + Token.GetLocationStringRepresentation()));
        }

        BorrowOriginName = ExtractBorrowOrigin(PointingTo);

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
        expr.Analyze(analyzer);
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
    public LangPath? TypePath => IsRaw
        ? RawPtrTypeDefinition.GetRawPtrModule()
            .Append(RawPtrTypeDefinition.GetRawPtrName(_refKind))
            .AppendGenerics([PointingTo.TypePath])
        : RefTypeDefinition.GetRefModule()
            .Append(RefTypeDefinition.GetRefName(_refKind))
            .AppendGenerics([PointingTo.TypePath]);
    public bool IsTemporary => true; // creates new reference/pointer

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        // The inner expression's CodeGen returns a ValueRefItem whose ValueRef
        // is already a pointer (alloca) to the value. That pointer IS the reference/raw pointer.
        var innerVal = PointingTo.CodeGen(codeGenContext);

        var ptrTypeRef = codeGenContext.GetRefItemFor(TypePath) as TypeRefItem;

        if (IsRaw)
        {
            // Raw pointer: same LLVM layout as references, just different type wrapper
            if (ptrTypeRef?.Type is RawPtrType rawPtrType)
                return rawPtrType.WrapAsRef(codeGenContext, innerVal);
            return innerVal;
        }

        if (ptrTypeRef?.Type is RefType refType)
            return refType.WrapAsRef(codeGenContext, innerVal);

        // Fallback — shouldn't happen
        return innerVal;
    }

    public void ResolvePaths(PathResolver resolver)
    {
        // Resolve the inner expression's paths so type lookup works
        // for comptime type args like &Foo
        if (PointingTo is ChainExpression chain)
            chain.ResolvePaths(resolver);
        else if (PointingTo is PathExpression pe)
            pe.ResolvePaths(resolver);
    }
}
