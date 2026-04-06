using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Definitions.Types;

public enum RefKind
{
    Shared,   // &T
    Const,    // &const T
    Mut,      // &mut T
    Uniq      // &uniq T
}

public class RefTypeDefinition : TypeDefinition
{
    public RefTypeDefinition(RefKind kind)
    {
        Kind = kind;
    }

    public static NormalLangPath GetRefModule()
    {
        return new NormalLangPath(null, ["Std", "Reference"]);
    }

    public static string GetRefName(RefKind kind)
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

    /// <summary>
    /// Checks whether a type path represents a &amp;uniq reference.
    /// Used to implement automatic reborrowing: passing a &amp;uniq T to a function
    /// expecting &amp;uniq T creates a temporary reborrow instead of moving.
    /// </summary>
    public static bool IsUniqRefType(LangPath? typePath)
    {
        return typePath is NormalLangPath nlp
               && nlp.Contains(GetRefModule())
               && nlp.PathSegments.Any(s => s.ToString() == GetRefName(RefKind.Uniq));
    }

    public override string Name => GetRefName(Kind);
    public override NormalLangPath Module => GetRefModule();

    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        var pointingToType = ((TypeRefItem)context.GetRefItemFor(genericArguments[0]));
        var typeRef = LLVMTypeRef.CreatePointer(pointingToType.TypeRef, 0);

        return new TypeRefItem()
        {
            Type = new ConcreteDefinition.RefType(this, pointingToType.Type, typeRef),
        };
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path is NormalLangPath nlp && nlp.GetFrontGenerics().Length > 0)
            return nlp.GetFrontGenerics();
        return null;
    }

    public override Token Token { get; }
    public override void Analyze(SemanticAnalyzer analyzer)
    {
    }
}
