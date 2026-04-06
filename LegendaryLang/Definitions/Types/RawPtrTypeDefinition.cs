using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Definitions.Types;

/// <summary>
/// Raw pointer types: *shared T, *const T, *mut T, *uniq T.
/// Unlike references, raw pointers have no borrow checking and are always Copy.
/// They share the same RefKind enum as references for consistency.
/// </summary>
public class RawPtrTypeDefinition : TypeDefinition
{
    public RawPtrTypeDefinition(RefKind kind)
    {
        Kind = kind;
    }

    public static NormalLangPath GetRawPtrModule()
    {
        return new NormalLangPath(null, ["Std", "Rawptr"]);
    }

    public static string GetRawPtrName(RefKind kind)
    {
        return kind switch
        {
            RefKind.Shared => "shared",
            RefKind.Const => "const_",
            RefKind.Mut => "mut_",
            RefKind.Uniq => "uniq",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public RefKind Kind { get; }

    public override string Name => GetRawPtrName(Kind);
    public override NormalLangPath Module => GetRawPtrModule();

    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        var pointingToType = ((TypeRefItem)context.GetRefItemFor(genericArguments[0]));
        var typeRef = LLVMTypeRef.CreatePointer(pointingToType.TypeRef, 0);

        return new TypeRefItem()
        {
            Type = new ConcreteDefinition.RawPtrType(this, pointingToType.Type, typeRef),
        };
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path is NormalLangPath nlp && nlp.GetFrontGenerics().Length > 0)
            return nlp.GetFrontGenerics();
        return null;
    }

    public override Token Token { get; }
    public override void Analyze(SemanticAnalyzer analyzer) { }
}
