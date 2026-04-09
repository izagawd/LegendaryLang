using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public abstract class CustomType : Type
{

    public CustomType(ComposableTypeDefinition definition) : base(definition)
    {
    }

    /// <summary>
    /// Resolved concrete types for each composed field. Set during CreateRefDefinition
    /// so that generic params (like T) are already resolved to their concrete types (like i32).
    /// Falls back to path-based lookup if not set.
    /// </summary>
    public ImmutableArray<Type>? ResolvedFieldTypes { get; init; }

    public ImmutableArray<LangPath> ComposedTypesAsPaths => ((ComposableTypeDefinition)TypeDefinition).ComposedTypes;

    private Type? GetFieldType(CodeGenContext context, int index)
    {
        if (ResolvedFieldTypes != null && index < ResolvedFieldTypes.Value.Length)
            return ResolvedFieldTypes.Value[index];
        var refItem = context.GetRefItemFor(ComposedTypesAsPaths[index]) as TypeRefItem;
        return refItem?.Type;
    }

    private int FieldCount => ResolvedFieldTypes?.Length ?? ComposedTypesAsPaths.Length;

    public override void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr)
    {
        for (var i = 0; i < FieldCount; i++)
        {
            var composedType = GetFieldType(codeGenContext, i);
            if (composedType == null) continue;
            var fieldPtrPtr = codeGenContext.Builder.BuildStructGEP2(TypeRef, ptr.ValueRef,
                (uint)i);
            if (value.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            {
                var fieldValuePtr = codeGenContext.Builder.BuildStructGEP2(TypeRef, value.ValueRef,
                    (uint)i);

                composedType.AssignTo(codeGenContext, new ValueRefItem
                    {
                        ValueRef = fieldValuePtr,
                        Type = composedType
                    },
                    new ValueRefItem
                    {
                        ValueRef = fieldPtrPtr,
                        Type = composedType
                    });
            }
            else
            {
                var toExtract = codeGenContext.Builder.BuildExtractValue(value.ValueRef, (uint)i);
                // For nested struct types, recursively decompose via extractvalue.
                // For leaf types (primitives, references), store the extracted value directly.
                // We can't use AssignTo for leaf types because their LoadValue methods
                // check TypeOf.Kind == PointerTypeKind to decide whether to dereference,
                // but an extracted value IS the raw value, not an alloca pointer to it.
                if (composedType is CustomType)
                {
                    composedType.AssignTo(codeGenContext, new ValueRefItem
                        {
                            ValueRef = toExtract,
                            Type = composedType
                        },
                        new ValueRefItem
                        {
                            ValueRef = fieldPtrPtr,
                            Type = composedType
                        });
                }
                else
                {
                    codeGenContext.Builder.BuildStore(toExtract, fieldPtrPtr);
                }
            }
        }
    }


    public override int GetPrimitivesCompositeCount(CodeGenContext context)
    {
        var count = 0;
        for (var i = 0; i < FieldCount; i++)
        {
            var ft = GetFieldType(context, i);
            if (ft != null) count += ft.GetPrimitivesCompositeCount(context);
        }
        return count;
    }

    public override unsafe LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
    {
        if (GetPrimitivesCompositeCount(context) > 0)
        {
            LLVMValueRef aggr = LLVM.GetUndef(TypeRef);
            for (var i = 0; i < FieldCount; i++)
            {
                var type = GetFieldType(context, i);
                if (type == null) continue;
                var otherComposed = context.Builder.BuildStructGEP2(TypeRef, valueRef.ValueRef, (uint)i);
                var refIt = new ValueRefItem
                {
                    ValueRef = otherComposed,
                    Type = type
                };
                aggr = context.Builder.BuildInsertValue(aggr, type.LoadValue(context, refIt), (uint)i);
            }

            return aggr;
        }

        return LLVM.GetUndef(TypeRef);
    }
}