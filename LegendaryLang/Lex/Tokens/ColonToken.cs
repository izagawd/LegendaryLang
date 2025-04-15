namespace LegendaryLang.Lex.Tokens;

public class ColonToken : Token
{
    public ColonToken(File file, int column, int line) : base(file, column, line)
    {
        
    }

    public override string Symbol => ":";
}