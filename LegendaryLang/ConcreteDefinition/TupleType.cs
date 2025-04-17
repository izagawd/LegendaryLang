using System.Collections.Immutable;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Types;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class TupleType : CustomType
{
    public ImmutableArray<Type> OtherTypes => ((TupleTypeDefinition) TypeDefinition).OtherTypes;
    public override void CodeGen(CodeGenContext context)

    {
      
        TypeRef = LLVMTypeRef.CreateStruct(
            OtherTypes.Select(i => (context.GetRefItemFor(i.TypePath) as TypeRefItem).TypeRef).ToArray(),
            false);

    }

    public override LLVMTypeRef TypeRef { get; protected set; }
    public override LangPath TypePath => new TupleLangPath(OtherTypes.Select(i => i.TypePath));
    public override string Name => $"({string.Join(',', OtherTypes.Select(i => i.Name))})";

    public TupleType(IEnumerable<Type> types) : base(new TupleTypeDefinition(types))
    {
    }
}