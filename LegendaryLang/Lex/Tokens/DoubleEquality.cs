namespace LegendaryLang.Lex.Tokens;

public class DoubleEquality : Token
{
    public DoubleEquality(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "==";
}