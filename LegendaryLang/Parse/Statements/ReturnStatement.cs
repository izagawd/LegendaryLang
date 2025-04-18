using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Statements;

public class ReturnStatement : IStatement
{
    public ReturnStatement Parse(Parser parser)
    {
        var parsed = parser.Pop();
        if (parsed is not ReturnToken returnToken)
        {
            throw new ExpectedParserException(parser, ParseType.ReturnToken,parsed);
        }

        IExpression? expression = null;
        if (parser.Peek() is not SemiColonToken)
        {
            expression = IExpression.Parse(parser);
        }
        return new ReturnStatement(returnToken, expression);
    }
    Token ISyntaxNode.Token   => Token;
    public ReturnToken Token { get; }
    public IExpression? ToReturn { get; }

    public ReturnStatement(ReturnToken token, IExpression? toReturn)
    {
        Token = token;
        ToReturn = toReturn;
    }
    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return ToReturn?.GetAllFunctionsUsed() ?? [];
    }


    public void Analyze(SemanticAnalyzer analyzer)
    {
        ToReturn?.Analyze(analyzer);
    }

    public void CodeGen(CodeGenContext CodeGenContext)
    {
        throw new NotImplementedException();
    }
}