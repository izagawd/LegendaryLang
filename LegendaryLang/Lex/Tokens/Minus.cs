namespace LegendaryLang.Lex.Tokens;

public class Minus : Token , IOperatorToken
{
    public Minus(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "-";
    public Operator Operator => Operator.Subtract;
}