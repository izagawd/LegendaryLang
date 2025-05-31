using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public abstract class PrimitiveType : Type
{
    public PrimitiveType(PrimitiveTypeDefinition definition) : base(definition)
    {
    }


    public override LangPath TypePath => TypeDefinition.TypePath;


    public override int GetPrimitivesCompositeCount(CodeGenContext context)
    {
        return 1;
    }

    public override void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr)
    {
        codeGenContext.Builder.BuildStore(value.LoadValForRetOrArg(codeGenContext), ptr.ValueRef);
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

    public override void CodeGen(CodeGenContext context)
    {
    }
}