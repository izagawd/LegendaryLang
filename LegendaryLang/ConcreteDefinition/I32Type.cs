using LegendaryLang.Definitions.Types;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class I32Type : PrimitiveType
{
    public I32Type(PrimitiveTypeDefinition definition) : base(definition)
    {
    }

    public override LLVMTypeRef TypeRef
    {
        get => LLVMTypeRef.Int32;
        protected set => throw new NotImplementedException();
    }

    public override string Name => "i32";
}