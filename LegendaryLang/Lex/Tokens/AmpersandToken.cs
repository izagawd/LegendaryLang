namespace LegendaryLang.Lex.Tokens;

public class AmpersandToken : Token
{
    public AmpersandToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "&";
}