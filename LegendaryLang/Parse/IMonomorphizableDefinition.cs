using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class GenericParameter
{
    public readonly IdentifierToken? Identifier;
    public readonly string Name;
    public List<LangPath> TraitBounds;

    public GenericParameter(string name)
    {
        Name = name;
        TraitBounds = new List<LangPath>();
    }

    public GenericParameter(IdentifierToken identifier)
    {
        Identifier = identifier;
        Name = identifier.Identity;
        TraitBounds = new List<LangPath>();
    }

    public GenericParameter(IdentifierToken identifier, IEnumerable<LangPath> traitBounds)
    {
        Identifier = identifier;
        Name = identifier.Identity;
        TraitBounds = traitBounds.ToList();
    }
}