using LegendaryLang.Definitions.Types;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

/// <summary>
/// Pointer-sized unsigned integer type.
/// Matches the target's pointer width at runtime — i64 on 64-bit, i32 on 32-bit.
/// </summary>
public class UsizeType : PrimitiveType
{
    /// <summary>
    /// The LLVM integer type matching pointer size. Intrinsics reference this directly.
    /// </summary>
    public static readonly LLVMTypeRef LLVMType =
        Environment.Is64BitProcess ? LLVMTypeRef.Int64 : LLVMTypeRef.Int32;

    public UsizeType(PrimitiveTypeDefinition definition) : base(definition)
    {
    }

    public override LLVMTypeRef TypeRef
    {
        get => LLVMType;
        protected set => throw new NotImplementedException();
    }

    public override string Name => "usize";
}
