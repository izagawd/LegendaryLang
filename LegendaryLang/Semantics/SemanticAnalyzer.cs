using System.Collections.Immutable;
using LegendaryLang.Parse;

namespace LegendaryLang.Semantics;

public class SemanticAnalyzer 
{
    public Stack<IDefinition> Definitions = new Stack<IDefinition>();
    
    
    public SemanticAnalyzer(IEnumerable<ParseResult> parseResults)
    {
        Definitions = new Stack<IDefinition>(parseResults.SelectMany(i => i.Definitions).ToImmutableHashSet());
    }

    public void Analyze()
    {
        foreach (var definition in Definitions.ToArray())
        {
            definition.Analyze(this);
        }
    }
}