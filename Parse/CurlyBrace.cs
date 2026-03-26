using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class CurlyBrace
{
    public static LeftCurlyBraceToken ParseLeft(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not LeftCurlyBraceToken leftCurlyBraceToken)
            throw new ExpectedParserException(parser, ParseType.LeftCurlyBrace, gotten);
        return leftCurlyBraceToken;
    }

    public static RightCurlyBraceToken Parseight(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not RightCurlyBraceToken rightCurlyBraceToken)
            throw new ExpectedParserException(parser, ParseType.RightCurlyBrace, gotten);
        return rightCurlyBraceToken;
    }
}