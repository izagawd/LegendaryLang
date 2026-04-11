using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

/// <summary>
/// Represents the array type [T; N] where T is the element type and N is a comptime usize.
/// Size is stored as a LangPath to support both concrete literals (e.g., "4") and 
/// generic parameters (e.g., "N") that get substituted during monomorphization.
/// </summary>
public class ArrayLangPath : LangPath
{
    public LangPath ElementType { get; }
    
    /// <summary>
    /// The size of the array. For concrete arrays, this is a NormalLangPath with a numeric segment (e.g., "4").
    /// For generic arrays, this is a NormalLangPath with a parameter name (e.g., "N").
    /// </summary>
    public LangPath Size { get; }

    public ArrayLangPath(LangPath elementType, LangPath size, IdentifierToken? firstIdentifierToken = null)
    {
        ElementType = elementType;
        Size = size;
        FirstIdentifierToken = firstIdentifierToken;
    }

    /// <summary>
    /// Tries to extract the concrete size as a ulong. Returns null if the size is a generic parameter.
    /// </summary>
    public ulong? TryGetConcreteSize()
    {
        if (Size is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            var seg = nlp.PathSegments[0].ToString();
            if (ulong.TryParse(seg, out var result))
                return result;
        }
        return null;
    }

    public override string ToString() => $"[{ElementType}; {Size}]";

    public override bool Equals(object? obj)
    {
        if (obj is ArrayLangPath other)
            return ElementType.Equals(other.ElementType) && Size.Equals(other.Size);
        return false;
    }

    public override int GetHashCode() => ToString().GetHashCode();

    public override bool IsMonomorphizedFrom(LangPath definitionLangPath)
    {
        return definitionLangPath is ArrayLangPath;
    }

    public override ImmutableArray<LangPath> GetGenericArguments()
    {
        return [ElementType, Size];
    }

    public override LangPath Resolve(PathResolver resolver)
    {
        return new ArrayLangPath(ElementType.Resolve(resolver), Size.Resolve(resolver));
    }

    public override LangPath Monomorphize(CodeGenContext codeGen)
    {
        return new ArrayLangPath(ElementType.Monomorphize(codeGen), Size.Monomorphize(codeGen));
    }
}
