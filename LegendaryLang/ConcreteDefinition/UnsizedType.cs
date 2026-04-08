using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

/// <summary>
/// Concrete type marker for unsized types (str, [T]).
/// These types have no fixed LLVM representation — they only exist behind fat pointers.
/// Stores the element LLVM type (i8 for str, T's type for [T]) and the metadata LLVM type
/// (usize for both str and slices) so that fat pointer construction can use them.
/// </summary>
public class UnsizedType : Type
{
    /// <summary>The LLVM type of each element (i8 for str, element type for [T]).</summary>
    public LLVMTypeRef ElementTypeRef { get; }

    /// <summary>The LLVM type of the fat pointer metadata (i64/usize for str and slices).</summary>
    public LLVMTypeRef MetadataTypeRef { get; }

    public UnsizedType(TypeDefinition definition, LLVMTypeRef elementTypeRef, LLVMTypeRef metadataTypeRef)
        : base(definition)
    {
        ElementTypeRef = elementTypeRef;
        MetadataTypeRef = metadataTypeRef;
    }

    // UnsizedType has no real LLVM type — use an empty struct as a placeholder.
    // This should never be used to alloca or store values directly.
    public override LLVMTypeRef TypeRef
    {
        get => LLVMTypeRef.CreateStruct([], false);
        protected set { }
    }

    public override LangPath TypePath => TypeDefinition.TypePath;
    public override string Name => TypeDefinition.Name;
    public override int GetPrimitivesCompositeCount(CodeGenContext context) => 0;

    public override void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr)
        => throw new InvalidOperationException($"Cannot assign unsized type '{Name}' — it can only exist behind a pointer.");

    public override LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
        => throw new InvalidOperationException($"Cannot load unsized type '{Name}' — it can only exist behind a pointer.");
}
