namespace LegendaryLang.Lex.Tokens;

public class StringLiteralToken : Token
{
    public StringLiteralToken(File file, int column, int line, string value) : base(file, column, line)
    {
        Value = value;
    }

    public override string Symbol => $"\"{Value}\"";

    /// <summary>The string content without surrounding quotes.</summary>
    public string Value { get; }
}
