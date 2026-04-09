namespace LegendaryLang.Lex.Tokens;

public class RightBracketToken : Token
{
    public RightBracketToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "]";
}
