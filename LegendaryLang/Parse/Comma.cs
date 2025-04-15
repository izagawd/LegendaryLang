using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class Comma
{
    public static void Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not CommaToken)
        {
            throw new ExpectedParserException(parser, (ParseType.Comma), gotten);
        }
    }
}