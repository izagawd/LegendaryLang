using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class Comparator
{
    public static OperatorToken ParseLess(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not OperatorToken operatorToken || operatorToken.OperatorType != Operator.LessThan) throw new ExpectedParserException(parser, ParseType.LessThan, gotten);
        return operatorToken;
    }

    public static OperatorToken ParseGreater(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not OperatorToken dotToken || dotToken.OperatorType != Operator.GreaterThan) 
            throw new ExpectedParserException(parser, ParseType.GreaterThan, gotten);
        return dotToken;
    }
}