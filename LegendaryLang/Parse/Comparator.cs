using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class Comparator
{
    public static LessThanToken ParseLess(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not LessThanToken dotToken) throw new ExpectedParserException(parser, ParseType.LessThan, gotten);
        return dotToken;
    }

    public static GreaterThanToken ParseGreater(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not GreaterThanToken dotToken)
            throw new ExpectedParserException(parser, ParseType.GreaterThan, gotten);
        return dotToken;
    }
}