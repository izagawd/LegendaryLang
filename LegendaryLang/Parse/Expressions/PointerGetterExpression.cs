using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class PointerGetterExpression : IExpression
{
    private readonly bool _isMut;

    public static PointerGetterExpression Parse(Parser parser)
    {
        var popped = parser.Pop();
        if (popped is not AmpersandToken ampersandToken)
        {
            throw new ExpectedParserException(parser,[ParseType. Ampersand],popped);
        }
        var peeked = parser.Peek();
        var isMut = false;
        if (peeked is MutToken mutToken)
        {
            isMut = true;
            parser.Pop();
        }
        var expr = IExpression.Parse(parser);
        return new PointerGetterExpression(expr, ampersandToken, isMut);
    }

    
    public PointerGetterExpression(IExpression pointingTo, AmpersandToken token, bool isMut)
    {
        _isMut = isMut;
        PointingTo = pointingTo;
        Token = token;
    }
    public IExpression PointingTo { get; }
    public IEnumerable<ISyntaxNode> Children => [PointingTo];
    public Token Token { get; }
    public void Analyze(SemanticAnalyzer analyzer)
    {
        PointingTo.Analyze(analyzer);
        if (PointingTo is not FieldAccessExpression && PointingTo is not PathExpression)
        {
            analyzer.AddException(new SemanticException("Pointer point must be a field access, or a variable access\n" + Token.GetLocationStringRepresentation()));
        }

    }

    public bool HasGuaranteedExplicitReturn => PointingTo.HasGuaranteedExplicitReturn;
    public LangPath? TypePath => PointerTypeDefinition.GetPointerModule().Append(PointerTypeDefinition.GetPointerName(_isMut)).Append(new NormalLangPath.GenericTypesPathSegment([PointingTo.TypePath]));
    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        return PointingTo.CodeGen(codeGenContext);
        
        
    }
}