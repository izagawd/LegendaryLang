using System.Collections.Immutable;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Statements;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class BlockExpression : IExpression, IStatement
{
    public bool MustReturn { get; }
    public LeftCurlyBraceToken LeftCurlyBraceToken { get; }
    public RightCurlyBraceToken RightCurlyBraceToken { get; }
    public ImmutableArray<ISyntaxNode> SyntaxNodes { get; }

    public BlockExpression(LeftCurlyBraceToken leftCurlyBraceToken, RightCurlyBraceToken rightCurlyBraceToken, IEnumerable<ISyntaxNode> syntaxNodes, bool mustReturnVoid)
    {
        LeftCurlyBraceToken = leftCurlyBraceToken;
        RightCurlyBraceToken = rightCurlyBraceToken;
        SyntaxNodes = syntaxNodes.ToImmutableArray();
        MustReturn = mustReturnVoid;
    }

    public new  static BlockExpression Parse(Parser parser)
    { 
        var mustReturnVoid = true;
        var leftCurly = CurlyBrace.ParseLeft(parser);
        var syntaxNodes = new List<ISyntaxNode>();
        var next = parser.Peek();
        while (next is not Lex.Tokens.RightCurlyBraceToken)
        {
            
            while (next is SemiColonToken)
            {
                parser.Pop();
                next = parser.Peek();
            }
            if (next is RightCurlyBraceToken)
            {
                break;
            }

         
            ISyntaxNode parsed;
            if (next is IIsStatementToken)
            {
                parsed = IStatement.Parse(parser);
                syntaxNodes.Add(parsed);
    
            }
            else
            {
                parsed = IExpression.Parse(parser);
                syntaxNodes.Add(parsed);
            }

            next = parser.Peek();
            if (next is RightCurlyBraceToken && parsed is not IStatement)
            {
                mustReturnVoid = false;
                break;
            }
            // statements already handle semicolons themselves
            if (parsed is not IStatement)
            {
                SemiColon.Parse(parser);

            }     
            next = parser.Peek();
        }
        return new BlockExpression(leftCurly,CurlyBrace.Parseight(parser), syntaxNodes, mustReturnVoid);
    }

    public void CodeGen(CodeGenContext CodeGenContext)
    {
        throw new NotImplementedException();
    }

    public VariableRefItem DataRefCodeGen(CodeGenContext context)
    {
        // Optionally: Push a new scope if you have scope management.
        // context.SymbolTable.EnterScope();

        var lastValue = context.GetVoid();

        context.AddRefScope();

        // Iterate over each syntax node in the block.
        foreach (var node in SyntaxNodes)
        {
            // If the node is an expression, use its ValueRefCodeGen.
            if (node is IExpression expr)
            {
                lastValue = expr.DataRefCodeGen(context);
            }
            // If the node is a statement, simply generate code.
            else if (node is IStatement stmt)
            {
                lastValue = context.GetVoid();
                stmt.CodeGen(context);
            }
        }

        context.PopRefs();

        if (MustReturn)
        {

            return context.GetVoid();
        }
        else
        {
            return lastValue;
        }
    }

    public BaseLangPath? BaseLangPath { get; }


    public BaseLangPath SetTypePath(SemanticAnalyzer semanticAnalyzer)
    {
        if (MustReturn)
        {
            return BaseLangPath.VoidBaseLangPath;
        }

        if (SyntaxNodes.Length == 0)
        {
            return BaseLangPath.VoidBaseLangPath;
        }

        var last = SyntaxNodes.Last();
        if (last is IExpression expression)
        {
            return expression.SetTypePath(semanticAnalyzer);
        }
        return BaseLangPath.VoidBaseLangPath;
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // If you do semantic analysis here, iterate through the nodes.
        foreach (var node in SyntaxNodes)
        {
            node.Analyze(analyzer);
        }
    }

    public Token LookUpToken => LeftCurlyBraceToken;
}