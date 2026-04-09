using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Statements;
public class ResumeBlockPropagator
{
    /// <summary>
    ///     used to hold the stack ptr of the implicit return value the if/(possible else chain) is supposed to have
    /// </summary>
    public ValueRefItem ImplicitReturnValue { get; set; }

    public LLVMBasicBlockRef ResumeBlock { get; set; }
}
public class ElseExpression : IExpression
{
    public ElseExpression(Token token, IExpression body)
    {
        Body = body;
        Token = token;
    }

    public IExpression Body { get; }
    public IEnumerable<ISyntaxNode> Children => [Body];

    public bool NeedsSemiColonAfterIfNotLastInBlock => Body.NeedsSemiColonAfterIfNotLastInBlock;

    public Token Token { get; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        Body.Analyze(analyzer);
    }

    public ValueRefItem CodeGen(CodeGenContext codeGenContext, ResumeBlockPropagator? propagator)
    {
        if (propagator is not null && Body is IfExpression ifExpr)
        {
           return ifExpr.CodeGen(codeGenContext, propagator);
        }
    
        return Body.CodeGen(codeGenContext);
        
    }
    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        return CodeGen(codeGenContext, null);
    }

    /// <summary>
    ///     Else expression is a unique expression in which it doesnt directly hav4 a type path
    /// </summary>
    public LangPath? TypePath => null;
    public bool IsTemporary => true; // branch result is fresh

    public bool HasGuaranteedExplicitReturn => Body.HasGuaranteedExplicitReturn;
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
    
    public bool EndsWithoutIf
    {
        get
        {
            if (ElseExpression is not null)
            {
                if (ElseExpression.Body is IfExpression ifExpression) return ifExpression.EndsWithoutIf;

                return true;
            }

            return false;
        }
    }


    public IExpression CondExpression { get; set; }
    public ElseExpression? ElseExpression { get; set; }


    public BlockExpression BodyExpression { get; set; }

    public bool NeedsSemiColonAfterIfNotLastInBlock => BodyExpression.NeedsSemiColonAfterIfNotLastInBlock;

    public IEnumerable<ISyntaxNode> Children
    {
        get
        {
            yield return CondExpression;
            yield return BodyExpression;
            if (ElseExpression is not null) yield return ElseExpression;
        }
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
            if (ElseExpression.Body.TypePath != BodyExpression.TypePath)
            {
                TypePath = LangPath.VoidBaseLangPath;
                analyzer.AddException(new SemanticException(
                    $"if and else blocks do not return the same type\n{Token.GetLocationStringRepresentation()}"));
            }
            else
            {
                TypePath = BodyExpression.TypePath;
            }
        }
    }

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        return CodeGen(codeGenContext,null);
    }
    public unsafe ValueRefItem CodeGen(CodeGenContext codeGenContext, ResumeBlockPropagator? resumeBlockPropagator)
    {
        // used to help make if/else expression return implicitly (if the else is not null of course)

        var expressionTypeRefItem = codeGenContext.GetRefItemFor(TypePath) as TypeRefItem;
        var expressionType = expressionTypeRefItem!.Type;
        ValueRefItem? possibleRefItem = null;

        
        var thenBB = codeGenContext.Builder.InsertBlock.Parent.AppendBasicBlock("then");
        var elseBB = ElseExpression is null
            ? default(LLVMBasicBlockRef?)
            :codeGenContext.Builder.InsertBlock.Parent.AppendBasicBlock("else");
        bool isFirstInIfChain = false;
        if (resumeBlockPropagator is null)
        {
            isFirstInIfChain = true;
            resumeBlockPropagator = new ResumeBlockPropagator();

            if (ElseExpression is not null)
            {
                LLVMValueRef? stackPtr = expressionTypeRefItem.Type.AssignToStack(codeGenContext, new ValueRefItem
                {
                    Type = expressionTypeRefItem.Type,
                    ValueRef = LLVM.GetUndef(expressionTypeRefItem.Type.TypeRef)
                });

                resumeBlockPropagator.ImplicitReturnValue = new ValueRefItem
                {
                    Type = expressionTypeRefItem.Type,
                    ValueRef = stackPtr!.Value
                };
            }
        }

        possibleRefItem = resumeBlockPropagator.ImplicitReturnValue;
        var isLastIfInChain = ElseExpression?.Body is not IfExpression;
        if (isLastIfInChain)
            resumeBlockPropagator.ResumeBlock = codeGenContext.Builder.InsertBlock.Parent.AppendBasicBlock("resume");

        var condCodeGen =  CondExpression.CodeGen(codeGenContext);
        var valToCompare = condCodeGen.Type.LoadValue(codeGenContext,condCodeGen);
        codeGenContext.Builder.BuildCondBr(valToCompare, thenBB, elseBB ?? resumeBlockPropagator.ResumeBlock);


        ReturnStatement? DirectReturnStatement(ISyntaxNode syntaxNode)
        {
            if (syntaxNode is ReturnStatement returnStatement) return returnStatement;
         
            foreach (var child in syntaxNode.Children.Where(i => i is not IItem))
                if (DirectReturnStatement(child) is not null)
                    return DirectReturnStatement(child);

            return null;
        }

        bool DirectlyContainsReturnStatement(ISyntaxNode syntaxNode)
        {
            return DirectReturnStatement(syntaxNode) is not null;
        }

        codeGenContext.Builder.PositionAtEnd(thenBB);


        if (ElseExpression is not null)
        {
            
            codeGenContext.Builder.PositionAtEnd(elseBB.Value);

            var codegennedElseVal = ElseExpression.CodeGen(codeGenContext, resumeBlockPropagator);


            if (isLastIfInChain)
            {
                expressionType.AssignTo(codeGenContext, codegennedElseVal, possibleRefItem);
                
                
                // if there is no direct return statement then we can safety branch to the resume block from the then block
                // since its not explicitly returning a value
                if (!DirectlyContainsReturnStatement(ElseExpression))
                    codeGenContext.Builder.BuildBr(resumeBlockPropagator.ResumeBlock);
                else
                    codeGenContext.Builder.BuildRet(codegennedElseVal.LoadValue(codeGenContext));
            }
        }
        codeGenContext.Builder.PositionAtEnd(thenBB);
        var bodyVal = BodyExpression.CodeGen(codeGenContext);

        expressionType.AssignTo(codeGenContext, bodyVal, possibleRefItem);

        // if there is no direct return statement then we can safety branch to the resume block from the then block
        // since its not explicitly returning a value
        if (!DirectlyContainsReturnStatement(BodyExpression))
            codeGenContext.Builder.BuildBr(resumeBlockPropagator.ResumeBlock);
        else
            codeGenContext.Builder.BuildRet(bodyVal.LoadValue(codeGenContext));

        if (isFirstInIfChain) codeGenContext.Builder.PositionAtEnd(resumeBlockPropagator.ResumeBlock);

        // we implicitly return void if the else expression is null
        if (ElseExpression is null) return codeGenContext.GetVoid();
        
        // MUST be set to null when done
        return possibleRefItem!;
    }


    public LangPath? TypePath { get; set; }
    public bool IsTemporary => true; // branch result is fresh

    public static IfExpression Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not IfToken ifToken) throw new ExpectedParserException(parser, [ParseType.If], gotten);
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
                var elseBody = IExpression.Parse(parser);
                elseExpression = new ElseExpression(elseToken, elseBody);
            }
            else
            {
                throw new ExpectedParserException(parser, [ParseType.If, ParseType.LeftCurlyBrace], gotten);
            }
        }

        return new IfExpression(ifToken, condition, toExecute, elseExpression);
    }

    /// <summary>
    ///     Used to keep track of the resume block, to be jumped to after a series of if/else chains (or even a single if) have
    ///     been code genned
    /// </summary>


    public bool HasGuaranteedExplicitReturn => CondExpression.HasGuaranteedExplicitReturn ||  (EndsWithoutIf && BodyExpression.HasGuaranteedExplicitReturn && ElseExpression!.HasGuaranteedExplicitReturn);
}