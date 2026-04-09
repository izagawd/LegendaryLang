using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class PrimitiveType : Type
{
    private readonly LLVMTypeRef _typeRef;
    private readonly string _name;

    public PrimitiveType(PrimitiveTypeDefinition definition, LLVMTypeRef typeRef, string name) : base(definition)
    {
        _typeRef = typeRef;
        _name = name;
    }

    public override LLVMTypeRef TypeRef
    {
        get => _typeRef;
        protected set => throw new NotImplementedException();
    }

    public override LangPath TypePath => TypeDefinition.TypePath;
    public override string Name => _name;

    public override int GetPrimitivesCompositeCount(CodeGenContext context) => 1;

    public override void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr)
    {
        codeGenContext.Builder.BuildStore(value.LoadValue(codeGenContext), ptr.ValueRef);
    }

    public override LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
    {
        if (valueRef.ValueRef.TypeOf.Kind != LLVMTypeKind.LLVMPointerTypeKind) return valueRef.ValueRef;
        return context.Builder.BuildLoad2(TypeRef, valueRef.ValueRef);
    }

    public override LLVMValueRef AssignToStack(CodeGenContext context, ValueRefItem dataRefItem)
    {
        LLVMValueRef value;
        if (dataRefItem.ValueRef.TypeOf.Kind != LLVMTypeKind.LLVMPointerTypeKind)
            value = dataRefItem.ValueRef;
        else
            value = context.Builder.BuildLoad2(TypeRef, dataRefItem.ValueRef);
        var allocated = context.Builder.BuildAlloca(TypeRef);
        context.Builder.BuildStore(value, allocated);
        return allocated;
    }
}
