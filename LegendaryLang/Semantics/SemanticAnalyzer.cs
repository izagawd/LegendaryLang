using System.Collections.Immutable;
using LegendaryLang.Parse;

namespace LegendaryLang.Semantics;

public class SemanticException : Exception;
public class SemanticAnalyzer 
{
    public Stack<ParseResult> ParseResults = new Stack<ParseResult>();
    
    private Stack<Dictionary<string, NormalLangPath>> ScopeItems = new();
    public SemanticAnalyzer(IEnumerable<ParseResult> parseResults)
    {
        ParseResults = new Stack<ParseResult>(parseResults);
    }

    private Dictionary<LangPath, IDefinition> DefinitionsMap = [];



    public IDefinition? GetDefinition(LangPath langPath)
    {
       if(DefinitionsMap.TryGetValue(langPath, out IDefinition? definition))
       {
           return definition;
       }
       return null;
    }

    public void RegisterDefinition(LangPath path, IDefinition definition)
    {
        DefinitionsMap[path] = definition;
    }

    public void  AddScope()
    {

        ScopeItems.Push(new());
 
    }

    public NormalLangPath? GetFullPathOfShortcut(string shortcut)
    {
        foreach (var scope in ScopeItems)
        {
            if (scope.TryGetValue(shortcut, out var symbol))
            {

                return symbol;
                
             
            }
        }
        return null;
    }
    public void AddToDeepestScope(string map, NormalLangPath to)
    {
        ScopeItems.Peek().Add(map, to);
    }
    public Dictionary<string, NormalLangPath> PopScope()
    {
        return ScopeItems.Pop();

    }
    public void Analyze()
    {
        
        // registers path mapping
        foreach (var i in ParseResults.SelectMany(i => i.TopLevels.OfType<IDefinition>()))
        {
            RegisterDefinition(i.FullPath,i);
        }
        
        foreach (var result in ParseResults)
        {
            AddScope();
            foreach (var i in result.TopLevels)
            {
                try
                {
                    i.Analyze(this);
                }
                catch(NotImplementedException)
                {
 
                }
            }
            PopScope();
       
        }
  
    }
}