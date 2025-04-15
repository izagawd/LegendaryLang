using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public class TupleType : CustomType
{




    public override LLVMTypeRef TypeRef { get; protected set; }
    public override LangPath Ident => new TupleLangPath(OtherTypes.Select(i => i.Ident));
    public override Token LookUpToken { get; }
    public override void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }
    public ImmutableArray<Type> OtherTypes { get; }
    public TupleType(IEnumerable<Type> otherTypes)
    {
        OtherTypes = otherTypes.ToImmutableArray();
    }
    public override void CodeGen(CodeGenContext context)

    {
        TypeRef = LLVMTypeRef.CreateStruct(
            OtherTypes.Select(i => (context.GetRefItemFor(i.Ident) as TypeRefItem).TypeRef).ToArray(),
            false);
        context.AddToTop(new TupleLangPath(OtherTypes.Select(i => i.Ident)),
            new TypeRefItem()
            {
                Type = this,
            });
    }

    public override int Priority { get; }
    public override ImmutableArray<LangPath> ComposedTypes => OtherTypes.Select(i => i.Ident).ToImmutableArray();
}