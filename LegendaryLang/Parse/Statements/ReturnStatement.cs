using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Statements;

public class ReturnStatement : IStatement
{
    public ReturnStatement(ReturnToken token, IExpression toReturn)
    {
        Token = token;
        ToReturn = toReturn;
    }

    public LangPath TypePath => ToReturn?.TypePath ?? LangPath.VoidBaseLangPath;
    public ReturnToken Token { get; }
    public IExpression? ToReturn { get; }
    Token ISyntaxNode.Token => Token;


    public IEnumerable<ISyntaxNode> Children
    {
        get
        {
            if (ToReturn is not null) yield return ToReturn;
        }
    }


    public void Analyze(SemanticAnalyzer analyzer)
    {
        ToReturn?.Analyze(analyzer);
    }

    /// <summary>
    ///     The <see cref="BlockExpression" /> will handle the codegen
    /// </summary>
    public void CodeGen(CodeGenContext CodeGenContext)
    {
    }

    public static ReturnStatement Parse(Parser parser)
    {
        var parsed = parser.Pop();
        if (parsed is not ReturnToken returnToken)
            throw new ExpectedParserException(parser, ParseType.ReturnToken, parsed);

        IExpression? expression = null;
        if (parser.Peek() is not SemiColonToken) expression = IExpression.Parse(parser);
        return new ReturnStatement(returnToken, expression);
    }

    public bool HasGuaranteedExplicitReturn => true;
}