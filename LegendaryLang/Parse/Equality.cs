using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class Equality
{
    public static EqualityToken Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not EqualityToken token)
        {
            throw new ExpectedParserException(parser, (ParseType.Equality), gotten);
        }
        return token;
    }
}