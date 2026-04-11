namespace LegendaryLang.Lex.Tokens;

public class RawToken : Token
{
    public RawToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "raw";
}
