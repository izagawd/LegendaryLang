using System.Reflection;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

/// <summary>
///     SYNTAX NODE THAT CAN BE WRITTEN OUTSIDE OF A FUNCTION/BLOCK
/// </summary>
public interface IItem : ISyntaxNode
{
    bool ISyntaxNode.NeedsSemiColonAfterIfNotLastInBlock => false;

    public static bool NextTokenIsItem(Parser parser)
    {
        var next = parser.Peek();
        return next is FnToken or StructToken or UseToken;
    }
    public static IItem Parse(Parser parser, NormalLangPath module)
    {
     

        if (parser.Peek() is not null)
        {
            var gotten = parser. Peek();
            if (gotten is FnToken)
                return FunctionDefinition.Parse(parser,module);
            else if (gotten is StructToken)
                return StructTypeDefinition.Parse(parser,module);
            else if (gotten is UseToken)
                return  UseDefinition.Parse(parser);
            else
                throw new ExpectedParserException(parser, [ParseType.Struct, ParseType.Fn], gotten);
        }
        else
        {
            throw new ExpectedParserException(parser, [ParseType.Struct, ParseType.Fn], parser.Peek());
        }
    }
}