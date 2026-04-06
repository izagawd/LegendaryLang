namespace LegendaryLang.Lex.Tokens;

public class LifetimeToken : Token
{
    public LifetimeToken(File file, int column, int line, string name) : base(file, column, line)
    {
        Name = name;
    }

    public override string Symbol => $"'{Name}";
    public string Name { get; }

    public override string ToString() => $"'{Name}";
}
