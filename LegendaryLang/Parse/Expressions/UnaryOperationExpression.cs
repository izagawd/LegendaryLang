using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class UnaryOperationExpression : IExpression
{
    public void SetFullPathOfShortCuts(SemanticAnalyzer analyzer)
    {
        Expression.SetFullPathOfShortCuts(analyzer);
    }

    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return Expression.GetAllFunctionsUsed();
    }

    public static UnaryOperationExpression Parse(Parser parser)
    {
        var token = parser.Pop();
        if (token is not IOperatorToken oper)
        {
            throw new ExpectedParserException(parser,ParseType.Operator,token);
        }
        var expr = IExpression.ParsePrimary(parser);
        return new UnaryOperationExpression(expr, oper);
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
        var gottenVal = Expression.DataRefCodeGen(codeGenContext);
        if (OperatorToken.Operator == Operator.Add)
        {
            return gottenVal;
        }

            var zero = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false);
            var loaded = gottenVal.Type.LoadValue(codeGenContext, gottenVal);
            var subbed= codeGenContext.Builder.BuildSub(zero,loaded);
            return new VariableRefItem()
            {
                Type = gottenVal.Type,
                ValueRef = subbed,
            };
        
    }

    public LangPath? TypePath => Expression.TypePath;





    public Token Token =>(OperatorToken as Token)!;
    public void Analyze(SemanticAnalyzer analyzer)
    {
        Expression.Analyze(analyzer);
        if (Expression.TypePath != new BoolTypeDefinition().TypePath && OperatorToken is  ExclamationMarkToken)
        {
            analyzer.AddException(new SemanticException($"Type '{Expression.TypePath}' is not a bool, so it cannot be used with the operator '{(OperatorToken as Token)!.Symbol}\n{Token.GetLocationStringRepresentation()}"));
        } else if (Expression.TypePath == new I32TypeDefinition().TypePath &&
                   !(OperatorToken is Plus || OperatorToken is Minus))
        {
            analyzer.AddException(new SemanticException($"Type '{Expression.TypePath}' cannot be used with the operator cannot be used with the operator '{(OperatorToken as Token)!.Symbol}' in a unary expression" +
                                                        $"\n{Token.GetLocationStringRepresentation()}"));
        }
    }
}