using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public abstract class PrimitiveType : Type
{
    public override int GetPrimitivesCompositeCount(CodeGenContext context)
    {
        return 1;
    }

    public override void AssignTo(CodeGenContext codeGenContext, VariableRefItem value, VariableRefItem ptr)
    {
         codeGenContext.Builder.BuildStore(value.LoadValForRetOrArg(codeGenContext), ptr.ValueRef );
    }

    public abstract string Name { get; }

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
        context.AddToTop(Ident,new TypeRefItem()
        {
            Type = this,

        });
    }
    

    public override int Priority => -1;
    public override LangPath Ident =>LangPath.PrimitivePath.Append([Name]);
}
public class I32 : PrimitiveType
{

    

        public override Token LookUpToken { get; }
        public override void Analyze(SemanticAnalyzer analyzer)
        {
            throw new NotImplementedException();
        }
        

        public override LLVMTypeRef TypeRef
        {
            get => LLVMTypeRef.Int32;
            protected set => throw new NotImplementedException();
        }

        public override int Priority => 1;
        public override string Name => "i32";
}