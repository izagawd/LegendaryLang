namespace LegendaryLang.Lex.Tokens;

public class NumberToken : Token
{
    public NumberToken(File file, int column, int line, string number) : base(file, column, line)
    {
        Number = number;
    }

    public override string Symbol => Number;
    public string Number { get; }
}