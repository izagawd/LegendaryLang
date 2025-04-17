namespace LegendaryLang.Lex.Tokens
{
    public class LessThanToken : Token
    {
        public LessThanToken(File file, int column, int line) : base(file, column, line)
        {
        }

        public override string Symbol => "<";
    }
}