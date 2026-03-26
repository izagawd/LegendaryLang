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
        PointingTo.Analyze(analyzer);
        if (PointingTo is not FieldAccessExpression && PointingTo is not PathExpression)
        {
            analyzer.AddException(new SemanticException("Reference target must be a field access or a variable access\n" + Token.GetLocationStringRepresentation()));
        }

        // Track borrow origin for lifetime checking
        if (PointingTo is PathExpression pe && pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            BorrowOriginName = nlp.PathSegments[0].ToString();
        }
        else if (PointingTo is FieldAccessExpression fae)
        {
            // For &s.field, the origin is s
            IExpression root = fae;
            while (root is FieldAccessExpression f) root = f.Caller;
            if (root is PathExpression rpe && rpe.Path is NormalLangPath rnlp && rnlp.PathSegments.Length == 1)
            {
                BorrowOriginName = rnlp.PathSegments[0].ToString();
            }
        }
    }

    public bool HasGuaranteedExplicitReturn => PointingTo.HasGuaranteedExplicitReturn;
    public LangPath? TypePath => RefTypeDefinition.GetRefModule()
        .Append(RefTypeDefinition.GetRefName(_refKind))
        .Append(new NormalLangPath.GenericTypesPathSegment([PointingTo.TypePath]));

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
