using LegendaryLang.Lex.Tokens;


namespace LegendaryLang.Parse;

public class  Variable
{
    public static Variable Parse(Parser parser)
    {
        var name = Identifier.Parse(parser);
        var nextToken = parser.Peek();
        if (nextToken is ColonToken)
        {
            Colon.Parse(parser);
            var typeId = BaseLangPath.Parse(parser);
            return new Variable(name, typeId);
        }
        return new Variable(name);
    }
    public IdentifierToken IdentifierToken {get; }
    public string Name => IdentifierToken.Identity;
    public BaseLangPath? TypePath { get; set; }
    public Variable(IdentifierToken token, BaseLangPath? typePath = null)
    {
        IdentifierToken = token;
        TypePath = typePath;
    }
}