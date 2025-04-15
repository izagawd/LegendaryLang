using LegendaryLang.Lex.Tokens;

using LegendaryLang.Parse.Types;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class BoolExpression : IExpression
{
    public static BoolExpression Parse(Parser parser)
    {
        var token = parser.Pop();
        if (token is not IBoolToken boolToken)
        {
            throw new ExpectedParserException(parser, ParseType.Bool,token);
        }

        return new BoolExpression(boolToken);
    }
    public IBoolToken Token { get; }

    public static BoolType BoolType { get; } = new BoolType();
    public unsafe VariableRefItem DataRefCodeGen(CodeGenContext context)
    {
        // Assume IBoolToken has a property "Value" that holds a Boolean.
        bool value = Token.Bool; // e.g., true or false
        // Create a constant i1 with value 1 for true, 0 for false.
        var boolValue = LLVM.ConstInt(BoolType.TypeRef, (ulong)(value ? 1 : 0), 0);
        return new VariableRefItem()
        {
            ValueRef = boolValue,
            Type = (context.GetRefItemFor(BaseLangPath) as TypeRefItem)?.Type
        };
    }

    public LangPath? BaseLangPath => BoolType.Ident;



    public LangPath SetTypePath(SemanticAnalyzer semanticAnalyzer)
    {
        return BoolType.Ident;
    }


    public BoolExpression(IBoolToken token)
    {
        Token = token;
    }
    public Token LookUpToken =>(Token) Token ;
    void ISyntaxNode.Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }
}