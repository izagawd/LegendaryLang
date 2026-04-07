using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

/// <summary>
/// Shared base for RefType and RawPtrType.
/// Handles both thin pointers (sized pointees) and fat pointers (unsized pointees).
/// A fat pointer is a struct {data_ptr, metadata} where metadata depends on the pointee
/// (usize for slices/str, vtable ptr for trait objects).
/// </summary>
public abstract class PointerLikeType : Type
{
    public Type PointingToType { get; }

    /// <summary>The LLVM type of the metadata field, or null for thin pointers.</summary>
    public LLVMTypeRef? MetadataTypeRef { get; }

    /// <summary>Whether this is a fat pointer (unsized pointee with metadata).</summary>
    public bool IsFat => MetadataTypeRef != null;

    protected PointerLikeType(TypeDefinition definition, Type pointingToType, LLVMTypeRef typeRef,
        LLVMTypeRef? metadataTypeRef = null)
        : base(definition)
    {
        PointingToType = pointingToType;
        TypeRef = typeRef;
        MetadataTypeRef = metadataTypeRef;
    }

    public override LLVMTypeRef TypeRef { get; protected set; }
    public override int GetPrimitivesCompositeCount(CodeGenContext context) => IsFat ? 2 : 1;

    /// <summary>
    /// Extracts just the data pointer from a pointer value (thin or fat).
    /// For thin pointers, this is the pointer itself.
    /// For fat pointers, this loads field 0 of the {ptr, metadata} struct.
    /// </summary>
    public LLVMValueRef ExtractDataPointer(CodeGenContext context, ValueRefItem valueRef)
    {
        if (!IsFat) return LoadValue(context, valueRef);
        var structVal = LoadValue(context, valueRef);
        return context.Builder.BuildExtractValue(structVal, 0);
    }

    /// <summary>
    /// Extracts the metadata from a fat pointer value.
    /// For thin pointers, returns null.
    /// For fat pointers, loads field 1 of the {ptr, metadata} struct.
    /// </summary>
    public LLVMValueRef? ExtractMetadata(CodeGenContext context, ValueRefItem valueRef)
    {
        if (!IsFat) return null;
        var structVal = LoadValue(context, valueRef);
        return context.Builder.BuildExtractValue(structVal, 1);
    }

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
