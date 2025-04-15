namespace LegendaryLang.Lex.Tokens
{
    public class SemiColonToken : Token
    {
        public SemiColonToken(File file, int column, int line) : base(file, column, line)
        {
        }

        public override string Symbol => ";";
    }
}