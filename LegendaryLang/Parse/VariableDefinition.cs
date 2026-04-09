using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public class VariableDefinition
{
    public VariableDefinition(IdentifierToken token, LangPath? typePath = null, string? lifetime = null)
    {
        IdentifierToken = token;
        TypePath = typePath;
        Lifetime = lifetime;
    }

    public IdentifierToken IdentifierToken { get; }
    public string Name => IdentifierToken.Identity;
    public LangPath? TypePath { get; set; }
    /// <summary>
    /// Lifetime annotation on this field's reference type (e.g., &'a T → Lifetime = "a").
    /// Null if the field type is not a reference or has no lifetime.
    /// </summary>
    public string? Lifetime { get; }

    public static VariableDefinition Parse(Parser parser)
    {
        var name = Identifier.Parse(parser);
        var nextToken = parser.Peek();
        if (nextToken is ColonToken)
        {
            Colon.Parse(parser);
            LangPath.LastParsedLifetime = null;
            var typeId = LangPath.Parse(parser, true);
            var lifetime = LangPath.LastParsedLifetime;
            LangPath.LastParsedLifetime = null;
            return new VariableDefinition(name, typeId, lifetime);
        }

        return new VariableDefinition(name);
    }
}