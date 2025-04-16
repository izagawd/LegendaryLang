using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public class TupleCreationExpression : IExpression
{
    public ImmutableArray<IExpression> Composites { get; set; }
    public TupleCreationExpression(LeftParenthesisToken token, IEnumerable<IExpression> composites)
    {
        Composites = composites.ToImmutableArray();
        LookUpToken = token;
    }
    public Token LookUpToken { get; }
    public void Analyze(SemanticAnalyzer analyzer)
    {
        foreach (var i in Composites)
        {
            i.Analyze(analyzer);
        }
    }

    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        throw new NotImplementedException();
    }

    public LangPath? TypePath { get; }
}