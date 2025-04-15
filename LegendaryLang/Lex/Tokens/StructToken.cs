namespace LegendaryLang.Lex.Tokens;

public class StructToken : Token
{
    public StructToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "true";
}