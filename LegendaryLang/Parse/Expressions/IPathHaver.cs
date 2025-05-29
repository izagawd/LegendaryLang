using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

/// <summary>
/// Implement this interface if your syntax node directly has some lang paths, so the semantic analysis can find them and shortcut them
/// </summary>
public interface IPathHaver : ISyntaxNode
{
    void SetFullPathOfShortCutsDirectly(SemanticAnalyzer analyzer);
}