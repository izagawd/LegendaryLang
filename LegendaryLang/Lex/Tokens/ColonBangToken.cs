namespace LegendaryLang.Lex.Tokens;

public class ColonBangToken : Token
{
    public ColonBangToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => ":!";
}
