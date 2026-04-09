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
    public bool IsTemporary => true; // returns void
    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var condBlock = codeGenContext.Builder.InsertBlock.Parent.AppendBasicBlock("while.cond");
        var bodyBlock = codeGenContext.Builder.InsertBlock.Parent.AppendBasicBlock("while.body");
        var resumeBlock = codeGenContext.Builder.InsertBlock.Parent.AppendBasicBlock("while.resume");

        // Jump to the condition block to start the loop
        codeGenContext.Builder.BuildBr(condBlock);

        // Condition block: evaluate condition, branch to body or resume
        codeGenContext.Builder.PositionAtEnd(condBlock);
        var condVal = Condition.CodeGen(codeGenContext);
        var condLoaded = condVal.Type.LoadValue(codeGenContext, condVal);
        codeGenContext.Builder.BuildCondBr(condLoaded, bodyBlock, resumeBlock);

        // Body block: execute body, then jump back to condition
        codeGenContext.Builder.PositionAtEnd(bodyBlock);
        var genned = ToExecute.CodeGen(codeGenContext);

        if (ToExecute.HasGuaranteedExplicitReturn)
        {
            codeGenContext.Builder.BuildRet(genned.Type.LoadValue(codeGenContext, genned));
        }
        else
        {
            codeGenContext.Builder.BuildBr(condBlock);
        }

        codeGenContext.Builder.PositionAtEnd(resumeBlock);
        return codeGenContext.GetVoid();
    }

    public bool HasGuaranteedExplicitReturn => false;
}