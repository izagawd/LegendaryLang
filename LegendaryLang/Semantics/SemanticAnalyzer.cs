using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
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
    private readonly Stack<Dictionary<LangPath, IDefinition>> DefinitionsStackMap = [];
    private readonly List<SemanticException> Exceptions = [];

    
    private readonly Stack<Dictionary<LangPath, LangPath>> VariableToTypeMapper = new();

    /// <summary>
    /// Tracks trait bounds for generic parameters currently in scope.
    /// Maps generic param name -> (traitPath, genericParamName)
    /// </summary>
    private readonly Stack<List<(LangPath traitPath, string genericParamName)>> TraitBoundsStack = new();

    /// <summary>
    /// All impl definitions for trait method validation
    /// </summary>
    public List<ImplDefinition> ImplDefinitions { get; } = new();

    public Stack<ParseResult> ParseResults = new();

    public SemanticAnalyzer(IEnumerable<ParseResult> parseResults)
    {
        ParseResults = new Stack<ParseResult>(parseResults);
    }

    public void PushTraitBounds(IEnumerable<(LangPath traitPath, string genericParamName)> bounds)
    {
        TraitBoundsStack.Push(bounds.ToList());
    }

    public void PopTraitBounds()
    {
        if (TraitBoundsStack.Count > 0)
            TraitBoundsStack.Pop();
    }

    /// <summary>
    /// Gets the trait definition associated with a trait bound for a given generic param name
    /// </summary>
    public TraitDefinition? GetTraitBoundFor(string genericParamName)
    {
        foreach (var bounds in TraitBoundsStack)
            foreach (var (traitPath, paramName) in bounds)
                if (paramName == genericParamName)
                    return GetDefinition(traitPath) as TraitDefinition;
        return null;
    }

    /// <summary>
    /// Checks if a path resolves to a trait method call (TraitName::method)
    /// and returns the method's return type path
    /// </summary>
    public LangPath? ResolveTraitMethodReturnType(NormalLangPath path)
    {
        var methodName = path.GetLastPathSegment().ToString();
        var traitPath = path.Pop();
        if (traitPath == null) return null;

        var traitDef = GetDefinition(traitPath) as TraitDefinition;
        if (traitDef == null) return null;

        var method = traitDef.GetMethod(methodName);
        if (method == null) return null;

        var returnType = method.ReturnTypePath;

        // If the return type is "Self", substitute it with the generic parameter
        // that has this trait as its bound
        if (returnType is NormalLangPath nlp && nlp.PathSegments.Length == 1
            && nlp.PathSegments[0].ToString() == "Self")
        {
            foreach (var bounds in TraitBoundsStack)
                foreach (var (tp, paramName) in bounds)
                    if (tp == traitPath)
                        return new NormalLangPath(null, [paramName]);
        }

        return returnType;
    }

    public void AddException(SemanticException exception)
    {
        Exceptions.Add(exception);
    }


    public IDefinition? GetDefinition(LangPath langPath)
    {
        foreach (var def in DefinitionsStackMap)
        {
            if (def.TryGetValue(langPath, out var definition)) return definition;
        }
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

    public void RegisterDefinitionAtDeepestScope(LangPath path, IDefinition definition)
    {
        DefinitionsStackMap.Peek()[path] = definition;

    }

    public void AddScope()
    {

        DefinitionsStackMap.Push(new ());
        VariableToTypeMapper.Push(new Dictionary<LangPath, LangPath>());
    }

    


    public void PopScope()
    {   
        DefinitionsStackMap.Pop();
     
        VariableToTypeMapper.Pop();
    }

    private void ResolvePaths()
    {
        var pathShortcutContext = new PathResolver();
        
        pathShortcutContext.AddScope();
        var primitiveParsed = ParseResults.First(i => i.Items.Any(j => j is I32TypeDefinition));
        foreach (var i in primitiveParsed.Items.OfType<PrimitiveTypeDefinition>())
        {
            var usings = new UseDefinition((NormalLangPath)i.TypePath, null);
            usings.RegisterUsings(pathShortcutContext);
        }
        foreach (var result in ParseResults)
        {
            pathShortcutContext.AddScope();
            foreach (var i in result.Items.OfType<IDefinition>())
            {
                var usings = new UseDefinition((NormalLangPath) i.TypePath, null);
                usings.RegisterUsings(pathShortcutContext);
            }
            foreach (var useDefinition in result.Items.OfType<UseDefinition>())
            {
                useDefinition.RegisterUsings(pathShortcutContext);
            }
            foreach (var i in result.Items.OfType<IPathResolvable>())
            {
                i.ResolvePaths(pathShortcutContext);
            }
            pathShortcutContext.PopScope();
        }
        pathShortcutContext.PopScope();
    }
    /// <returns>Collection of semantic errors that occured</returns>
    public SemanticException[] Analyze()
    {
        AddScope();
        // registers path mapping
        foreach (var i in ParseResults.SelectMany(i => i.Items.OfType<IDefinition>()))
            RegisterDefinitionAtDeepestScope(i.TypePath, i);

        // Collect impl definitions
        foreach (var impl in ParseResults.SelectMany(i => i.Items.OfType<ImplDefinition>()))
        {
            ImplDefinitions.Add(impl);
            // Register each impl method as a definition so it can be found during codegen
            foreach (var method in impl.Methods)
                RegisterDefinitionAtDeepestScope(method.TypePath, method);
        }

        ResolvePaths();

        foreach (var result in ParseResults)
        {
            AddScope();
      
            foreach (var i in result.Items.OfType<IAnalyzable>())
            {
                i.Analyze(this);
            }
            PopScope();
        }
        PopScope();

        return Exceptions.ToArray();
    }
}