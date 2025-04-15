using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class Equality
{
    public static void Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not EqualityToken)
        {
            throw new ExpectedParserException(parser, (ParseType.Equality), gotten);
        }
    }
}