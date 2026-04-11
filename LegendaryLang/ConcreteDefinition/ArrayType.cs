using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class ArrayType : Type
{
    public Type ElementType { get; }
    public uint Size { get; }
    private readonly LangPath _typePath;

    public ArrayType(ArrayTypeDefinition definition, Type elementType, uint size,
        LLVMTypeRef typeRef, LangPath typePath) : base(definition)
    {
        ElementType = elementType;
        Size = size;
        TypeRef = typeRef;
        _typePath = typePath;
    }

    public override LLVMTypeRef TypeRef { get; protected set; }
    public override LangPath TypePath => _typePath;
    public override string Name => _typePath.ToString();

    private LLVMValueRef[] GepIndices(uint index) =>
    [
        LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false),
        LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, index, false)
    ];

    public override LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
    {
        if (valueRef.ValueRef.TypeOf.Kind != LLVMTypeKind.LLVMPointerTypeKind)
            return valueRef.ValueRef;

        LLVMValueRef aggr;
        unsafe { aggr = LLVM.GetUndef(TypeRef); }
        for (uint i = 0; i < Size; i++)
        {
            var indices = GepIndices(i);
            var elemPtr = context.Builder.BuildInBoundsGEP2(
                TypeRef, valueRef.ValueRef, indices, $"arr_load_{i}");
            var refItem = new ValueRefItem { ValueRef = elemPtr, Type = ElementType };
            aggr = context.Builder.BuildInsertValue(aggr, ElementType.LoadValue(context, refItem), i);
        }
        return aggr;
    }

    public override void AssignTo(CodeGenContext context, ValueRefItem value, ValueRefItem ptr)
    {
        for (uint i = 0; i < Size; i++)
        {
            var indices = GepIndices(i);
            var destPtr = context.Builder.BuildInBoundsGEP2(
                TypeRef, ptr.ValueRef, indices, $"arr_assign_{i}");

            if (value.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            {
                var srcPtr = context.Builder.BuildInBoundsGEP2(
                    TypeRef, value.ValueRef, indices, $"arr_src_{i}");
                ElementType.AssignTo(context,
                    new ValueRefItem { ValueRef = srcPtr, Type = ElementType },
                    new ValueRefItem { ValueRef = destPtr, Type = ElementType });
            }
            else
            {
                var extracted = context.Builder.BuildExtractValue(value.ValueRef, i);
                if (ElementType is CustomType)
                {
                    ElementType.AssignTo(context,
                        new ValueRefItem { ValueRef = extracted, Type = ElementType },
                        new ValueRefItem { ValueRef = destPtr, Type = ElementType });
                }
                else
                {
                    context.Builder.BuildStore(extracted, destPtr);
                }
            }
        }
    }

    public override int GetPrimitivesCompositeCount(CodeGenContext context)
    {
        return ElementType.GetPrimitivesCompositeCount(context) * (int)Size;
    }
}
