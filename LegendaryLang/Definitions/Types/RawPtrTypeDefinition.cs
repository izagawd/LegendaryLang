using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Definitions.Types;

/// <summary>
/// Raw pointer types: *shared T, *const T, *mut T, *uniq T.
/// Unlike references, raw pointers have no borrow checking and are always Copy.
/// </summary>
public class RawPtrTypeDefinition : PointerTypeDefinitionBase
{
    public RawPtrTypeDefinition(RefKind kind)
    {
        Kind = kind;
    }

    public static NormalLangPath GetRawPtrModule()
    {
        return new NormalLangPath(null, ["Std", "Rawptr"]);
    }

    public static string GetRawPtrName(RefKind kind) => RefTypeDefinition.GetRefName(kind);

    public RefKind Kind { get; }

    public override string Name => GetRawPtrName(Kind);
    public override NormalLangPath Module => GetRawPtrModule();

    protected override ConcreteDefinition.Type CreateConcreteType(ConcreteDefinition.Type pointingToType, LLVMTypeRef typeRef)
        => new ConcreteDefinition.RawPtrType(this, pointingToType, typeRef);
}
