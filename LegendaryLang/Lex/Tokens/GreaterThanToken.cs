namespace LegendaryLang.Lex.Tokens;

public class GreaterThanToken : Token
{
    public GreaterThanToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => ">";
}