namespace LegendaryLang.Lex.Tokens
{
    public class RightParenthesisToken : Token
    {
        public RightParenthesisToken(File file, int column, int line) : base(file, column, line)
        {
        }

        public override string Symbol => ")";
    }
}