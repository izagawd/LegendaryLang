namespace LegendaryLang.Lex.Tokens;

public class LeftCurlyBraceToken : Token
{
    public LeftCurlyBraceToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "{";
}