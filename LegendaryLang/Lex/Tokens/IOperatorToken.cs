using LegendaryLang.Parse;

namespace LegendaryLang.Lex.Tokens;

public interface IOperatorToken
{
    public Operator Operator { get; }

    public static IOperatorToken Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not IOperatorToken token) throw new ExpectedParserException(parser, [ParseType.Operator], gotten);
        return token;
    }
}