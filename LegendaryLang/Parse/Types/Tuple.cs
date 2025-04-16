using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public class TupleType : CustomType
{




    public override LLVMTypeRef TypeRef { get; protected set; }
    public override LangPath TypePath => new TupleLangPath(OtherTypes.Select(i => i.TypePath));
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
            OtherTypes.Select(i => (context.GetRefItemFor(i.TypePath) as TypeRefItem).TypeRef).ToArray(),
            false);
        context.AddToDeepestScope(new TupleLangPath(OtherTypes.Select(i => i.TypePath)),
            new TypeRefItem()
            {
                Type = this,
            });
    }

    public override int Priority { get; }
    public override ImmutableArray<LangPath> ComposedTypes => OtherTypes.Select(i => i.TypePath).ToImmutableArray();
}