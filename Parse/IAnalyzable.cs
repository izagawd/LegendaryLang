using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public interface IAnalyzable
{
    public void Analyze(SemanticAnalyzer analyzer);
}