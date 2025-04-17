using System.Collections.Immutable;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Types;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class StructType : CustomType
{
    public VariableDefinition? GetField(string fieldName)
    {
        return StructTypeDefinition.GetField(fieldName);
    }
    public uint GetIndexOfField(string fieldName)
    {
        return StructTypeDefinition.GetIndexOfField(fieldName);
    }
    public StructTypeDefinition StructTypeDefinition => (StructTypeDefinition) TypeDefinition;
    public ImmutableArray<VariableDefinition> Fields => StructTypeDefinition.Fields;
    public override LLVMTypeRef TypeRef { get; protected set; }
    public override LangPath TypePath => TypeDefinition.TypePath;
    public override string Name => TypeDefinition.Name;

    public unsafe override void CodeGen(CodeGenContext context)
    {
 
        // 1. Check if the struct is already generated (avoid double-generation)
        if (TypeRef.Handle != IntPtr.Zero)
            return;

        // 2. Create an opaque (named but incomplete) LLVM struct first
        var llvmStructName = (this as IDefinition).FullPath.ToString();// e.g., "my.module.MyStruct"
        TypeRef = LLVM.StructCreateNamed(context.Module.Context, llvmStructName.ToCString());

        // 3. Generate LLVM types for the struct fields
        var fieldTypes = Fields.Select(field =>
        {
            var idk = context.GetRefItemFor(field.TypePath);
            return (idk as TypeRefItem).Type;
        }).ToArray();

        fixed (void* ptr = fieldTypes.Select(i => i.TypeRef).ToArray())
        {        
            LLVM.StructSetBody(TypeRef,(LLVMSharp.Interop.LLVMOpaqueType**) ptr,(uint) fieldTypes.Length, 0);
            
        }
        // 4. Set the body of the opaque struct



    }
    public StructType(StructTypeDefinition definition) : base(definition)
    {
    }
}