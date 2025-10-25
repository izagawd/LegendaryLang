namespace LegendaryLang.Lex.Tokens;

public class MutToken : Token
{
    public MutToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "mut";
}