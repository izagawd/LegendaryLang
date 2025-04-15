namespace LegendaryLang.Lex.Tokens;

public class EqualityToken : Token
{
    public EqualityToken(File file, int column, int  line) : base(file,column, line)
    {
    }

    public override string Symbol => "=";
}