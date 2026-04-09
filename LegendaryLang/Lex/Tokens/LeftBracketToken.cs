namespace LegendaryLang.Lex.Tokens;

public class LeftBracketToken : Token
{
    public LeftBracketToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "[";
}
