namespace LegendaryLang.Lex.Tokens;

public class DoubleColonToken : Token
{
    public DoubleColonToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "::";
}