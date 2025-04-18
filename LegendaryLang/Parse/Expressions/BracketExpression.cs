using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public class BracketExpression : IExpression
{
    
    public static BracketExpression Parse(Parser parser,LeftParenthesisToken leftParenthesisToken,  IExpression expression)
    {
 
        Parenthesis.ParseRight(parser);
        return new BracketExpression(leftParenthesisToken, expression);
    }
    public IExpression Expression { get; }
    public LeftParenthesisToken LeftParenthesisToken { get; }
    public BracketExpression(LeftParenthesisToken token, IExpression expression)
    {
        LeftParenthesisToken = token;
        Expression = expression;
    }

    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return Expression.GetAllFunctionsUsed();
    }

    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        return Expression.DataRefCodeGen(codeGenContext);
    }


    public void Analyze(SemanticAnalyzer analyzer)
    {
        Expression.Analyze(analyzer);
    }

    public LangPath? TypePath => Expression.TypePath;
    public Token Token => LeftParenthesisToken;
}