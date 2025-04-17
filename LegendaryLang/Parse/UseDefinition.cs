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
        if (path is not NormalLangPath normalPath)
        {
            throw new Exception("d");
        }

        if (normalPath.PathSegments.Any(i => i is NormalLangPath.GenericTypesPathSegment))
        {
            throw new Exception("d");
        }
        
        SemiColon.Parse(parser);
        return new UseDefinition(normalPath, useToken);
    }
    public NormalLangPath PathToUse { get; }
    public void Analyze(SemanticAnalyzer analyzer)
    {
        analyzer.AddToDeepestScope(PathToUse.PathSegments.Last(),PathToUse);
    }

    

    public UseToken Token { get; }

    public UseDefinition(NormalLangPath pathToUse, UseToken token)
    {
        PathToUse = pathToUse;
        Token = token;

    }


    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return [];
    }

    public Token LookUpToken => Token;







}