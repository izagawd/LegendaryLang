using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public interface IPathResolvable : ISyntaxNode
{
    public void ResolvePaths(PathResolver resolver)
    {
        foreach (var i in Children.OfType<IPathResolvable>())
        {
            i.ResolvePaths(resolver);
        }
    }
}