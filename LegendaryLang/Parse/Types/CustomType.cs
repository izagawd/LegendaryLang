using System.Collections.Immutable;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public abstract class CustomType : Type
{
    public override void AssignTo(CodeGenContext codeGenContext, VariableRefItem value, VariableRefItem ptr)
    {
        for (int i = 0; i < ComposedTypes.Length; i++)
        {
            var composed = ComposedTypes[i];
            var composedType = codeGenContext.GetRefItemFor(composed) as TypeRefItem;
            LLVMValueRef fieldValuePtr;
            var fieldPtrPtr = codeGenContext.Builder.BuildStructGEP2(TypeRef,ptr.ValueRef,
               (uint) i);
            if (value.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            {
                fieldValuePtr = codeGenContext.Builder.BuildStructGEP2(TypeRef, value.ValueRef,
                    (uint) i );

                composedType.Type.AssignTo(codeGenContext, new VariableRefItem()
                    {
                        ValueRef = fieldValuePtr,
                        Type = composedType.Type
                    },
                    new VariableRefItem()
                    {
                        ValueRef = fieldPtrPtr,
                        Type = composedType.Type
                    });


            }
            else
            {

                var toExtract = codeGenContext.Builder.BuildExtractValue(value.ValueRef,(uint)i);
                composedType.Type.AssignTo(codeGenContext,
                    new VariableRefItem()
                    {
                        ValueRef = toExtract,
                        Type = composedType.Type
                    }, new VariableRefItem()
                    {
                        ValueRef = fieldPtrPtr,
                        Type = composedType.Type
                    });
            }


        }
    
    }


    public override int GetPrimitivesCompositeCount(CodeGenContext context)
    {
        return  ComposedTypes.Select(i => (context.GetRefItemFor(i) as TypeRefItem).Type.GetPrimitivesCompositeCount(context))
            .Sum();
    }
    public unsafe override LLVMValueRef LoadValueForRetOrArg(CodeGenContext context,VariableRefItem variableRef)
    {
        
        if (GetPrimitivesCompositeCount(context) > 0)
        {

            LLVMValueRef aggr = LLVM.GetUndef(TypeRef);
            for (int i = 0; i < ComposedTypes.Length; i++)
            {
                var composed = ComposedTypes[i];
                var type = context.GetRefItemFor(composed) as TypeRefItem;
                var otherComposed = context.Builder.BuildStructGEP2(TypeRef, variableRef.ValueRef, (uint)i);
                var refIt = new VariableRefItem()
                {
                    ValueRef = otherComposed,
                    Type = type.Type
                };
              
                if (aggr == null)
                {
                    aggr = context.Builder.BuildExtractValue(aggr,(uint) i);
                }
                else
                {
                    aggr = context.Builder.BuildInsertValue(aggr, type.Type.LoadValueForRetOrArg(context,refIt) ,(uint) i);
                }
         
                
            }

            return aggr;
        }

        return  LLVM.GetUndef(TypeRef);
        
      
    }
    public abstract ImmutableArray<LangPath> ComposedTypes { get; }
}