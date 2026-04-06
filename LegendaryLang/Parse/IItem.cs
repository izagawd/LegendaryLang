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
        return next is FnToken or StructToken or UseToken or TraitToken or ImplToken or EnumToken;
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
            else if (gotten is EnumToken)
                return EnumTypeDefinition.Parse(parser,module);
            else if (gotten is UseToken)
                return  UseDefinition.Parse(parser);
            else if (gotten is TraitToken)
                return TraitDefinition.Parse(parser,module);
            else if (gotten is ImplToken)
                return ImplDefinition.Parse(parser,module);
            else
                throw new ExpectedParserException(parser, [ParseType.Struct, ParseType.Fn, ParseType.Trait, ParseType.Impl, ParseType.Enum], gotten);
        }
        else
        {
            throw new ExpectedParserException(parser, [ParseType.Struct, ParseType.Fn, ParseType.Trait, ParseType.Impl, ParseType.Enum], parser.Peek());
        }
    }
}