namespace LegendaryLang.Lex.Tokens;

public class MakeToken : Token
{
    public MakeToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "make";
}
