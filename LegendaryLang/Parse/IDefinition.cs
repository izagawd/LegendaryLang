namespace LegendaryLang.Parse;

public interface IDefinition : ISyntaxNode
{
    public string Name { get; }
    public NormalLangPath FullPath => Module.Append(Name);
    public NormalLangPath Module { get; }
    public bool HasBeenGened { get; set; }


    // a token to describe where it could possibly be
}