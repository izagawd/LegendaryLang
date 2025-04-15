namespace LegendaryLang.Lex.Tokens;

public class ForwardSlash : Token, IOperatorToken
{
    public ForwardSlash(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol  => "/";
    public Operator Operator => Operator.Divide;
}