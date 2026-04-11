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

        // Add unsized primitive types
        items.Add(new StrTypeDefinition());

        // Add reference type definitions
        items.Add(new RefTypeDefinition(RefKind.Shared)); // &T
        items.Add(new RefTypeDefinition(RefKind.Mut));    // &mut T

        // Add raw pointer type definitions
        items.Add(new RawPtrTypeDefinition(RefKind.Shared)); // *shared T
        items.Add(new RawPtrTypeDefinition(RefKind.Mut));    // *mut T

        return new ParseResult
        {
            Items = items,
            File = null
        };
    }
}