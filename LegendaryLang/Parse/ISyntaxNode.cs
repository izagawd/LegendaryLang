using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public interface ISyntaxNode
{

    public IEnumerable<NormalLangPath> GetAllFunctionsUsed();
    public  Token Token { get; }

}