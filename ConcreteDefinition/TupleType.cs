using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class TupleType : CustomType
{
    public TupleType(TupleTypeDefinition definition, IEnumerable<Type> types, LLVMTypeRef typeRef) : base(definition)
    {
        TypeRef = typeRef;
     
    }

  

    public override LLVMTypeRef TypeRef { get; protected set; }
    public override LangPath TypePath => new TupleLangPath(ComposedTypesAsPaths);
    public override string Name => $"Tuple{TypePath}";

    

}