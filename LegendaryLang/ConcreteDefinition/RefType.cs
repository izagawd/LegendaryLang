using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class RefType : Type
{
    public Type PointingToType { get; }
    public RefTypeDefinition RefTypeDefinition { get; }
    public RefType(RefTypeDefinition definition, Type pointingToType, LLVMTypeRef typeRef) : base(definition)
    {
        RefTypeDefinition = definition;
        TypeRef = typeRef;
        PointingToType = pointingToType;
    }

    public override LLVMTypeRef TypeRef { get; protected set; }
    public override LangPath TypePath => ((NormalLangPath) RefTypeDefinition.TypePath ).Append(new NormalLangPath.GenericTypesPathSegment([PointingToType.TypePath]));
    public override string Name => RefTypeDefinition.Name;
    public override int GetPrimitivesCompositeCount(CodeGenContext context) => 1;

    public override LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
    {
        // A reference variable's ValueRef is a pointer to a stack slot holding the pointer.
        // Load the pointer value itself.
        if (valueRef.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            return context.Builder.BuildLoad2(TypeRef, valueRef.ValueRef);
        return valueRef.ValueRef;
    }

    public override void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr)
    {
        // Load the pointer value from the source, store into the destination
        var loaded = LoadValue(codeGenContext, value);
        codeGenContext.Builder.BuildStore(loaded, ptr.ValueRef);
    }

    public override LLVMValueRef AssignToStack(CodeGenContext context, ValueRefItem dataRefItem)
    {
        // Allocate a stack slot for the pointer and store the pointer value
        var alloca = context.Builder.BuildAlloca(TypeRef);
        var ptrVal = LoadValue(context, dataRefItem);
        context.Builder.BuildStore(ptrVal, alloca);
        return alloca;
    }
}
