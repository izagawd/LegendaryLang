using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class DotParser
{
    public static DotToken Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not DotToken dotToken) throw new ExpectedParserException(parser, ParseType.Dot, gotten);
        return dotToken;
    }
}