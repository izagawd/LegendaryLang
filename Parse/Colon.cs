using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class Colon
{
    public static void Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not ColonToken) throw new ExpectedParserException(parser, ParseType.Colon, gotten);
    }
}