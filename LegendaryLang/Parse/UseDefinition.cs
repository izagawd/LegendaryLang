using System.Reflection;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public class UseDefinition : ITopLevel
{
    public static UseDefinition Parse(Parser parser)
    {
        var usin = parser.Pop();
        if (usin is not UseToken useToken)
        {
            throw new ExpectedParserException(parser, [ParseType.Use], usin);
        }
        var path = NormalLangPath.Parse(parser);
        SemiColon.Parse(parser);
        return new UseDefinition(path, useToken);
    }
    public NormalLangPath PathToUse { get; }
    public void Analyze(SemanticAnalyzer analyzer)
    {
        analyzer.AddToDeepestScope(PathToUse.Path.Last(),PathToUse);
    }

    

    public UseToken Token { get; }

    public UseDefinition(NormalLangPath pathToUse, UseToken token)
    {
        PathToUse = pathToUse;
        Token = token;

    }
  

    public Token LookUpToken => Token;







}