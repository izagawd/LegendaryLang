namespace LegendaryLang.Lex.Tokens;

public class LetToken : Token, IIsStatementToken
{
    public LetToken(File file,int column, int line) : base(file,column, line)
    {
    }

    public string GetLineOfCode()
    {
        return File.GetLine(Line);
    }
    public override string Symbol => "let";
}