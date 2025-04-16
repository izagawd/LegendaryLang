using LegendaryLang.Lex.Tokens;

using LegendaryLang.Parse.Types;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class NumberExpression : IExpression
{
    public static NumberToken ParseToken(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not NumberToken numberToken)
        {
            throw new ExpectedParserException(parser, ParseType.Number, gotten);
        }
        return numberToken;
    }
    public static NumberExpression Parse(Parser parser)
    {
        return new NumberExpression(NumberExpression.ParseToken(parser));
    }
    public NumberToken Token { get; }
    public unsafe VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        return new VariableRefItem()
        {
            ValueRef = LLVM.ConstInt(LLVM.Int32Type(), ulong.Parse(Token.Number), 0),
            Type = (codeGenContext.GetRefItemFor(TypePath) as TypeRefItem).Type
        };
    }

    public LangPath? TypePath { get; set; }  =new I32().TypePath;



    public LangPath SetTypePath(SemanticAnalyzer semanticAnalyzer)
    {
        return TypePath!;
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {

    }

    public Token LookUpToken => Token;

    public NumberExpression(NumberToken token)
    {
        Token = token;
    }
}