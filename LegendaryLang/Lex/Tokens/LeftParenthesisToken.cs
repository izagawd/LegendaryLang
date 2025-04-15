namespace LegendaryLang.Lex.Tokens
{
    public class LeftParenthesisToken : Token
    {
        public LeftParenthesisToken(File file, int column, int line) : base(file, column, line)
        {
        }

        public override string Symbol => "(";
    }
}