using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;

public static class Identifier
{
    public static IdentifierToken Parse(Parser parser)
    {
        var token = parser.Pop();
        if (token is IdentifierToken identifier) return identifier;

        throw new ExpectedParserException(parser, ParseType.Identifier,
            token);
    }
}