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
        return new UseDefinition(path, useToken,parser.File.Module);
    }
    public NormalLangPath PathToUse { get; }
    public void Analyze(SemanticAnalyzer analyzer)
    {
        analyzer.AddToDeepestScope(PathToUse.Path.Last(),PathToUse);
    }

    

    public UseToken Token { get; }

    public UseDefinition(NormalLangPath pathToUse, UseToken token, NormalLangPath module)
    {
        PathToUse = pathToUse;
        Token = token;
        Module = module;
    }
  

    public Token LookUpToken => Token;
    public string Name => $"using {PathToUse}";
    /// <summary>
    ///NOTE: This doesnt refer to the module its using. it refers to the module in which the token is located!!!
    /// </summary>
    public NormalLangPath Module { get; }
    public bool HasBeenGened { get; set; }
    public void CodeGen(CodeGenContext context)
    {

    }




}