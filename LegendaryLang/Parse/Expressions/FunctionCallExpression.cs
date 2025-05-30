using System.Collections.Immutable;
using LegendaryLang.Definitions;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class FunctionCallExpression : IExpression, IPathHaver
{




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
    public IEnumerable<ISyntaxNode> Children => Arguments;
    public NormalLangPath FunctionPath { get; set; }
    public FunctionCallExpression(NormalLangPath path, IEnumerable<IExpression> arguments)
    {
        Arguments = arguments.ToImmutableArray();
        FunctionPath = path;
    }

    public Token Token => FunctionPath.FirstIdentifierToken!;
    public void SetFullPathOfShortCutsDirectly(SemanticAnalyzer analyzer)
    {
        FunctionPath=(NormalLangPath) FunctionPath.GetFromShortCutIfPossible(analyzer);
    }
    public void Analyze(SemanticAnalyzer analyzer)
    {
   
        var def = analyzer.GetDefinition(FunctionPath);
        if (def is null)
        {
            def = analyzer.GetDefinition(FunctionPath.Pop());
        }
        if (def is FunctionDefinition fd)
        {

            if (fd.GenericParameters.Length != FunctionPath.GetFrontGenerics().Length)
            {
                analyzer.AddException(new SemanticException($"Incorrect number of generic parameters: {FunctionPath.GetFrontGenerics().Length}\n" +
                                                            $"Expected: {fd.GenericParameters.Length}\n\n" +
                                                            $"{Token.GetLocationStringRepresentation()}"));
                TypePath = fd.ReturnTypePath;
            }
            else
            {
                TypePath = fd.GetMonomorphizedReturnTypePath(FunctionPath);
            }
        }
        else
        {
            TypePath = LangPath.VoidBaseLangPath;
            analyzer.AddException(new SemanticException($"Cannot find function {FunctionPath}\n{Token.GetLocationStringRepresentation()}"));
        }
        foreach (var i in Arguments)
        {
            i.Analyze(analyzer);
        }
    }

    public ValueRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {


 
        var zaPath = codeGenContext.GetRefItemFor(FunctionPath) as FunctionRefItem;
        var callResult =  codeGenContext.Builder.BuildCall2(zaPath.Function.FunctionType, zaPath.Function.FunctionValueRef,
            Arguments.Select(
                i =>
                {
                    var gened =i.DataRefCodeGen(codeGenContext);
                    return gened.Type.LoadValue(codeGenContext,gened);
                }).ToArray()
            );

        var returnType = zaPath.Function.ReturnType;
        LLVMValueRef stackPtr= returnType.AssignToStack(codeGenContext,new ValueRefItem()
        {
                Type = returnType,
                ValueRef = callResult
        });  

 
        return new ValueRefItem()
        {
            Type = returnType,
            ValueRef = stackPtr,
        };
    }

    public LangPath? TypePath { get; set; }

}