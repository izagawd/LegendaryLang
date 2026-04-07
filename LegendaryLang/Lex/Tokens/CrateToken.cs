namespace LegendaryLang.Lex.Tokens;

public class CrateToken : Token
{
    public CrateToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "crate";
}
