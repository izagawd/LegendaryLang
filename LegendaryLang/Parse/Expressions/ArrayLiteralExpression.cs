using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

/// <summary>
/// Represents an array literal expression: [expr1, expr2, expr3]
/// All elements must have the same type. The result type is [T; N].
/// </summary>
public class ArrayLiteralExpression : IExpression
{
    public ImmutableArray<IExpression> Elements { get; }
    public Token Token { get; }
    public LangPath? TypePath { get; set; }
    public IEnumerable<ISyntaxNode> Children => Elements;
    bool ISyntaxNode.NeedsSemiColonAfterIfNotLastInBlock => false;
    public bool IsTemporary => true;
    public bool HasGuaranteedExplicitReturn => false;

    public ArrayLiteralExpression(IEnumerable<IExpression> elements, Token token)
    {
        Elements = elements.ToImmutableArray();
        Token = token;
    }

    public static ArrayLiteralExpression Parse(Parser parser)
    {
        var bracketToken = Bracket.ParseLeft(parser);
        var elements = new List<IExpression>();

        while (parser.Peek() is not RightBracketToken)
        {
            elements.Add(IExpression.ParsePrimary(parser));
            if (parser.Peek() is CommaToken)
                parser.Pop();
            else
                break;
        }

        Bracket.ParseRight(parser);
        return new ArrayLiteralExpression(elements, bracketToken);
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        if (Elements.Length == 0)
        {
            analyzer.AddException(new SemanticException(
                $"Empty array literals are not allowed — cannot infer element type\n{Token.GetLocationStringRepresentation()}"));
            TypePath = LangPath.VoidBaseLangPath;
            return;
        }

        foreach (var elem in Elements)
            elem.Analyze(analyzer);

        var elementType = Elements[0].TypePath;
        for (int i = 1; i < Elements.Length; i++)
        {
            if (Elements[i].TypePath != elementType)
            {
                analyzer.AddException(new SemanticException(
                    $"Array element type mismatch: expected '{elementType}' but got '{Elements[i].TypePath}'\n" +
                    Token.GetLocationStringRepresentation()));
            }
        }

        var sizePath = new NormalLangPath(null, [Elements.Length.ToString()]);
        TypePath = new ArrayLangPath(elementType!, sizePath);
    }

    public ValueRefItem CodeGen(CodeGenContext context)
    {
        var arrayPath = (ArrayLangPath)TypePath!;

        // Resolve the array type
        var arrayTypeRefItem = (TypeRefItem)context.GetRefItemFor(arrayPath)!;
        var arrayType = arrayTypeRefItem.Type;
        var llvmArrayType = arrayType.TypeRef;

        // Build array value using insertvalue (like tuple construction)
        LLVMValueRef aggr;
        unsafe { aggr = LLVM.GetUndef(llvmArrayType); }
        for (int i = 0; i < Elements.Length; i++)
        {
            var elemVal = Elements[i].CodeGen(context);
            var rawVal = elemVal.LoadValue(context);
            aggr = context.Builder.BuildInsertValue(aggr, rawVal, (uint)i);
        }

        return new ValueRefItem
        {
            Type = arrayType,
            ValueRef = aggr
        };
    }
}
