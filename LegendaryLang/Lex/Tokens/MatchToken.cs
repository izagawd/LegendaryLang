namespace LegendaryLang.Lex.Tokens;

public class MatchToken : Token
{
    public MatchToken(File file, int column, int line) : base(file, column, line) {}
    public override string Symbol => "match";
}
