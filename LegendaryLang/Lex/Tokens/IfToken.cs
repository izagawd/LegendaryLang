namespace LegendaryLang.Lex.Tokens;

public class IfToken : Token
{
    public IfToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "if";
}