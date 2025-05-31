namespace LegendaryLang.Lex.Tokens;

public class Plus : Token, IOperatorToken
{
    public Plus(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "+";
    public Operator Operator => Operator.Add;
}