using System.Collections.Immutable;
using LegendaryLang.Parse;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions;



public interface IMonomorphizable : IDefinition
{


    public  IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments);

}