using System.Reflection;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;

namespace LegendaryLang;

public class PrimitiveTypeGenerator
{
    public static ParseResult Generate()
    {
        var items = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsAssignableTo(typeof(PrimitiveTypeDefinition)) && !t.IsAbstract)
            .Select(i => (IItem)Activator.CreateInstance(i)).ToList();

        // Add pointer type definitions (immutable and mutable references)
        items.Add(new PointerTypeDefinition(false)); // &T
        items.Add(new PointerTypeDefinition(true));  // &mut T

        return new ParseResult
        {
            Items = items,
            File = null
        };
    }
}