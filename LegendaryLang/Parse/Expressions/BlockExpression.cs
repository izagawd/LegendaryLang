﻿using System.Collections.Immutable;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Statements;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public class BlockExpression : IExpression
{
    public BlockExpression(LeftCurlyBraceToken leftCurlyBraceToken, RightCurlyBraceToken rightCurlyBraceToken,
        IEnumerable<BlockSyntaxNodeContainer> syntaxNodes, LangPath? expectedReturnType)
    {
        LeftCurlyBraceToken = leftCurlyBraceToken;
        RightCurlyBraceToken = rightCurlyBraceToken;
        BlockSyntaxNodeContainers = [..syntaxNodes];
        ExpectedReturnType = expectedReturnType;
    }


    public LeftCurlyBraceToken LeftCurlyBraceToken { get; }
    public RightCurlyBraceToken RightCurlyBraceToken { get; }
    public ImmutableArray<ISyntaxNode> SyntaxNodes => BlockSyntaxNodeContainers.Select(i => i.Node).ToImmutableArray();

    public LangPath? ExpectedReturnType { get; set; }

    public ImmutableArray<BlockSyntaxNodeContainer> BlockSyntaxNodeContainers { get; }
    public IEnumerable<ISyntaxNode> Children => SyntaxNodes;


    public bool HasGuaranteedExplicitReturn => SyntaxNodes.Where(i => i is not IItem).OfType<ICanHaveExplicitReturn>().Any(i => i.HasGuaranteedExplicitReturn);
    public ValueRefItem DataRefCodeGen(CodeGenContext context)
    {
        var lastValue = context.GetVoid();
        context.AddScope();

        foreach (var i in SyntaxNodes.OfType<IDefinition>())
        {
            context.AddToDeepestScope(i);
        }
        // Iterate over each syntax node in the block.
        foreach (var item in BlockSyntaxNodeContainers.Where(i => i.Node is not IItem)) 
        {
            // If the node is an expression, use its ValueRefCodeGen.
            if (item.Node is IExpression expr)
            {
                lastValue = expr.DataRefCodeGen(context);
            }
            // return statements are special, as when you encounter one theres no
            // point in code genning the rest of the nodes
            else if (item.Node is ReturnStatement ret)
            {
                lastValue = ret.ToReturn?.DataRefCodeGen(context) ?? context.GetVoid();
                break;
            }
            // If the node is a statement, simply generate code.
            else if (item.Node is IStatement stmt)
            {
                lastValue = context.GetVoid();
                stmt.CodeGen(context);
            }


            ReturnStatement? GetFirstNoticedGuaranteedReturn(ISyntaxNode syntaxNode)
            {
                if (syntaxNode is IItem)
                {
                    return null;
                }
                // an if chain that ends with an "else if" or "if" is not guaranteed to always return a value,
                // so it is ignored
                if (syntaxNode is ICanHaveExplicitReturn can && !can.HasGuaranteedExplicitReturn) return null;
                if (syntaxNode is ReturnStatement returnStatement) return returnStatement;

                foreach (var child in syntaxNode.Children.Where(i => i is not IItem))
                {
                    var ret = GetFirstNoticedGuaranteedReturn(child);
                    if (ret is not null) return ret;
                }

                return null;
            }

            // if encountering a return statement after recursive checks, ignoring if expressions,
            // stop looping, since explicit returns ignores the rest of the code in
            // blocks anyways
            var firstNoticed = GetFirstNoticedGuaranteedReturn(item.Node);
            if (firstNoticed is not null) break;
        }


        context.PopScope();


        if (lastValue.Type.TypePath == LangPath.VoidBaseLangPath)
        {
            if (ExpectedReturnType == LangPath.VoidBaseLangPath) return context.GetVoid();
            // if the last values type is void, despite desired expected return type not being void,
            // it "should" be assumed that the return statement is unreachable, so we just
            // return an uninitialized value for its expected return type
            return (context.GetRefItemFor(ExpectedReturnType) as TypeRefItem).Type.CreateUninitializedValRef(context);
        }

        return lastValue;
    }


    public LangPath? TypePath { get; private set; }


    public bool NeedsSemiColonAfterIfNotLastInBlock
    {
        get
        {
            if (TypePath == LangPath.VoidBaseLangPath) return false;
            return true;
        }
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        analyzer.AddScope();
        var last = BlockSyntaxNodeContainers.Cast<BlockSyntaxNodeContainer?>().LastOrDefault();

        void SetExpectedReturnTypesRecursively(ISyntaxNode syntaxNode)
        {
            if (syntaxNode is BlockExpression block) block.ExpectedReturnType = ExpectedReturnType;
            foreach (var i in syntaxNode.Children.Where(i => i is not IItem)) SetExpectedReturnTypesRecursively(i);
        }

        foreach (var i in BlockSyntaxNodeContainers.Select(i => i.Node).OfType<IDefinition>() )
        {
            analyzer.RegisterDefinitionAtDeepestScope(i.FullPath,i);
        }
        SetExpectedReturnTypesRecursively(this);
        foreach (var item in SyntaxNodes.OfType<IAnalyzable>()) item.Analyze(analyzer);

        foreach (var item in BlockSyntaxNodeContainers)
        {
            if (item.Node.NeedsSemiColonAfterIfNotLastInBlock && item.Node != last?.Node && !item.HasSemiColonAfter)
                analyzer.AddException(new SemanticException($"Expected semicolon after" +
                                                            $"\n{item.Node.Token.GetLocationStringRepresentation()}"));

            if (item.Node is ReturnStatement returnStatement)
                if (ExpectedReturnType != returnStatement.TypePath)
                    analyzer.AddException(
                        new SemanticException(
                            $"Expected {ExpectedReturnType}, " +
                            $"found {returnStatement.TypePath}\n{returnStatement.Token!.GetLocationStringRepresentation()}"));
        }


        if (SyntaxNodes.Length == 0)
        {
            TypePath = LangPath.VoidBaseLangPath;
        }
        else
        {
            LangPath? possibleTypePath = null;
            if (last is not null)
            {
                if (last.Value.HasSemiColonAfter)
                    possibleTypePath = LangPath.VoidBaseLangPath;
                else if (last.Value.Node is IExpression expr) possibleTypePath = expr.TypePath;
            }

            TypePath = possibleTypePath ?? LangPath.VoidBaseLangPath;
        }

        analyzer.PopScope();
    }

    public Token Token => RightCurlyBraceToken;
    

    public void ResolvePaths(PathResolver resolver)
    {
        resolver.AddScope();
        if (ExpectedReturnType is not null)
            ExpectedReturnType = ExpectedReturnType.Resolve(resolver);
    
        foreach (var useDefinition in Children.OfType<UseDefinition>())
        {
            useDefinition.RegisterUsings(resolver);
        }

        foreach (var i in Children.OfType<IDefinition>() )
        {
            var usings = new UseDefinition(i.FullPath, i.Token);
            usings.RegisterUsings(resolver);
        }
        foreach (var i in Children.OfType<IPathResolvable>())
        {
            i.ResolvePaths(resolver);
        }
        resolver.PopScope();
    }


    public new static BlockExpression Parse(Parser parser, LangPath? expectedReturnType)
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

            if (next is RightCurlyBraceToken) break;


            ISyntaxNode parsed;
            if (next is IStatementToken)
            {
                parsed = IStatement.Parse(parser);
                // has semi colon after set to true sice statements are forced to have a semi colon after anyways
                syntaxNodes.Add(new BlockSyntaxNodeContainer { Node = parsed, HasSemiColonAfter = true });
            } else if (IItem.NextTokenIsItem(parser))
            {
                parsed = IItem.Parse(parser, new NormalLangPath(null,[new NormalLangPath.UntypableSegment()]));
                syntaxNodes.Add(new BlockSyntaxNodeContainer { Node = parsed, HasSemiColonAfter = parser.Peek() is SemiColonToken });
            }
            else
            {
                parsed = IExpression.Parse(parser);
                var container = new BlockSyntaxNodeContainer { Node = parsed, HasSemiColonAfter = false };
                if (parser.Peek() is SemiColonToken) container.HasSemiColonAfter = true;
                syntaxNodes.Add(container);
            }


            next = parser.Peek();
        }

        return new BlockExpression(leftCurly, CurlyBrace.Parseight(parser), syntaxNodes, expectedReturnType);
    }

    public struct BlockSyntaxNodeContainer
    {
        public ISyntaxNode Node;
        public bool HasSemiColonAfter;
    }
}