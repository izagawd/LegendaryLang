namespace LegendaryLang.Lex.Tokens;

public class RightPointToken : Token
{
    public RightPointToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "->";
}