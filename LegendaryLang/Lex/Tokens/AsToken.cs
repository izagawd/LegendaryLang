namespace LegendaryLang.Lex.Tokens;

public class AsToken : Token
{
    public AsToken(File file, int column, int line) : base(file, column, line) {}
    public override string Symbol => "as";
}
