namespace LegendaryLang.Lex.Tokens
{
    public class GreaterThan : Token
    {
        public GreaterThan(File file, int column, int line) : base(file, column, line)
        {
        }

        public override string Symbol => ">";
    }
}