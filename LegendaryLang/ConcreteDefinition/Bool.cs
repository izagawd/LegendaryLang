using LegendaryLang.Definitions.Types;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class BoolType : PrimitiveType
{
    public BoolType(BoolTypeDefinition definition) : base(definition)
    {

    }
    public override LLVMTypeRef TypeRef
    {
        get => LLVMTypeRef.Int1;
        protected set => throw new NotImplementedException();
    }


    public override string Name => "bool";
  
}