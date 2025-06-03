using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;

namespace LegendaryLang.Semantics;

public class SemanticException : Exception
{
    public SemanticException(string message) : base(message)
    {
    }

    public SemanticException()
    {
    }
}


public class SemanticAnalyzer
{
    private readonly Dictionary<LangPath, IDefinition> DefinitionsMap = [];
    private readonly List<SemanticException> Exceptions = [];

    
    private readonly Stack<Dictionary<LangPath, LangPath>> VariableToTypeMapper = new();
    public Stack<ParseResult> ParseResults = new();

    public SemanticAnalyzer(IEnumerable<ParseResult> parseResults)
    {
        ParseResults = new Stack<ParseResult>(parseResults);
    }

    public void AddException(SemanticException exception)
    {
        Exceptions.Add(exception);
    }


    public IDefinition? GetDefinition(LangPath langPath)
    {
        if (DefinitionsMap.TryGetValue(langPath, out var definition)) return definition;
        return null;
    }

    public LangPath? GetVariableTypePath(LangPath variableLangPath)
    {
        foreach (var scope in VariableToTypeMapper)
            if (scope.TryGetValue(variableLangPath, out var symbol))
                return symbol;

        return null;
    }

    public void RegisterVariableType(LangPath variableLangPath, LangPath typPath, int? scope = null)
    {
        if (scope is null)
            VariableToTypeMapper.Peek().Add(variableLangPath, typPath);
        else
            VariableToTypeMapper.Reverse().Skip(scope.Value).First().Add(variableLangPath, typPath);
    }

    public void RegisterDefinition(LangPath path, IDefinition definition)
    {
        DefinitionsMap[path] = definition;
    }

    public void AddScope()
    {

        VariableToTypeMapper.Push(new Dictionary<LangPath, LangPath>());
    }

    


    public void PopScope()
    {
     
        VariableToTypeMapper.Pop();
    }

    private void ResolvePaths()
    {
        var pathShortcutContext = new PathResolver();
        foreach (var result in ParseResults)
        {
            pathShortcutContext. AddScope();
            foreach (var useDefinition in result.TopLevels.OfType<UseDefinition>())
            {
                useDefinition.RegisterUsings(pathShortcutContext);
            }
            foreach (var i in result.TopLevels)
            {
                i.SetFullPathOfShortCutsDirectly(pathShortcutContext);
            }
            pathShortcutContext.PopScope();
        }
    }
    /// <returns>Collection of semantic errors that occured</returns>
    public SemanticException[] Analyze()
    {
        // registers path mapping
        foreach (var i in ParseResults.SelectMany(i => i.TopLevels.OfType<IDefinition>()))
            RegisterDefinition(i.FullPath, i);


        ResolvePaths();
        foreach (var result in ParseResults)
        {
            AddScope();
            foreach (var i in result.TopLevels) i.Analyze(this);
            PopScope();
        }
        return Exceptions.ToArray();
    }
}