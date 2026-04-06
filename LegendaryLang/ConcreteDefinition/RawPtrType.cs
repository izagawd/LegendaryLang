using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

/// <summary>
/// Concrete raw pointer type. At the LLVM level, identical to RefType (an opaque pointer).
/// Semantically different: raw pointers are always Copy and have no borrow checking.
/// </summary>
public class RawPtrType : Type
{
    public Type PointingToType { get; }
    public RawPtrTypeDefinition RawPtrTypeDefinition { get; }

    public RawPtrType(RawPtrTypeDefinition definition, Type pointingToType, LLVMTypeRef typeRef) : base(definition)
    {
        RawPtrTypeDefinition = definition;
        TypeRef = typeRef;
        PointingToType = pointingToType;
    }

    public override LLVMTypeRef TypeRef { get; protected set; }

    public override LangPath TypePath =>
        ((NormalLangPath)RawPtrTypeDefinition.TypePath).AppendGenerics([PointingToType.TypePath]);

    public override string Name => RawPtrTypeDefinition.Name;
    public override int GetPrimitivesCompositeCount(CodeGenContext context) => 1;

    public override LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef)
    {
        // A raw pointer variable's ValueRef is a pointer to a stack slot holding the pointer.
        // Load the pointer value itself.
        if (valueRef.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            return context.Builder.BuildLoad2(TypeRef, valueRef.ValueRef);
        return valueRef.ValueRef;
    }

    public override void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr)
    {
        var loaded = LoadValue(codeGenContext, value);
        codeGenContext.Builder.BuildStore(loaded, ptr.ValueRef);
    }

    public override LLVMValueRef AssignToStack(CodeGenContext context, ValueRefItem dataRefItem)
    {
        var alloca = context.Builder.BuildAlloca(TypeRef);
        var ptrVal = LoadValue(context, dataRefItem);
        context.Builder.BuildStore(ptrVal, alloca);
        return alloca;
    }
}
