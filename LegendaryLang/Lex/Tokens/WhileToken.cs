using LegendaryLang.Parse.Expressions;

namespace LegendaryLang.Lex.Tokens;

public class WhileToken : Token
{
    public WhileToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "while";
}