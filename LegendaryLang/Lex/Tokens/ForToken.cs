namespace LegendaryLang.Lex.Tokens;

public class ForToken : Token
{
    public ForToken(File file, int column, int line) : base(file, column, line) {}
    public override string Symbol => "for";
}
