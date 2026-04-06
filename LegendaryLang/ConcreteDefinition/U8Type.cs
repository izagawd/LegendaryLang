using LegendaryLang.Definitions.Types;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class U8Type : PrimitiveType
{
    public U8Type(PrimitiveTypeDefinition definition) : base(definition)
    {
    }

    public override LLVMTypeRef TypeRef
    {
        get => LLVMTypeRef.Int8;
        protected set => throw new NotImplementedException();
    }

    public override string Name => "u8";
}
