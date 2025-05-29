using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Statements;

public class ElseExpression : IExpression
{
    public IExpression Body {get; set;}
    public void SetFullPathOfShortCuts(SemanticAnalyzer analyzer)
    {
        Body.SetFullPathOfShortCuts(analyzer);
    }

    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return Body.GetAllFunctionsUsed();
    }

    public ElseExpression(Token token, IExpression body)
    {
        Body = body;
        Token = token;
    }

    public Token Token { get; }
    public void Analyze(SemanticAnalyzer analyzer)
    {
        Body.Analyze(analyzer);
    }

    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        return Body.DataRefCodeGen(codeGenContext); 
    }

    public LangPath? TypePath => Body.TypePath;
}
public class IfExpression : IExpression
{
    public IfExpression(IfToken ifToken, IExpression conditionExpression, BlockExpression body,
        ElseExpression? elseExpression)
    {
        Token = ifToken;
        CondExpression = conditionExpression;
        BodyExpression = body;
        ElseExpression = elseExpression;
    }
    public static IfExpression Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not IfToken ifToken)
        {
            throw new ExpectedParserException(parser,[ParseType.If],gotten);
        }
        var condition = IExpression.Parse(parser);
        var toExecute = BlockExpression.Parse(parser);
        var next = parser.Peek();
        ElseExpression? elseExpression = null;
        if (next is Else elseToken)
        {
            parser.Pop();
            next = parser.Peek();
         
            if (next is LeftCurlyBraceToken or IfToken)
            {
                IExpression elseBody = IExpression.Parse(parser);
                elseExpression = new ElseExpression(elseToken, elseBody);
            }
            else
            {
                throw new ExpectedParserException(parser,[ParseType.If, ParseType.LeftCurlyBrace],gotten);
            }
        }
        return new IfExpression(ifToken, condition, toExecute, elseExpression);
    }
    public IExpression CondExpression {get; set;}
    public ElseExpression? ElseExpression {get; set;}

    public void SetFullPathOfShortCuts(SemanticAnalyzer analyzer)
    {
        CondExpression.SetFullPathOfShortCuts(analyzer);
        ElseExpression?.SetFullPathOfShortCuts(analyzer);
    }
    public BlockExpression BodyExpression {get; set;}
    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
       return CondExpression.GetAllFunctionsUsed().Union(ElseExpression?.GetAllFunctionsUsed() ??[]);
    }

    public Token Token { get; }
    public void Analyze(SemanticAnalyzer analyzer)
    {
        CondExpression.Analyze(analyzer);
        BodyExpression.Analyze(analyzer);
        ElseExpression?.Analyze(analyzer);
        if (ElseExpression is null)
        {
            TypePath = LangPath.VoidBaseLangPath;
        }
        else
        {
            
            if (ElseExpression.TypePath != BodyExpression.TypePath)
            {
                analyzer.AddException(new SemanticException($"if and else blocks do not return the same type\n{Token.GetLocationStringRepresentation()}"));
            }
            else
            {
                TypePath = BodyExpression.TypePath;
            }
        }
    }

    public unsafe VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
       
        var thenBB  =     codeGenContext.Module.LastFunction.AppendBasicBlock("then");
        LLVMBasicBlockRef? elseBB  = ElseExpression is  null ?  default(LLVMBasicBlockRef?) :  codeGenContext.Module.LastFunction.AppendBasicBlock("then");
        var resume = codeGenContext.Module.LastFunction.AppendBasicBlock("resume");
        var condCodeGen = CondExpression.DataRefCodeGen(codeGenContext);
        codeGenContext.Builder.BuildCondBr(condCodeGen.ValueRef,thenBB,elseBB ?? resume);
        codeGenContext.Builder.PositionAtEnd(thenBB);
        BodyExpression.DataRefCodeGen(codeGenContext);
        codeGenContext.Builder.BuildBr(resume);
        if (ElseExpression is not null)
        {
            codeGenContext.Builder.PositionAtEnd(elseBB!.Value);
            ElseExpression.DataRefCodeGen(codeGenContext);
            codeGenContext.Builder.BuildBr(resume);
        }
        codeGenContext.Builder.PositionAtEnd(resume);
        return null;

    }

    public LangPath? TypePath { get; set; }
}