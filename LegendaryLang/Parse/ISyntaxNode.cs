using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public interface ISyntaxNode
{
    public IEnumerable<ISyntaxNode> Children { get; }
    public bool NeedsSemiColonAfterIfNotLastInBlock => true;

    public void ResolvePaths(PathResolver resolver)
    {
        foreach (var i in Children)
        {
            i.ResolvePaths(resolver);
        }
    }
    /// <summary>
    ///     Token used to locate where the syntax node is written
    /// </summary>
    public Token Token { get; }
}