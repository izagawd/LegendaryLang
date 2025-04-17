using LegendaryLang.Lex.Tokens;


namespace LegendaryLang.Parse;

public class  VariableDefinition
{
    public static VariableDefinition Parse(Parser parser)
    {
        var name = Identifier.Parse(parser);
        var nextToken = parser.Peek();
        if (nextToken is ColonToken)
        {
            Colon.Parse(parser);
            var typeId = LangPath.Parse(parser);
            return new VariableDefinition(name, typeId);
        }
        return new VariableDefinition(name);
    }
    public IdentifierToken IdentifierToken {get; }
    public string Name => IdentifierToken.Identity;
    public LangPath? TypePath { get; set; }
    public VariableDefinition(IdentifierToken token, LangPath? typePath = null)
    {
        IdentifierToken = token;
        TypePath = typePath;
    }
}