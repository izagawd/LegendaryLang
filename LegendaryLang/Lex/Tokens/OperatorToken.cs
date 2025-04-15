namespace LegendaryLang.Lex.Tokens;

public class OperatorToken : Token
{
    public Operator Operator { get; }

    public OperatorToken(File file, int column, int line, Operator @operator) : base(file, column, line)
    {
        Operator = @operator;
    }

    public override string Symbol => Operator.ToSymbol();
}