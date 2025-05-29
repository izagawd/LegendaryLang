using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Statements;

public class ElseExpression : IExpression
{
    public IEnumerable<ISyntaxNode> Children => [Body];
    public IExpression Body {get; set;}

    public bool NeedsSemiColonAfterIfNotLastInBlock => Body.NeedsSemiColonAfterIfNotLastInBlock;
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

    /// <summary>
    /// Else expression is a unique expression in which it doesnt directly hav4 a type path
    /// </summary>
    public LangPath? TypePath => null;
}
public class IfExpression : IExpression
{
    public bool EndsWithoutIf
    {
        get
        {
 
            if (ElseExpression is not null)
            {
                if (ElseExpression.Body is IfExpression ifExpression)
                {
                    return ifExpression.EndsWithoutIf;
                }

                return true;
            }
            return false;
        }
    }

    public bool NeedsSemiColonAfterIfNotLastInBlock => BodyExpression.NeedsSemiColonAfterIfNotLastInBlock;
    public IEnumerable<ISyntaxNode> Children
    {
        get
        {
            yield return CondExpression;
            yield return BodyExpression;
            if (ElseExpression is not null)
            {
                yield return ElseExpression;
            }
        }
    }

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
        var toExecute = BlockExpression.Parse(parser, null);
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


    public BlockExpression BodyExpression {get; set;}

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
            if (ElseExpression.Body.TypePath != BodyExpression.TypePath)
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
        LLVMValueRef? stackPtr = null;

        var expressionTypeRefItem = codeGenContext.GetRefItemFor(TypePath) as TypeRefItem;
        var expressionType = expressionTypeRefItem.Type;
        VariableRefItem? possibleRefItem= null;
        if (ElseExpression is not null)
        {

        
            stackPtr= expressionTypeRefItem.Type.AssignToStack(codeGenContext,new VariableRefItem()
            {
                Type = expressionTypeRefItem.Type,
                ValueRef = LLVM.GetUndef(expressionTypeRefItem.Type.TypeRef)
            });
            possibleRefItem= new VariableRefItem()
            {
                Type = expressionTypeRefItem.Type,
                ValueRef = stackPtr!.Value,
            };
        }
        
        var thenBB  =     codeGenContext.Module.LastFunction.AppendBasicBlock("then");
        LLVMBasicBlockRef? elseBB  = ElseExpression is  null ?  default(LLVMBasicBlockRef?) :  codeGenContext.Module.LastFunction.AppendBasicBlock("else");
        var resume = codeGenContext.Module.LastFunction.AppendBasicBlock("resume");
        var condCodeGen = CondExpression.DataRefCodeGen(codeGenContext);
        codeGenContext.Builder.BuildCondBr(condCodeGen.ValueRef,thenBB,elseBB ?? resume);
        codeGenContext.Builder.PositionAtEnd(thenBB);


        bool DirectlyContainsReturnStatement(ISyntaxNode syntaxNode)
        {
            if (syntaxNode is ReturnStatement returnStatement)
            {
                return true;
            }

            foreach (var child in syntaxNode.Children.Where(i => i is not IfExpression))
            {
                if (DirectlyContainsReturnStatement(child))
                {
                    return true;
                }
            }
            return false;
        }
        var bodyVal = BodyExpression.DataRefCodeGen(codeGenContext);
        expressionType.AssignTo(codeGenContext, bodyVal, possibleRefItem);
        if (!DirectlyContainsReturnStatement(BodyExpression))
        {
            codeGenContext.Builder.BuildBr(resume);
        }
        else
        {
            codeGenContext.Builder.BuildRet(bodyVal.LoadValForRetOrArg(codeGenContext));   
        }
        
        if (ElseExpression is not null)
        {
            codeGenContext.Builder.PositionAtEnd(elseBB!.Value);
            var codegennedElseVal =ElseExpression.DataRefCodeGen(codeGenContext);
            expressionType.AssignTo(codeGenContext, codegennedElseVal, possibleRefItem);
            if (!DirectlyContainsReturnStatement(ElseExpression.Body))
            {
                codeGenContext.Builder.BuildBr(resume);
            }
            else
            {
                codeGenContext.Builder.BuildRet(codegennedElseVal.LoadValForRetOrArg(codeGenContext));   
            }
        }
        codeGenContext.Builder.PositionAtEnd(resume);

        if (ElseExpression is null)
        {
            return codeGenContext.GetVoid();
        }

        return possibleRefItem!;


    }

    /// <summary>
    /// This can be null, since block expressions can be null
    /// </summary>
    public LangPath? TypePath { get; set; }
}