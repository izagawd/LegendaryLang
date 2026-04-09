namespace LegendaryLang.Lex.Tokens;

public class ImplToken : Token
{
    public ImplToken(File file, int column, int line) : base(file, column, line) {}
    public override string Symbol => "impl";
}
