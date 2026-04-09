namespace LegendaryLang.Lex.Tokens;

public class FatArrowToken : Token
{
    public FatArrowToken(File file, int column, int line) : base(file, column, line) {}
    public override string Symbol => "=>";
}
