using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class EnumType : Type
{
    public EnumType(EnumTypeDefinition definition, LLVMTypeRef typeRef, LangPath? monomorphizedTypePath = null)
        : base(definition)
    {
        TypeRef = typeRef;
        _monomorphizedTypePath = monomorphizedTypePath;
    }

    private readonly LangPath? _monomorphizedTypePath;

    public EnumTypeDefinition EnumTypeDefinition => (EnumTypeDefinition)TypeDefinition;
    public override LLVMTypeRef TypeRef { get; protected set; }
    public override LangPath TypePath => _monomorphizedTypePath ?? TypeDefinition.TypePath;
    public override string Name => TypeDefinition.Name;

    /// <summary>
    /// Resolved variant info from CreateRefDefinition: (variant, resolved field concrete types)
    /// </summary>
    public ImmutableArray<(EnumVariant variant, ImmutableArray<Type> fieldTypes)>? ResolvedVariants { get; set; }

    public bool HasPayloads { get; set; }
    public ulong MaxPayloadBytes { get; set; }

    public EnumVariant? GetVariant(string name) => EnumTypeDefinition.GetVariant(name);

    public (EnumVariant variant, ImmutableArray<Type> fieldTypes)? GetResolvedVariant(string name)
    {
        if (ResolvedVariants == null) return null;
        foreach (var rv in ResolvedVariants)
            if (rv.variant.Name == name) return rv;
        return null;
    }

    public override void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr)
    {
        if (value.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
        {
            // Pointer to pointer — copy tag
            var srcTag = codeGenContext.Builder.BuildStructGEP2(TypeRef, value.ValueRef, 0);
            var dstTag = codeGenContext.Builder.BuildStructGEP2(TypeRef, ptr.ValueRef, 0);
            var tagVal = codeGenContext.Builder.BuildLoad2(LLVMTypeRef.Int32, srcTag);
            codeGenContext.Builder.BuildStore(tagVal, dstTag);

            // Copy payload byte-by-byte via i8 pointer cast
            if (HasPayloads && MaxPayloadBytes > 0)
            {
                var srcPayload = codeGenContext.Builder.BuildStructGEP2(TypeRef, value.ValueRef, 1);
                var dstPayload = codeGenContext.Builder.BuildStructGEP2(TypeRef, ptr.ValueRef, 1);
                for (ulong i = 0; i < MaxPayloadBytes; i++)
                {
                    var srcByte = codeGenContext.Builder.BuildGEP2(LLVMTypeRef.Int8, srcPayload,
                        [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, i, false)]);
                    var dstByte = codeGenContext.Builder.BuildGEP2(LLVMTypeRef.Int8, dstPayload,
                        [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, i, false)]);
                    var byteVal = codeGenContext.Builder.BuildLoad2(LLVMTypeRef.Int8, srcByte);
                    codeGenContext.Builder.BuildStore(byteVal, dstByte);
                }
            }
        }
        else
        {
            // Value (aggregate) to pointer — extract and store tag
            var tagVal = codeGenContext.Builder.BuildExtractValue(value.ValueRef, 0);
            var dstTag = codeGenContext.Builder.BuildStructGEP2(TypeRef, ptr.ValueRef, 0);
            codeGenContext.Builder.BuildStore(tagVal, dstTag);

            if (HasPayloads && MaxPayloadBytes > 0)
            {
                var payloadVal = codeGenContext.Builder.BuildExtractValue(value.ValueRef, 1);
                var dstPayload = codeGenContext.Builder.BuildStructGEP2(TypeRef, ptr.ValueRef, 1);
                // Store payload byte-by-byte
                for (ulong i = 0; i < MaxPayloadBytes; i++)
                {
                    var byteVal = codeGenContext.Builder.BuildExtractValue(payloadVal, (uint)i);
                    var dstByte = codeGenContext.Builder.BuildGEP2(LLVMTypeRef.Int8, dstPayload,
                        [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, i, false)]);
                    codeGenContext.Builder.BuildStore(byteVal, dstByte);
                }
            }
        }
    }

    public override int GetPrimitivesCompositeCount(CodeGenContext context) => 1;

    public override unsafe LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
    {
        if (valueRef.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
        {
            // Build aggregate field-by-field to avoid LLVM issues with loading array-containing structs
            LLVMValueRef aggr = LLVM.GetUndef(TypeRef);

            // Load tag (field 0)
            var tagPtr = context.Builder.BuildStructGEP2(TypeRef, valueRef.ValueRef, 0);
            var tagVal = context.Builder.BuildLoad2(LLVMTypeRef.Int32, tagPtr);
            aggr = context.Builder.BuildInsertValue(aggr, tagVal, 0);

            // Load payload (field 1) byte-by-byte and reconstruct
            if (HasPayloads && MaxPayloadBytes > 0)
            {
                var payloadArrayType = LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)MaxPayloadBytes);
                LLVMValueRef payloadAggr = LLVM.GetUndef(payloadArrayType);
                var payloadPtr = context.Builder.BuildStructGEP2(TypeRef, valueRef.ValueRef, 1);
                for (ulong i = 0; i < MaxPayloadBytes; i++)
                {
                    var bytePtr = context.Builder.BuildGEP2(LLVMTypeRef.Int8, payloadPtr,
                        [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, i, false)]);
                    var byteVal = context.Builder.BuildLoad2(LLVMTypeRef.Int8, bytePtr);
                    payloadAggr = context.Builder.BuildInsertValue(payloadAggr, byteVal, (uint)i);
                }
                aggr = context.Builder.BuildInsertValue(aggr, payloadAggr, 1);
            }

            return aggr;
        }
        return valueRef.ValueRef;
    }
}
