using LegendaryLang.Parse;

namespace LegendaryLang.Lex.Tokens;

public class OperatorToken : Token
{
    public static OperatorToken Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not OperatorToken token) throw new ExpectedParserException(parser, [ParseType.Operator], gotten);
        return token;
    }
    public OperatorToken(File file, int column, int line, Operator @operator) : base(file, column, line)
    {
        OperatorType = @operator;
    }

    public Operator OperatorType { get; }
    public override string Symbol { get; }
}