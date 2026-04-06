namespace LegendaryLang.Parse;

/// <summary>
/// Represents a function,struct, const or static var, enum, or a trait definition, pre monomorphized (if monomorphization is applicable)
/// </summary>
public interface IDefinition : ISyntaxNode
{
    public string Name { get; }
    public LangPath TypePath {get; }
    public NormalLangPath Module { get; }


    // a token to describe where it could possibly be
}