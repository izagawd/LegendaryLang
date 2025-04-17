using LegendaryLang.Parse.Types;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class BoolType : PrimitiveType
{
    public BoolType(BoolTypeDefinition definition) : base(definition)
    {

    }
    public override LLVMTypeRef TypeRef
    {
        get => LLVMTypeRef.Int1;
        protected set => throw new NotImplementedException();
    }
    public override int GetPrimitivesCompositeCount(CodeGenContext context)
    {
        throw new NotImplementedException();
    }

    public override LLVMValueRef LoadValueForRetOrArg(CodeGenContext context, VariableRefItem variableRef)
    {
        throw new NotImplementedException();
    }

    public override void AssignTo(CodeGenContext codeGenContext, VariableRefItem value, VariableRefItem ptr)
    {
        throw new NotImplementedException();
    }

    public override string Name => "bool";
  
}