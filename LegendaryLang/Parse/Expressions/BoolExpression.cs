using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class BoolExpression : IExpression
{
    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return [];
    }

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

    public static BoolType BoolType { get; } = new(new BoolTypeDefinition());
    public unsafe VariableRefItem DataRefCodeGen(CodeGenContext context)
    {
        // Assume IBoolToken has a property "Value" that holds a Boolean.
        bool value = Token.Bool; // e.g., true or false
        // Create a constant i1 with value 1 for true, 0 for false.
        var boolValue = LLVM.ConstInt(BoolType.TypeRef, (ulong)(value ? 1 : 0), 0);
        return new VariableRefItem()
        {
            ValueRef = boolValue,
            Type = (context.GetRefItemFor(TypePath) as TypeRefItem)?.Type
        };
    }

    public LangPath? TypePath => BoolType.TypePath;


    public BoolExpression(IBoolToken token)
    {
        Token = token;
    }
    public Token LookUpToken =>(Token) Token ;
    public void Analyze(SemanticAnalyzer analyzer)
    {

    }
}