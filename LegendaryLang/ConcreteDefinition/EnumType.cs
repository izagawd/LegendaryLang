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
        // For enums, memcpy the entire struct (tag + payload)
        if (value.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
        {
            // Pointer to pointer — copy tag
            var srcTag = codeGenContext.Builder.BuildStructGEP2(TypeRef, value.ValueRef, 0);
            var dstTag = codeGenContext.Builder.BuildStructGEP2(TypeRef, ptr.ValueRef, 0);
            var tagVal = codeGenContext.Builder.BuildLoad2(LLVMTypeRef.Int32, srcTag);
            codeGenContext.Builder.BuildStore(tagVal, dstTag);

            // Copy payload if present
            if (HasPayloads && MaxPayloadBytes > 0)
            {
                var payloadArrayType = LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)MaxPayloadBytes);
                var srcPayload = codeGenContext.Builder.BuildStructGEP2(TypeRef, value.ValueRef, 1);
                var dstPayload = codeGenContext.Builder.BuildStructGEP2(TypeRef, ptr.ValueRef, 1);
                var payloadVal = codeGenContext.Builder.BuildLoad2(payloadArrayType, srcPayload);
                codeGenContext.Builder.BuildStore(payloadVal, dstPayload);
            }
        }
        else
        {
            // Value to pointer — extract and store tag
            var tagVal = codeGenContext.Builder.BuildExtractValue(value.ValueRef, 0);
            var dstTag = codeGenContext.Builder.BuildStructGEP2(TypeRef, ptr.ValueRef, 0);
            codeGenContext.Builder.BuildStore(tagVal, dstTag);

            if (HasPayloads && MaxPayloadBytes > 0)
            {
                var payloadVal = codeGenContext.Builder.BuildExtractValue(value.ValueRef, 1);
                var dstPayload = codeGenContext.Builder.BuildStructGEP2(TypeRef, ptr.ValueRef, 1);
                codeGenContext.Builder.BuildStore(payloadVal, dstPayload);
            }
        }
    }

    public override int GetPrimitivesCompositeCount(CodeGenContext context) => 1;

    public override unsafe LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
    {
        if (valueRef.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            return context.Builder.BuildLoad2(TypeRef, valueRef.ValueRef);
        return valueRef.ValueRef;
    }
}
