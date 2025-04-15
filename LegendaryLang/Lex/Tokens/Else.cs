namespace LegendaryLang.Lex.Tokens;

public class Else : Token
{
    public Else(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "else";
}