namespace LegendaryLang.Lex.Tokens;

public class DotToken : Token
{
    public DotToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => ".";
}