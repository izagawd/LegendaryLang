namespace LegendaryLang.Lex.Tokens;

public class TraitToken : Token
{
    public TraitToken(File file, int column, int line) : base(file, column, line) {}
    public override string Symbol => "trait";
}
