namespace LegendaryLang.Lex.Tokens
{
    public class LessThan : Token
    {
        public LessThan(File file, int column, int line) : base(file, column, line)
        {
        }

        public override string Symbol => "<";
    }
}