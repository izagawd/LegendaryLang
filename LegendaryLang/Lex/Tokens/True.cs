namespace LegendaryLang.Lex.Tokens;

public class True : Token, IBoolToken
{
    public True(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "true";
    public bool Bool => true;
}