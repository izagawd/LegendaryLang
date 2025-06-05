using System.Reflection;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;

namespace LegendaryLang;

public class PrimitiveTypeGenerator
{
    public static ParseResult Generate()
    {
        return new ParseResult
        {
            Items = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsAssignableTo(typeof(PrimitiveTypeDefinition)) && !t.IsAbstract)
                .Select(i => (IItem)Activator.CreateInstance(i)).ToList(),
            File = null
        };
    }
}