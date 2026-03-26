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


    public ImmutableArray<LangPath> ComposedTypesAsPaths => ((ComposableTypeDefinition)TypeDefinition).ComposedTypes;

    public override void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr)
    {
        for (var i = 0; i < ComposedTypesAsPaths.Length; i++)
        {
            var composed = ComposedTypesAsPaths[i];
            var composedType = codeGenContext.GetRefItemFor(composed) as TypeRefItem;
            LLVMValueRef fieldValuePtr;
            var fieldPtrPtr = codeGenContext.Builder.BuildStructGEP2(TypeRef, ptr.ValueRef,
                (uint)i);
            if (value.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            {
                fieldValuePtr = codeGenContext.Builder.BuildStructGEP2(TypeRef, value.ValueRef,
                    (uint)i);

                composedType.Type.AssignTo(codeGenContext, new ValueRefItem
                    {
                        ValueRef = fieldValuePtr,
                        Type = composedType.Type
                    },
                    new ValueRefItem
                    {
                        ValueRef = fieldPtrPtr,
                        Type = composedType.Type
                    });
            }
            else
            {
                var toExtract = codeGenContext.Builder.BuildExtractValue(value.ValueRef, (uint)i);
                composedType.Type.AssignTo(codeGenContext,
                    new ValueRefItem
                    {
                        ValueRef = toExtract,
                        Type = composedType.Type
                    }, new ValueRefItem
                    {
                        ValueRef = fieldPtrPtr,
                        Type = composedType.Type
                    });
            }
        }
    }


    public override int GetPrimitivesCompositeCount(CodeGenContext context)
    {
        return ComposedTypesAsPaths.Select(i =>
                (context.GetRefItemFor(i) as TypeRefItem).Type.GetPrimitivesCompositeCount(context))
            .Sum();
    }

    public override unsafe LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
    {
        if (GetPrimitivesCompositeCount(context) > 0)
        {
            LLVMValueRef aggr = LLVM.GetUndef(TypeRef);
            for (var i = 0; i < ComposedTypesAsPaths.Length; i++)
            {
                var composed = ComposedTypesAsPaths[i];
                var type = context.GetRefItemFor(composed) as TypeRefItem;
                var otherComposed = context.Builder.BuildStructGEP2(TypeRef, valueRef.ValueRef, (uint)i);
                var refIt = new ValueRefItem
                {
                    ValueRef = otherComposed,
                    Type = type.Type
                };

                if (aggr == null)
                    aggr = context.Builder.BuildExtractValue(aggr, (uint)i);
                else
                    aggr = context.Builder.BuildInsertValue(aggr, type.Type.LoadValue(context, refIt), (uint)i);
            }

            return aggr;
        }

        return LLVM.GetUndef(TypeRef);
    }
}