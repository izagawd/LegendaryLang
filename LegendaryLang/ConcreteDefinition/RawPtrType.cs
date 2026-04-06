using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

/// <summary>
/// Concrete raw pointer type. Semantically different from RefType (no borrow checking, always Copy)
/// but identical at the LLVM level.
/// </summary>
public class RawPtrType : PointerLikeType
{
    public RawPtrTypeDefinition RawPtrTypeDefinition { get; }

    public RawPtrType(RawPtrTypeDefinition definition, Type pointingToType, LLVMTypeRef typeRef)
        : base(definition, pointingToType, typeRef)
    {
        RawPtrTypeDefinition = definition;
    }

    public override LangPath TypePath =>
        ((NormalLangPath)RawPtrTypeDefinition.TypePath).AppendGenerics([PointingToType.TypePath]);

    public override string Name => RawPtrTypeDefinition.Name;
}
