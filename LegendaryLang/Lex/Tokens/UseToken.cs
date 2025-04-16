namespace LegendaryLang.Lex.Tokens;

public class UseToken : Token
{
    public UseToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "use";
}