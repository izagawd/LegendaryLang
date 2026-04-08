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

        // Add reference type definitions for all 4 reference kinds
        items.Add(new RefTypeDefinition(RefKind.Shared)); // &T
        items.Add(new RefTypeDefinition(RefKind.Const));  // &const T
        items.Add(new RefTypeDefinition(RefKind.Mut));    // &mut T
        items.Add(new RefTypeDefinition(RefKind.Uniq));   // &uniq T

        // Add raw pointer type definitions for all 4 pointer kinds
        items.Add(new RawPtrTypeDefinition(RefKind.Shared)); // *shared T
        items.Add(new RawPtrTypeDefinition(RefKind.Const));  // *const T
        items.Add(new RawPtrTypeDefinition(RefKind.Mut));    // *mut T
        items.Add(new RawPtrTypeDefinition(RefKind.Uniq));   // *uniq T

        return new ParseResult
        {
            Items = items,
            File = null
        };
    }
}