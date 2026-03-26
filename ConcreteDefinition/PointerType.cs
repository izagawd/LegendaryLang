using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class PointerType : Type
{
    public Type PointingToType { get; }
    public PointerTypeDefinition PointerTypeDefinition { get; }
    public PointerType(PointerTypeDefinition definition, Type pointingToType, LLVMTypeRef typeRef) : base(definition)
    {
        PointerTypeDefinition = definition;
        TypeRef = typeRef;
        PointingToType = pointingToType;
    }

    
    
    public override LLVMTypeRef TypeRef { get; protected set; }
    public override LangPath TypePath => ((NormalLangPath) PointerTypeDefinition.TypePath ).Append(new NormalLangPath.GenericTypesPathSegment([PointingToType.TypePath]));
    public override string Name => PointerTypeDefinition.Name;
    public override int GetPrimitivesCompositeCount(CodeGenContext context)
    {
        return 0;
    }

    public override LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
    {
        throw new NotImplementedException();
    }

    public override void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr)
    {
        throw new NotImplementedException();
    }
}