namespace LegendaryLang.Parse;

/// <summary>
/// Represents a function,struct, const or static var, enum, or a trait definition, pre monomorphized (if monomorphization is applicable)
/// </summary>
public interface IDefinition : ISyntaxNode
{
    public string Name { get; }
    public NormalLangPath FullPath => Module.Append(Name);
    public NormalLangPath Module { get; }
    public bool HasBeenGened { get; set; }


    // a token to describe where it could possibly be
}