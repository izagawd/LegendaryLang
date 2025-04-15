namespace LegendaryLang.Lex.Tokens;

public abstract class Token
{
    public int Column {get; }
    public int Line { get; }
    public File File { get; }
    public abstract string Symbol { get; }
    
    public Token(File file, int column, int line)
    {
        Column = column;
        Line = line;
        File = file;
    }
}