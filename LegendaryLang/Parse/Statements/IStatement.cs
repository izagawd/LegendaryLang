using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse.Statements;

public interface IStatement : ISyntaxNode, IAnalyzable,  IPathResolvable
{
    public static IStatement Parse(Parser parser)
    {
        IStatement parsed;
        if (parser.Peek() is LetToken)
            parsed = LetStatement.Parse(parser);
        else
            parsed = ReturnStatement.Parse(parser);

        SemiColon.Parse(parser);
        return parsed;
    }


    public void CodeGen(CodeGenContext CodeGenContext);
}