using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class DoubleColon
{
    public static void Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not DoubleColonToken)
        {
            throw new ExpectedParserException(parser, (ParseType.DoubleColon), gotten);
        }
    }
}