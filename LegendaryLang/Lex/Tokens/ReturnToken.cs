namespace LegendaryLang.Lex.Tokens;

public class ReturnToken : Token, IStatementToken
{
    public ReturnToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "return";
}