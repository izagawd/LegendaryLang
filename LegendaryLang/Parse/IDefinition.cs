using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public class LangModulePath : LangPath
{
    
    public override bool Equals(object? obj)
    {
        throw new NotImplementedException();
    }
}
public interface IDefinition : ISyntaxNode
{
    public string Name { get; }
    public NormalLangPath FullPath => Module.Append(Name);
    public NormalLangPath Module {get; }
    public bool HasBeenGened { get; set; }
    public void CodeGen(CodeGenContext context);
    
    public void Analyze(SemanticAnalyzer analyzer);
    // a token to describe where it could possibly be
    public Token Token { get; }
}