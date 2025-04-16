using System.Reflection;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public class UseDefinition : IDefinition
{
    public static UseDefinition Parse(Parser parser)
    {
        var usin = parser.Pop();
        if (usin is not UseToken useToken)
        {
            throw new ExpectedParserException(parser, [ParseType.Use], usin);
        }
        var path = NormalLangPath.Parse(parser);
        return new UseDefinition(path, useToken,parser.File.Module);
    }
    public NormalLangPath Path { get; }
    void IDefinition.Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }

    Token IDefinition.Token => Token;

    public UseToken Token { get; }

    public UseDefinition(NormalLangPath path, UseToken token, NormalLangPath module)
    {
        Path = path;
        Token = token;
        Module = module;
    }
  

    public Token LookUpToken => Token;
    public string Name => $"using {Path}";
    /// <summary>
    ///NOTE: This doesnt refer to the module its using. it refers to the module in which the token is located!!!
    /// </summary>
    public NormalLangPath Module { get; }
    public bool HasBeenGened { get; set; }
    public void CodeGen(CodeGenContext context)
    {

    }



    void ISyntaxNode.Analyze(SemanticAnalyzer analyzer)
    {
        

    }
}