using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;

namespace LegendaryLang.Parse.Statements;

public interface IStatement : ISyntaxNode, IAnalyzable,  IPathResolvable, ICanHaveExplicitReturn
{
    public static IStatement Parse(Parser parser)
    {
        IStatement parsed;
        if (parser.Peek() is LetToken)
            parsed = LetStatement.Parse(parser);
        else if(parser.Peek() is ReturnToken)
            parsed = ReturnStatement.Parse(parser);
        else
        {
            parsed = IExpression.Parse(parser);
        }
        return parsed;
    }


    public void CodeGen(CodeGenContext CodeGenContext);
}