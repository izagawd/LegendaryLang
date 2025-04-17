using System.Collections.Immutable;

namespace LegendaryLang.Parse;



public class GenericParameter
{
    public string Name;
    
}
public interface IMonomorphizableDefinition
{
    
    public ImmutableArray<GenericParameter> GenericParameters {get; }
}