using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class StructType : CustomType
{
    public StructType(StructTypeDefinition definition, LLVMTypeRef typeRef, LangPath? monomorphizedTypePath = null) : base(definition)
    {
        TypeRef = typeRef;
        _monomorphizedTypePath = monomorphizedTypePath;
    }

    private readonly LangPath? _monomorphizedTypePath;

    public StructTypeDefinition StructTypeDefinition => (StructTypeDefinition)TypeDefinition;
    public ImmutableArray<VariableDefinition> Fields => StructTypeDefinition.Fields;
    public override LLVMTypeRef TypeRef { get; protected set; }
    public override LangPath TypePath => _monomorphizedTypePath ?? TypeDefinition.TypePath;
    public override string Name => TypeDefinition.Name;

    public VariableDefinition? GetField(string fieldName)
    {
        return StructTypeDefinition.GetField(fieldName);
    }

    public uint GetIndexOfField(string fieldName)
    {
        return StructTypeDefinition.GetIndexOfField(fieldName);
    }


}