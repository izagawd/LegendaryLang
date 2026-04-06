using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public static class Bracket
{
    public static LeftBracketToken ParseLeft(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not LeftBracketToken bracketToken)
            throw new ExpectedParserException(parser, ParseType.LeftBracket, gotten);
        return bracketToken;
    }

    public static void ParseRight(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not RightBracketToken)
            throw new ExpectedParserException(parser, ParseType.RightBracket, gotten);
    }
}
