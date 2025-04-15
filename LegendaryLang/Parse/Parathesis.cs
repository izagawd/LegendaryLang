using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public static class Parenthesis
{
    public static LeftParenthesisToken  ParseLeft(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not LeftParenthesisToken parenthesisToken)
        {
            throw new ExpectedParserException(parser, (ParseType.LeftParenthesis), gotten);
        }
        return parenthesisToken;
    }
    public static void ParseRight(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not RightParenthesisToken)
        {
            throw new ExpectedParserException(parser, (ParseType.RightParenthesis), gotten);
        }
    }
}