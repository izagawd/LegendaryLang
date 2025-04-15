using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public interface ISyntaxNode
{
    public  Token LookUpToken { get; }
    public void Analyze(SemanticAnalyzer analyzer);
}