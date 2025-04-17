using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class FunctionCallExpression : IExpression
{
    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return [FunctionPath];
    }

    public ImmutableArray<IExpression> Arguments { get; }
    public static FunctionCallExpression ParseFunctionCallExpression(Parser parser,
        NormalLangPath normalLangPath)
    {
        var leftParenth = Parenthesis.ParseLeft(parser);
        var currentToken = parser.Peek();
        var expressions = new List<IExpression>();
        while (currentToken is not RightParenthesisToken)
        {
            var expression = IExpression.Parse(parser);
            expressions.Add(expression);
            currentToken = parser.Peek();
            if (currentToken is CommaToken)
            {
                Comma.Parse(parser);
                currentToken = parser.Peek();
            }
        }
        Parenthesis.ParseRight(parser);
        return new FunctionCallExpression(normalLangPath, expressions);
    }

    public ImmutableArray<LangPath> GenericArguments
    {
        get
        {
            return (FunctionPath?.GetLastPathSegment() as NormalLangPath.GenericTypesPathSegment)?.TypePaths ?? [];
        }
    }

    public NormalLangPath FunctionPath { get; set; }
    public FunctionCallExpression(NormalLangPath path, IEnumerable<IExpression> arguments)
    {
        Arguments = arguments.ToImmutableArray();
        FunctionPath = path;
    }
    public Token LookUpToken { get; }
    public void Analyze(SemanticAnalyzer analyzer)
    {
        FunctionPath=(NormalLangPath) FunctionPath.GetAsShortCutIfPossible(analyzer);
        foreach (var i in Arguments)
        {
            i.Analyze(analyzer);
        }
    }

    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {


 
        var zaPath = codeGenContext.GetRefItemFor(FunctionPath) as FunctionRefItem;
        var callResult =  codeGenContext.Builder.BuildCall2(zaPath.Function.FunctionType, zaPath.Function.FunctionValueRef,
            Arguments.Select(
                i =>
                {
                    var gened =i.DataRefCodeGen(codeGenContext);
                    return gened.Type.LoadValueForRetOrArg(codeGenContext,gened);
                }).ToArray()
            );

        var returnType = zaPath.Function.ReturnType;
        LLVMValueRef stackPtr= returnType.AssignToStack(codeGenContext,new VariableRefItem()
        {
                Type = returnType,
                ValueRef = callResult
        });  

 
        return new VariableRefItem()
        {
            Type = returnType,
            ValueRef = stackPtr,
        };
    }

    public LangPath? TypePath { get; }

}