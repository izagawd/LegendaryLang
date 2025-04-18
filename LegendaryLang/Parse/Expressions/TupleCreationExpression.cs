using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public class TupleCreationExpression : IExpression
{
    private IExpression _expressionImplementation;
    public ImmutableArray<IExpression> Composites { get; set; }
    public TupleCreationExpression(LeftParenthesisToken token, IEnumerable<IExpression> composites)
    {
        Composites = composites.ToImmutableArray();
        Token = token;
    }
    public Token Token { get; }
    public void Analyze(SemanticAnalyzer analyzer)
    {
        foreach (var i in Composites)
        {
            i.Analyze(analyzer);
        }
    }


    public void SetFullPathOfShortCuts(SemanticAnalyzer analyzer)
    {
        foreach (var i in Composites)
        {
            i.SetFullPathOfShortCuts(analyzer);
        }
    }

    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return Composites.SelectMany(composite => composite.GetAllFunctionsUsed());
    }

    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        throw new NotImplementedException();
    }

    public LangPath? TypePath { get; }
}