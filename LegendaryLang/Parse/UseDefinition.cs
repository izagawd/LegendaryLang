using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public class UseDefinition : IItem
{
    public UseDefinition(NormalLangPath pathToUse, Token token)
    {
        PathToUse = pathToUse;
        Token = token;
    }

    public NormalLangPath PathToUse { get; }


    public Token Token { get; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
    }

    public IEnumerable<ISyntaxNode> Children => [];


    Token ISyntaxNode.Token => Token;

    public static UseDefinition Parse(Parser parser)
    {
        var usin = parser.Pop();
        if (usin is not UseToken useToken) throw new ExpectedParserException(parser, [ParseType.Use], usin);
        var path = NormalLangPath.Parse(parser);
        if (path is not NormalLangPath normalPath) throw new Exception("d");

        if (normalPath.PathSegments.Any(i => i is NormalLangPath.GenericTypesPathSegment)) throw new Exception("d");

        SemiColon.Parse(parser);
        return new UseDefinition(normalPath, useToken);
    }

    public void RegisterUsings(PathResolver resolver)
    {
        resolver.AddToDeepestScope(PathToUse.PathSegments.Last(), PathToUse);
    }

}