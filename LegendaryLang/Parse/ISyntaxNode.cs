using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public interface ISyntaxNode
{
    public bool IncludesReturnExpression
    {
        get { return false; }
    }
    public bool IncludesReturnStatement => false;
    /// <summary>
    /// used to set the shortcuts of lang paths.
    /// EG if
    /// use foo::bar; is declared within the scope of this syntax node
    /// is used
    /// any
    /// bar used in the syntax node will be evaluated to its full path (in this case: foo::bar)
    /// </summary>
    /// <param name="analyzer"></param>
    public void SetFullPathOfShortCuts(SemanticAnalyzer analyzer);
    
    
    
    /// </summary>
    /// <returns>all the functions that have been called inside this syntax node</returns>
    public IEnumerable<NormalLangPath> GetAllFunctionsUsed();
    
    /// <summary>
    /// Token used to locate where the syntax node is written
    /// </summary>
    public  Token Token { get; }

}