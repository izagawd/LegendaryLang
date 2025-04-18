using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Statements;

public class ReturnStatement : IStatement
{
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
        throw new NotImplementedException();
    }


    public void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }

    public void CodeGen(CodeGenContext CodeGenContext)
    {
        throw new NotImplementedException();
    }
}