using LegendaryLang.Parse;

namespace LegendaryLang.Semantics;

public class PathResolver
{
    public void AddToDeepestScope(string map, NormalLangPath to)
    {
        ScopeItems.Peek().Add(map, to);
    }
    private readonly Stack<Dictionary<string, NormalLangPath>> ScopeItems = new();
    public NormalLangPath? GetFullPathOfShortcut(string shortcut)
    {
        foreach (var scope in ScopeItems)
            if (scope.TryGetValue(shortcut, out var symbol))
                return symbol;

        return null;
    }

    public void AddScope()
    {
        ScopeItems.Push(new Dictionary<string, NormalLangPath>());
    }

    public void PopScope()
    {
        ScopeItems.Pop();
    }

}