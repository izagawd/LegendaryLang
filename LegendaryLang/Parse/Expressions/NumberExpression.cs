using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class NumberExpression : IExpression
{
    public NumberExpression(NumberToken token)
    {
        Token = token;
    }

    public NumberToken Token { get; }
    public IEnumerable<ISyntaxNode> Children => [];
    Token ISyntaxNode.Token => Token;


    public unsafe ValueRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        return new ValueRefItem
        {
            ValueRef = LLVM.ConstInt(LLVM.Int32Type(), ulong.Parse(Token.Number), 0),
            Type = (codeGenContext.GetRefItemFor(TypePath) as TypeRefItem).Type
        };
    }

    public LangPath? TypePath { get; set; } = new I32TypeDefinition().TypePath;


    public void Analyze(SemanticAnalyzer analyzer)
    {
    }

    public static NumberToken ParseToken(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not NumberToken numberToken) throw new ExpectedParserException(parser, ParseType.Number, gotten);
        return numberToken;
    }

    public static NumberExpression Parse(Parser parser)
    {
        return new NumberExpression(ParseToken(parser));
    }

    public bool HasGuaranteedExplicitReturn => false;
}