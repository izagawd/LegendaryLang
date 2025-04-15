namespace LegendaryLang.Lex.Tokens;

public class RightCurlyBraceToken : Token
{
    public RightCurlyBraceToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "}";
}