using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

/// <summary>
/// Shared base for RefType and RawPtrType.
/// ALL pointer-like types are represented as {ptr, metadata} structs in LLVM IR.
/// For sized pointees the metadata field is the zero-sized empty-struct () type.
/// This uniform layout eliminates all thin/fat conditional branches — every path
/// is data-driven over the same two-field struct.
/// </summary>
public abstract class PointerLikeType : Type
{
    public Type PointingToType { get; }

    /// <summary>The LLVM type of the metadata field. Empty struct ({}) for sized pointees, usize for slices/str.</summary>
    public LLVMTypeRef MetadataTypeRef { get; }

    /// <summary>For unsized pointees: the LLVM type of each element (i8 for str, T for [T]).</summary>
    public LLVMTypeRef? ElementTypeRef { get; }

    /// <summary>
    /// Whether this pointer carries non-trivial (non-zero-sized) metadata.
    /// True for slices and str, false for all sized pointees.
    /// </summary>
    public bool HasNonTrivialMetadata => ElementTypeRef != null;

    protected PointerLikeType(TypeDefinition definition, Type pointingToType, LLVMTypeRef typeRef,
        LLVMTypeRef? elementTypeRef = null, LLVMTypeRef? metadataTypeRef = null)
        : base(definition)
    {
        PointingToType = pointingToType;
        TypeRef = typeRef;
        ElementTypeRef = elementTypeRef;
        MetadataTypeRef = metadataTypeRef ?? LLVMTypeRef.CreateStruct([], false);
    }

    public override LLVMTypeRef TypeRef { get; protected set; }
    public override int GetPrimitivesCompositeCount(CodeGenContext context) => HasNonTrivialMetadata ? 2 : 1;

    /// <summary>
    /// Loads the full {ptr, metadata} struct value from storage.
    /// If valueRef is an alloca (pointer kind), loads from it; otherwise returns the value directly.
    /// </summary>
    public override LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
    {
        if (valueRef.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            return context.Builder.BuildLoad2(TypeRef, valueRef.ValueRef);
        return valueRef.ValueRef;
    }

    /// <summary>
    /// Extracts the raw data pointer from the {ptr, metadata} struct (field 0).
    /// Works uniformly for both thin ({ptr, {}}) and fat ({ptr, metadata}) pointers.
    /// </summary>
    public LLVMValueRef ExtractDataPointer(CodeGenContext context, ValueRefItem valueRef)
    {
        var structVal = LoadValue(context, valueRef);
        return context.Builder.BuildExtractValue(structVal, 0);
    }

    /// <summary>
    /// Extracts the metadata value from the {ptr, metadata} struct (field 1).
    /// Returns the zero-sized () value for thin pointers, usize for fat pointers.
    /// </summary>
    public LLVMValueRef ExtractMetadata(CodeGenContext context, ValueRefItem valueRef)
    {
        var structVal = LoadValue(context, valueRef);
        return context.Builder.BuildExtractValue(structVal, 1);
    }

    public override void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr)
    {
        var loaded = LoadValue(codeGenContext, value);
        codeGenContext.Builder.BuildStore(loaded, ptr.ValueRef);
    }

    public override LLVMValueRef AssignToStack(CodeGenContext context, ValueRefItem dataRefItem)
    {
        var alloca = context.Builder.BuildAlloca(TypeRef);
        var structVal = LoadValue(context, dataRefItem);
        context.Builder.BuildStore(structVal, alloca);
        return alloca;
    }

    /// <summary>
    /// Constructs a {ptr, metadata} struct from a raw data pointer and metadata value.
    /// For thin pointers pass LLVMValueRef.CreateConstNull(emptyStructType) as metadata.
    /// </summary>
    public ValueRefItem BuildFatPointer(CodeGenContext context, LLVMValueRef dataPtr, LLVMValueRef metadata)
    {
        var alloca = context.Builder.BuildAlloca(TypeRef);
        var dataDst = context.Builder.BuildStructGEP2(TypeRef, alloca, 0);
        context.Builder.BuildStore(dataPtr, dataDst);
        var metaDst = context.Builder.BuildStructGEP2(TypeRef, alloca, 1);
        context.Builder.BuildStore(metadata, metaDst);
        return new ValueRefItem { Type = this, ValueRef = alloca };
    }

    /// <summary>
    /// Creates a reference (this pointer-like type) wrapping the given receiver value.
    /// For sized pointees: the receiver's ValueRef (alloca ptr) becomes the data pointer.
    /// For unsized pointees (str, slices): the receiver IS a fat pointer — its data ptr
    /// and metadata are extracted and copied into the new reference.
    /// Used by auto-ref wrapping in method dispatch and explicit &amp; expressions.
    /// </summary>
    public ValueRefItem WrapAsRef(CodeGenContext context, ValueRefItem receiver)
    {
        if (HasNonTrivialMetadata)
        {
            // Unsized: receiver is already a fat pointer {ptr, metadata}.
            // Extract both fields and build a new reference with them.
            var loaded = receiver.Type.LoadValue(context, receiver);
            var dataPtr = context.Builder.BuildExtractValue(loaded, 0);
            var metadata = context.Builder.BuildExtractValue(loaded, 1);
            return BuildFatPointer(context, dataPtr, metadata);
        }

        // Sized: receiver.ValueRef is a pointer to the value — use it as the data pointer.
        return BuildFatPointer(context, receiver.ValueRef,
            LLVMValueRef.CreateConstNull(MetadataTypeRef));
    }
}
