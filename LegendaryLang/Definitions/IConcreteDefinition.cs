using LegendaryLang.Parse;

namespace LegendaryLang.Definitions;

/// <summary>
///     EG MyStruct <T>
///         where T is a generic wont count as a concrete definition,
///         but MyStruct<i32> would count. same goes for functions and traits.
/// </summary>
public interface IConcreteDefinition : IDefinition
{
    /// <summary>
    /// Original definition of this item, pre monomorphizatino
    /// </summary>
    public IDefinition? Definition { get; }


}