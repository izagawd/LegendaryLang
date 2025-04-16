using System.Collections.Immutable;
using LegendaryLang.Parse;

namespace LegendaryLang.Semantics;

public class SemanticException : Exception;
public class SemanticAnalyzer 
{
    public Stack<IDefinition> Definitions = new Stack<IDefinition>();
    
    private Stack<Dictionary<LangPath, IRefItem>> ScopeItems = new();
    public SemanticAnalyzer(IEnumerable<ParseResult> parseResults)
    {
        Definitions = new Stack<IDefinition>(parseResults.SelectMany(i => i.Definitions).ToImmutableHashSet());
    }

    public void Analyze()
    {
        foreach (var definition in Definitions.ToArray())
        {

            try
            {
                definition.Analyze(this);
            }
            catch(NotImplementedException)
            {
 
            }
               
    
       
        }
    }
}