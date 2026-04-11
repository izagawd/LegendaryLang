using LegendaryLang.Parse;

namespace LegendaryLang.Semantics;

public class PathResolver
{
    public NormalLangPath? AddToDeepestScope(string map, NormalLangPath to)
    {
        var scope = ScopeItems.Peek();
        if (scope.TryGetValue(map, out var existing))
        {
            if (existing.Equals(to)) return null; // same path, no conflict
            return existing; // different path, conflict
        }
        scope[map] = to;
        return null;
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