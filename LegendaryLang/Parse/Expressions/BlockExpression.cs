using System.Collections.Immutable;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Statements;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class BlockExpression : IExpression
{
    public struct BlockSyntaxNodeContainer
    {
        public   ISyntaxNode Node;
        public bool HasSemiColonAfter;
    }

    public void SetFullPathOfShortCuts(SemanticAnalyzer analyzer)
    {
        foreach (var i in SyntaxNodes)
        {
            i.SetFullPathOfShortCuts(analyzer);
        }
    }

    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {

        return SyntaxNodes.SelectMany(i => i.GetAllFunctionsUsed());
   
    }

    public LeftCurlyBraceToken LeftCurlyBraceToken { get; }
    public RightCurlyBraceToken RightCurlyBraceToken { get; }
    public ImmutableArray<ISyntaxNode> SyntaxNodes => BlockSyntaxNodeContainers.Select(i => i.Node).ToImmutableArray();
    

    public BlockExpression(LeftCurlyBraceToken leftCurlyBraceToken, RightCurlyBraceToken rightCurlyBraceToken, IEnumerable<BlockSyntaxNodeContainer> syntaxNodes)
    {
        LeftCurlyBraceToken = leftCurlyBraceToken;
        RightCurlyBraceToken = rightCurlyBraceToken;
        BlockSyntaxNodeContainers = [..syntaxNodes];
      
    }

    public ImmutableArray<BlockSyntaxNodeContainer> BlockSyntaxNodeContainers { get;  }

    public new  static BlockExpression Parse(Parser parser)
    { 
      
        var leftCurly = CurlyBrace.ParseLeft(parser);
        var syntaxNodes = new List<BlockSyntaxNodeContainer>();
        var next = parser.Peek();
        while (next is not Lex.Tokens.RightCurlyBraceToken)
        {
            
            while (parser.Peek() is SemiColonToken)
            {
                parser.Pop();
                next = parser.Peek();
            }
            if (next is RightCurlyBraceToken)
            {
                break;
            }

         
            ISyntaxNode parsed;
            if (next is IStatementToken)
            {
                parsed = IStatement.Parse(parser);
                // has semi colon after set to true sice statements are forced to have a semi colon after anyways
                syntaxNodes.Add(new BlockSyntaxNodeContainer { Node = parsed, HasSemiColonAfter = true });
                
            }
            else
            {
                parsed = IExpression.Parse(parser);
                var container = new BlockSyntaxNodeContainer { Node = parsed, HasSemiColonAfter = false };
                if (parser.Peek() is SemiColonToken)
                {
                    container.HasSemiColonAfter = true;
                }
                syntaxNodes.Add(container);
            }
            

  
            next = parser.Peek();
        }
        return new BlockExpression(leftCurly,CurlyBrace.Parseight(parser), syntaxNodes);
    }



    public VariableRefItem DataRefCodeGen(CodeGenContext context)
    {
        // Optionally: Push a new scope if you have scope management.
        // context.SymbolTable.EnterScope();
        
        var lastValue = context.GetVoid();
        context.AddScope();
        VariableRefItem? toEvalGenned = null;
        // Iterate over each syntax node in the block.
        foreach (var item in BlockSyntaxNodeContainers)
        {
            // If the node is an expression, use its ValueRefCodeGen.
            if (item.Node is IExpression expr)
            {
                
                lastValue = expr.DataRefCodeGen(context);
                if (item.Node == DtaRefExprToEval?.Node)
                {
                    toEvalGenned = lastValue;
                }
            }
            // If the node is a statement, simply generate code.
            else if (item.Node is IStatement stmt)
            {
                lastValue = context.GetVoid();
                stmt.CodeGen(context);
            }
        }

        context.PopScope();

        if (DtaRefExprToEval is not null)
        {
            if(DtaRefExprToEval.Value.HasSemiColonAfter)
                return context.GetVoid();
            return toEvalGenned ?? context.GetVoid();
        }

        return context.GetVoid();
        
       
    }

    public LangPath? TypePath { get; private set; }


    public BlockSyntaxNodeContainer? DtaRefExprToEval { get; set; }


    public void Analyze(SemanticAnalyzer analyzer)
    {
        ReturnStatement? lastReturnStatement = null;
        analyzer.AddScope();
        var last = BlockSyntaxNodeContainers.Cast<BlockSyntaxNodeContainer?>().LastOrDefault();

        foreach (var item in BlockSyntaxNodeContainers)
        {
  
            if (item.Node is IExpression expr && expr != last?.Node && !item.HasSemiColonAfter)
            {
                analyzer.AddException(new SemanticException($"Expected semicolon after expression" +
                                                            $"\n{item.Node.Token.GetLocationStringRepresentation()}"));
            }

            if (item.Node is ReturnStatement returnStatement)
            {
                if (lastReturnStatement is not null && lastReturnStatement.ToReturn.TypePath != returnStatement.ToReturn.TypePath)
                {
                    var returnTypePath = lastReturnStatement.ToReturn.TypePath;
                    analyzer.AddException(
                        new SemanticException($"Conflicting return types\n" +
                                              $"Expected {returnTypePath}, due to \n{returnTypePath.FirstIdentifierToken!.GetLocationStringRepresentation()}\n" +
                                              $"found {returnStatement.ToReturn.TypePath}"));
                }
                else
                {
                    lastReturnStatement = returnStatement;
                }
       

            }
        }

        foreach (var item in SyntaxNodes.OfType<IAnalyzable>())
        {
            item.Analyze(analyzer);
        }
        
        if (SyntaxNodes.Length == 0)
        {
            TypePath= LangPath.VoidBaseLangPath;
        }
        else
        {
            LangPath? possibleTypePath = null;
            if (last?.Node is IExpression expression)
            {
                if (last.Value.HasSemiColonAfter)
                {
                    possibleTypePath = LangPath.VoidBaseLangPath;
                }
                else
                {
                    possibleTypePath = expression.TypePath;
                }
            }
            
            var lastReturnTypePath = lastReturnStatement?.ToReturn.TypePath;
            if (lastReturnTypePath is not null && possibleTypePath is not null && possibleTypePath != lastReturnTypePath )
            {
                analyzer.AddException(new SemanticException($"Conflicting return types\n" +
                                      $"Expected {lastReturnTypePath}, due to \n{lastReturnStatement.Token!.GetLocationStringRepresentation()}\n" +
                                      $"found {possibleTypePath}"));
            }
            TypePath = lastReturnTypePath ?? possibleTypePath ?? LangPath.VoidBaseLangPath;
        }
        DtaRefExprToEval = last;
        analyzer.PopScope();
        
    }

    public Token Token => RightCurlyBraceToken;
}