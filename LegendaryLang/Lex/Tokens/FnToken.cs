namespace LegendaryLang.Lex.Tokens;

public class FnToken : Token
{
    public FnToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "fn";
}