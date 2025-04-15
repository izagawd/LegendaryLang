using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class UnaryOperationExpression : IExpression
{
    public static UnaryOperationExpression Parse(Parser parser)
    {
        var token = parser.Pop();
        if (token is not IOperatorToken exclamationMarkToken)
        {
            throw new ExpectedParserException(parser,ParseType.Operator,token);
        }
        var expr = IExpression.ParsePrimary(parser);
        return new UnaryOperationExpression(expr, exclamationMarkToken);
    }
    public IExpression Expression { get; }
    public IOperatorToken OperatorToken { get; }

    public UnaryOperationExpression(IExpression expression, IOperatorToken operatorToken)
    {
        Expression = expression;
        OperatorToken =operatorToken;
    }

    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        return Expression.DataRefCodeGen(codeGenContext);
    }

    public BaseLangPath? BaseLangPath { get; }


    public BaseLangPath SetTypePath(SemanticAnalyzer semanticAnalyzer)
    {
        return Expression.SetTypePath(semanticAnalyzer);
    }



    public Token LookUpToken =>(OperatorToken as Token)!;
    void ISyntaxNode.Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }
}