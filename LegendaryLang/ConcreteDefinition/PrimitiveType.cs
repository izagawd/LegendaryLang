using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public abstract class PrimitiveType : Type
{
    public PrimitiveType(PrimitiveTypeDefinition definition) : base(definition){}


    public override int GetPrimitivesCompositeCount(CodeGenContext context)
    {
        return 1;
    }

    public override void AssignTo(CodeGenContext codeGenContext, VariableRefItem value, VariableRefItem ptr)
    {
        codeGenContext.Builder.BuildStore(value.LoadValForRetOrArg(codeGenContext), ptr.ValueRef );
    }



    public override LLVMValueRef LoadValueForRetOrArg(CodeGenContext context, VariableRefItem variableRef)
    {
        if (variableRef.ValueRef.TypeOf.Kind != LLVMTypeKind.LLVMPointerTypeKind)
        {
            return variableRef.ValueRef;
        }
        return context.Builder.BuildLoad2(TypeRef, variableRef.ValueRef);
    
    }

    public override LLVMValueRef AssignToStack(CodeGenContext context, VariableRefItem dataRefItem)
    {
        LLVMValueRef value;
        if (dataRefItem.ValueRef.TypeOf.Kind != LLVMTypeKind.LLVMPointerTypeKind)
        {
            value = dataRefItem.ValueRef;
            
        }
        else
        {
            value= context.Builder.BuildLoad2(TypeRef, dataRefItem.ValueRef);
        }
        var allocated = context.Builder.BuildAlloca(TypeRef);
        context.Builder.BuildStore(value, allocated);
        return allocated;
    }

    public override void CodeGen(CodeGenContext context)
    {

    }
    


    public override LangPath TypePath =>TypeDefinition.TypePath;

}

