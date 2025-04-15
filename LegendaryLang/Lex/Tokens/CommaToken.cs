namespace LegendaryLang.Lex.Tokens;

public class CommaToken : Token
{
    public CommaToken(File file, int column, int line) : base(file, column, line)
    {
        
    }

    public override string Symbol => ",";
}