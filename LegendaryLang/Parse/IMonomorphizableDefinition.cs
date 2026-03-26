using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class GenericParameter
{
    public readonly IdentifierToken? Identifier;
    public readonly string Name;
    public LangPath? TraitBound;

    public GenericParameter(string name)
    {
        Name = name;
    }

    public GenericParameter(IdentifierToken identifier)
    {
        Identifier = identifier;
        Name = identifier.Identity;
    }

    public GenericParameter(IdentifierToken identifier, LangPath? traitBound)
    {
        Identifier = identifier;
        Name = identifier.Identity;
        TraitBound = traitBound;
    }
}