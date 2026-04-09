using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class RefType : PointerLikeType
{
    public RefTypeDefinition RefTypeDefinition { get; }

    public RefType(RefTypeDefinition definition, Type pointingToType, LLVMTypeRef typeRef,
        LLVMTypeRef? elementTypeRef = null, LLVMTypeRef? metadataTypeRef = null)
        : base(definition, pointingToType, typeRef, elementTypeRef, metadataTypeRef)
    {
        RefTypeDefinition = definition;
    }

    public override LangPath TypePath =>
        ((NormalLangPath)RefTypeDefinition.TypePath).AppendGenerics([PointingToType.TypePath]);

    public override string Name => RefTypeDefinition.Name;
}
