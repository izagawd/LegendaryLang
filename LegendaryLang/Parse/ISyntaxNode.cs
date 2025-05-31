using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public interface ISyntaxNode
{
    public IEnumerable<ISyntaxNode> Children { get; }
    public bool NeedsSemiColonAfterIfNotLastInBlock => true;


    /// <summary>
    ///     Token used to locate where the syntax node is written
    /// </summary>
    public Token Token { get; }
}