using System.Collections.Immutable;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;

namespace LegendaryLang.Semantics;

public class SemanticException : Exception
{
    public SemanticException(string message) : base(message){}
    public SemanticException(){}
}
public class SemanticAnalyzer
{
    private List<SemanticException> Exceptions = [];

    public void AddException(SemanticException exception)
    {
        Exceptions.Add(exception);
    }
    public Stack<ParseResult> ParseResults = new();
    
    private Stack<Dictionary<string, NormalLangPath>> ScopeItems = new();
    private Stack<Dictionary<LangPath, LangPath>> VariableToTypeMapper = new();
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

    public LangPath? GetVariableTypePath(LangPath variableLangPath)
    {
        foreach (var scope in VariableToTypeMapper)
        {
            if (scope.TryGetValue(variableLangPath, out var symbol))
            {

                return symbol;
                
             
            }
        }
        return null;
    }
    public void RegisterVariableType(LangPath variableLangPath, LangPath typPath, int? scope = null)
    {
        if (scope is null)
        {
            VariableToTypeMapper.Peek().Add(variableLangPath, typPath);
        }
        else
        {
            VariableToTypeMapper.Reverse().Skip(scope.Value).First().Add(variableLangPath, typPath);
        }
       
    }
    public void RegisterDefinition(LangPath path, IDefinition definition)
    {
        DefinitionsMap[path] = definition;
    }

    public void  AddScope()
    {

        ScopeItems.Push(new());
        VariableToTypeMapper.Push(new ());
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
    public void PopScope()
    {
         ScopeItems.Pop();
         VariableToTypeMapper.Pop();
    }

    /// <returns>Collection of semantic errors that occured</returns>
    public SemanticException[] Analyze()
    {
        
        // registers path mapping
        foreach (var i in ParseResults.SelectMany(i => i.TopLevels.OfType<IDefinition>()))
        {
            RegisterDefinition(i.FullPath,i);
        }
        
        foreach (var result in ParseResults)
        {
            AddScope();
            foreach (var i in result.TopLevels.OfType<UseDefinition>())
            {
                i.RegisterUsings(this);
            }

            void SetFullPathOfShortCutsRecursively(ISyntaxNode node)
            {
                if (node is IPathHaver pathHaver)
                {
                    
                    pathHaver.SetFullPathOfShortCutsDirectly(this);
                }

                foreach (var child in node.Children)
                {
                    var isBlock = child is BlockExpression;
                    if (isBlock)
                    {
                        AddScope();
                    }
                    SetFullPathOfShortCutsRecursively(child);
                    if (isBlock)
                    {
                        PopScope();
                    }
                }
            }
            foreach (var i in result.TopLevels)
            {
              
                SetFullPathOfShortCutsRecursively(i);
                
            }
            PopScope();

        }
        foreach (var result in ParseResults)
        {
            AddScope();
            foreach (var i in result.TopLevels)
            {
              
                i.Analyze(this);
                

            }
            PopScope();

        }
        return Exceptions.ToArray();
  
    }
}