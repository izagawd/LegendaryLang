using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
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
    Token ISyntaxNode.Token => (Token)Token;
    public void SetFullPathOfShortCuts(SemanticAnalyzer analyzer)
    {
        
    }

    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return [];
    }

    public unsafe VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        return new VariableRefItem()
        {
            ValueRef = LLVM.ConstInt(LLVM.Int32Type(), ulong.Parse(Token.Number), 0),
            Type = (codeGenContext.GetRefItemFor(TypePath) as TypeRefItem).Type
        };
    }

    public LangPath? TypePath { get; set; }  =new I32TypeDefinition().TypePath;





    public void Analyze(SemanticAnalyzer analyzer)
    {

    }



    public NumberExpression(NumberToken token)
    {
        Token = token;
    }
}