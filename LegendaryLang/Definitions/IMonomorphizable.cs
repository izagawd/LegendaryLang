using System.Collections.Immutable;
using LegendaryLang.Parse;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions;



public interface IMonomorphizable : IDefinition
{


    /// <summary>
    /// Creates a definition,does not the implementation, of an Item. It will contain a pointer to the definition.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="genericArguments"></param>
    /// <returns></returns>
    public  IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments);

}