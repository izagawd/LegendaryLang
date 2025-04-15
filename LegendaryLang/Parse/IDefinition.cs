using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public interface IDefinition : ISyntaxNode
{
    public bool HasBeenGened { get; set; }
    public void CodeGen(CodeGenContext context);
    public int Priority { get; }
    public void Analyze(SemanticAnalyzer analyzer);
    // a token to describe where it could possibly be
    public Token Token { get; }
}