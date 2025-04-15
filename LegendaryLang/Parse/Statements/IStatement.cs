using System.Diagnostics;

namespace LegendaryLang.Parse.Statements;

public interface IStatement: ISyntaxNode
{
    public static IStatement Parse(Parser parser)
    {
        var parsed = LetStatement.Parse(parser);
        SemiColon.Parse(parser);
        return parsed;
    }
    
    
    public void CodeGen(CodeGenContext CodeGenContext);
}