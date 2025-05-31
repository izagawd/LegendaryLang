namespace LegendaryLang.Lex.Tokens;

public class LetToken : Token, IStatementToken
{
    public LetToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "let";

    public string GetLineOfCode()
    {
        return File.GetLine(Line);
    }
}