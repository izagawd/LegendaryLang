using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public class TupleType : Type
{


    public override int GetPrimitivesCompositeCount(CodeGenContext context)
    {
        return  OtherTypes.Select(i => i.GetPrimitivesCompositeCount(context))
            .Sum();
    }

    public override LLVMValueRef LoadValueForRetOrArg(CodeGenContext context, VariableRefItem variableRef)
    {
        throw new NotImplementedException();
    }

    public override LLVMValueRef AssignTo(CodeGenContext codeGenContext, VariableRefItem value, VariableRefItem ptr)
    {
        throw new NotImplementedException();
    }

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
}