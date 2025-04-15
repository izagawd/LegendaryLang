using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public class FunctionCallExpression : IExpression
{
    public static FunctionCallExpression ParseFunctionCallExpression(Parser parser,
        VariableRefItem dataRefItem)
    {
        return null;
    }
    public Token LookUpToken { get; }
    public void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }

    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        throw new NotImplementedException();
    }

    public LangPath? BaseLangPath { get; }
    public LangPath SetTypePath(SemanticAnalyzer semanticAnalyzer)
    {
        throw new NotImplementedException();
    }
}