using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class WhileExpression : IExpression
{
    public WhileToken WhileToken { get; }
    public IExpression Condition { get; }
    public BlockExpression ToExecute { get; }

    
    public static WhileExpression Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not WhileToken whileToken) throw new ExpectedParserException(parser, [ParseType.While], gotten);
        var condition = IExpression.Parse(parser);
        var toExecute = BlockExpression.Parse(parser, null);
        var next = parser.Peek();

        return new WhileExpression(whileToken, condition, toExecute);
    }

    public WhileExpression(WhileToken whileToken, IExpression condition, BlockExpression toExecute)
    {
        WhileToken = whileToken;
        Condition = condition;
        ToExecute = toExecute;
    }
    public IEnumerable<ISyntaxNode> Children => [Condition,ToExecute];
    public Token Token { get; }
    public bool NeedsSemiColonAfterIfNotLastInBlock => false;

    public void Analyze(SemanticAnalyzer analyzer)
    {
        Condition.Analyze(analyzer);
        ToExecute.Analyze(analyzer);
        if (ToExecute.TypePath != LangPath.VoidBaseLangPath)
        {
            analyzer.AddException(new SemanticException($"Block of whiles should be void, found {ToExecute.TypePath}\n{ToExecute.Token.GetLocationStringRepresentation()}"));
        }

        if (Condition.TypePath != new BoolTypeDefinition().TypePath)
        {
            analyzer.AddException(new SemanticException($"Conditions of whiles should be bool, found {Condition.TypePath}\n{ToExecute.Token.GetLocationStringRepresentation()}"));
        }
    }

    public LangPath TypePath => LangPath.VoidBaseLangPath;
    public ValueRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        var loop = codeGenContext.Builder.InsertBlock.Parent.AppendBasicBlock("loop");
        var resume = codeGenContext.Builder.InsertBlock.Parent.AppendBasicBlock("resume");
        var iffed = Condition.DataRefCodeGen(codeGenContext);
        var valPtr = iffed.Type.LoadValue(codeGenContext,iffed);
        codeGenContext.Builder.BuildCondBr(valPtr, loop, resume);
        codeGenContext.Builder.PositionAtEnd(loop);
        var genned = ToExecute.DataRefCodeGen(codeGenContext);
        codeGenContext.Builder.PositionAtEnd(loop);
        if (ToExecute.HasGuaranteedExplicitReturn)
        {
            codeGenContext.Builder.BuildRet(genned.Type.LoadValue(codeGenContext,genned));
        }
        else
        {
            codeGenContext.Builder.BuildBr(loop);
        }
        codeGenContext.Builder.PositionAtEnd(resume);
        return codeGenContext.GetVoid();
    }

    public bool HasGuaranteedExplicitReturn => false;
}