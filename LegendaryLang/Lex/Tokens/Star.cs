namespace LegendaryLang.Lex.Tokens;

public class Star : Token, IOperatorToken
{
    public Star(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "*";
    public Operator Operator => Operator.Multiply;
}