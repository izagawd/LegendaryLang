using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public class TupleCreationExpression : IExpression
{
    private IExpression _expressionImplementation;

    public TupleCreationExpression(LeftParenthesisToken token, IEnumerable<IExpression> composites)
    {
        Composites = composites.ToImmutableArray();
        Token = token;
    }

    public ImmutableArray<IExpression> Composites { get; set; }
    public IEnumerable<ISyntaxNode> Children => Composites;
    public Token Token { get; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        foreach (var i in Composites) i.Analyze(analyzer);
    }


    public ValueRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        throw new NotImplementedException();
    }

    public LangPath? TypePath { get; }
    public bool HasGuaranteedExplicitReturn => Composites.Any(i => i.HasGuaranteedExplicitReturn);
}