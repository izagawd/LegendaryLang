using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class GenericParameter
{
    public readonly IdentifierToken? Identifier;
    public readonly string Name;

    public GenericParameter(string name)
    {
        Name = name;
    }

    public GenericParameter(IdentifierToken identifier)
    {
        Identifier = identifier;
        Name = identifier.Identity;
    }
}