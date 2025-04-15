using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class SemiColon
{
    public static void Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not SemiColonToken)
        {
            throw new ExpectedParserException(parser, (ParseType.SemiColon), gotten);
        }
    }
}