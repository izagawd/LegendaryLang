namespace LegendaryLang.Lex.Tokens;

public class False : Token , IBoolToken
{
    public False(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "false";
    public bool Bool => false;
}