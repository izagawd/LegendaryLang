using System.Collections.Immutable;
using LegendaryLang.Parse;

namespace LegendaryLang.Semantics;

public class SemanticException : Exception;
public class SemanticAnalyzer 
{
    public Stack<IDefinition> Definitions = new Stack<IDefinition>();
    
    private Stack<Dictionary<LangPath, LangPath>> ScopeItems = new();
    public SemanticAnalyzer(IEnumerable<ParseResult> parseResults)
    {
        Definitions = new Stack<IDefinition>(parseResults.SelectMany(i => i.Definitions).ToImmutableHashSet());
    }



    public void  AddScope()
    {

        ScopeItems.Push(new());
 
    }

    public void AddToDeepestScope(LangPath map, LangPath to)
    {
        ScopeItems.Peek().Add(map, to);
    }
    public Dictionary<LangPath, LangPath> PopScope()
    {
        return ScopeItems.Pop();

    }
    public void Analyze()
    {
        AddScope();
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
        PopScope();
    }
}