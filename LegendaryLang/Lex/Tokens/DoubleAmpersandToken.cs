namespace LegendaryLang.Lex.Tokens;

public class DoubleAmpersandToken : Token
{
    public DoubleAmpersandToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "&&";
}