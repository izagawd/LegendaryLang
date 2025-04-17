using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;



public class GenericParameter
{
    public readonly string Name;
    public readonly IdentifierToken? Identifier;

    public GenericParameter(string name)
    {
        Name = name;

    }

    public GenericParameter( IdentifierToken identifier )
    {
        Identifier = identifier;
        Name = identifier.Identity;
    }
}
