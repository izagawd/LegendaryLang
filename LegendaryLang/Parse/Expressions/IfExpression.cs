using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Statements;

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

    public ValueRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        return Body.DataRefCodeGen(codeGenContext);
    }

    /// <summary>
    ///     Else expression is a unique expression in which it doesnt directly hav4 a type path
    /// </summary>
    public LangPath? TypePath => null;

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

    private ResumeBlockPropagator? _resumeBlockPropagator { get; set; }
    public IExpression CondExpression { get; set; }
    public ElseExpression? ElseExpression { get; set; }


    public bool IsFirstInIfChain { get; set; }
    public bool IsLastIfInChain { get; set; }
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

    public unsafe ValueRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        // used to help make if/else expression return implicitly (if the else is not null of course)

        var expressionTypeRefItem = codeGenContext.GetRefItemFor(TypePath) as TypeRefItem;
        var expressionType = expressionTypeRefItem!.Type;
        ValueRefItem? possibleRefItem = null;



        var thenBB = codeGenContext.Builder.InsertBlock.Parent.AppendBasicBlock("then");
        var elseBB = ElseExpression is null
            ? default(LLVMBasicBlockRef?)
            :codeGenContext.Builder.InsertBlock.Parent.AppendBasicBlock("else");
        if (_resumeBlockPropagator is null)
        {
            _resumeBlockPropagator = new ResumeBlockPropagator();
            // passes a shared ResumeBlockPropagator down the if/else chain
            var currentIf = this;
            currentIf.IsFirstInIfChain = true;
            while (currentIf.ElseExpression?.Body is IfExpression ifExpr)
            {
                ifExpr._resumeBlockPropagator = _resumeBlockPropagator;
                currentIf = ifExpr;
            }

            currentIf.IsLastIfInChain = true;
            if (ElseExpression is not null)
            {
                LLVMValueRef? stackPtr = expressionTypeRefItem.Type.AssignToStack(codeGenContext, new ValueRefItem
                {
                    Type = expressionTypeRefItem.Type,
                    ValueRef = LLVM.GetUndef(expressionTypeRefItem.Type.TypeRef)
                });

                _resumeBlockPropagator.ImplicitReturnValue = new ValueRefItem
                {
                    Type = expressionTypeRefItem.Type,
                    ValueRef = stackPtr!.Value
                };
            }
        }

        possibleRefItem = _resumeBlockPropagator.ImplicitReturnValue;

        if (IsLastIfInChain)
            _resumeBlockPropagator.ResumeBlock = codeGenContext.Builder.InsertBlock.Parent.AppendBasicBlock("resume");

        var condCodeGen =  CondExpression.DataRefCodeGen(codeGenContext);
        var valToCompare = condCodeGen.Type.LoadValue(codeGenContext,condCodeGen);
        codeGenContext.Builder.BuildCondBr(valToCompare, thenBB, elseBB ?? _resumeBlockPropagator.ResumeBlock);


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
            codeGenContext.Builder.PositionAtEnd(elseBB!.Value);

            var codegennedElseVal = ElseExpression.DataRefCodeGen(codeGenContext);


            if (IsLastIfInChain)
            {
                expressionType.AssignTo(codeGenContext, codegennedElseVal, possibleRefItem);
                
                
                // if there is no direct return statement then we can safety branch to the resume block from the then block
                // since its not explicitly returning a value
                if (!DirectlyContainsReturnStatement(ElseExpression))
                    codeGenContext.Builder.BuildBr(_resumeBlockPropagator.ResumeBlock);
                else
                    codeGenContext.Builder.BuildRet(codegennedElseVal.LoadValue(codeGenContext));
            }
        }
        codeGenContext.Builder.PositionAtEnd(thenBB);
        var bodyVal = BodyExpression.DataRefCodeGen(codeGenContext);

        expressionType.AssignTo(codeGenContext, bodyVal, possibleRefItem);

        // if there is no direct return statement then we can safety branch to the resume block from the then block
        // since its not explicitly returning a value
        if (!DirectlyContainsReturnStatement(BodyExpression))
            codeGenContext.Builder.BuildBr(_resumeBlockPropagator.ResumeBlock);
        else
            codeGenContext.Builder.BuildRet(bodyVal.LoadValue(codeGenContext));

        if (IsFirstInIfChain) codeGenContext.Builder.PositionAtEnd(_resumeBlockPropagator.ResumeBlock);

        // we implicitly return void if the else expression is null
        if (ElseExpression is null) return codeGenContext.GetVoid();

        return possibleRefItem!;
    }


    public LangPath? TypePath { get; set; }

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
    private class ResumeBlockPropagator
    {
        /// <summary>
        ///     used to hold the stack ptr of the implicit return value the if/(possible else chain) is supposed to have
        /// </summary>
        public ValueRefItem ImplicitReturnValue { get; set; }

        public LLVMBasicBlockRef ResumeBlock { get; set; }
    }

    public bool HasGuaranteedExplicitReturn => CondExpression.HasGuaranteedExplicitReturn ||  (EndsWithoutIf && BodyExpression.HasGuaranteedExplicitReturn && ElseExpression!.HasGuaranteedExplicitReturn);
}