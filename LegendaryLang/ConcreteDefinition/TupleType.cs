﻿using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class TupleType : CustomType
{
    public TupleType(IEnumerable<Type> types) : base(new TupleTypeDefinition(types))
    {
    }

    public ImmutableArray<Type> OtherTypes => ((TupleTypeDefinition)TypeDefinition).OtherTypes;

    public override LLVMTypeRef TypeRef { get; protected set; }
    public override LangPath TypePath => new TupleLangPath(OtherTypes.Select(i => i.TypePath));
    public override string Name => $"({string.Join(',', OtherTypes.Select(i => i.Name))})";

    public override void CodeGen(CodeGenContext context)

    {
        TypeRef = LLVMTypeRef.CreateStruct(
            OtherTypes.Select(i => (context.GetRefItemFor(i.TypePath) as TypeRefItem).TypeRef).ToArray(),
            false);
    }
}