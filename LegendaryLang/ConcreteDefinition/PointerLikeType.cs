using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

/// <summary>
/// Shared base for RefType and RawPtrType — they have identical LLVM codegen
/// (load pointer, store pointer, alloca-and-store for stack).
/// </summary>
public abstract class PointerLikeType : Type
{
    public Type PointingToType { get; }

    protected PointerLikeType(TypeDefinition definition, Type pointingToType, LLVMTypeRef typeRef)
        : base(definition)
    {
        PointingToType = pointingToType;
        TypeRef = typeRef;
    }

    public override LLVMTypeRef TypeRef { get; protected set; }
    public override int GetPrimitivesCompositeCount(CodeGenContext context) => 1;

    public override LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
    {
        if (valueRef.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            return context.Builder.BuildLoad2(TypeRef, valueRef.ValueRef);
        return valueRef.ValueRef;
    }

    public override void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr)
    {
        var loaded = LoadValue(codeGenContext, value);
        codeGenContext.Builder.BuildStore(loaded, ptr.ValueRef);
    }

    public override LLVMValueRef AssignToStack(CodeGenContext context, ValueRefItem dataRefItem)
    {
        var alloca = context.Builder.BuildAlloca(TypeRef);
        var ptrVal = LoadValue(context, dataRefItem);
        context.Builder.BuildStore(ptrVal, alloca);
        return alloca;
    }
}
