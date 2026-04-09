namespace LegendaryLang.Lex.Tokens;

public class EnumToken : Token
{
    public EnumToken(File file, int column, int line) : base(file, column, line) {}
    public override string Symbol => "enum";
}
