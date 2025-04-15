namespace LegendaryLang.Lex.Tokens;

public class If : Token
{
    public If(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "if";
}