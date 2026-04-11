using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Definitions.Types;

public enum RefKind
{
    Shared,   // &T
    Mut,      // &mut T
}

public static class RefKindParser
{
    /// <summary>
    /// Parse a RefKind from the token stream. Consumes mut/const/mut if present,
    /// defaults to Shared.
    /// </summary>
    public static RefKind Parse(Parser parser)
    {
        if (parser.Peek() is MutToken)
        {
            parser.Pop();
            return RefKind.Mut;
        }
        return RefKind.Shared;
    }
}

/// <summary>
/// Shared base for RefTypeDefinition and RawPtrTypeDefinition.
/// Both are single-generic-arg pointer types with identical CreateRefDefinition
/// and GetGenericArguments logic.
/// </summary>
public abstract class PointerTypeDefinitionBase : TypeDefinition
{
    public override Token Token => null;

    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        var pointeePath = genericArguments[0];
        var pointingToTypeRef = (TypeRefItem)context.GetRefItemFor(pointeePath);
        var pointingToType = pointingToTypeRef.Type;

        // Unsized pointee → fat pointer: {ptr, metadata_type}
        if (pointingToType is UnsizedType unsized)
        {
            var fatPtrTypeRef = LLVMTypeRef.CreateStruct(
                [LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), unsized.MetadataTypeRef], false);
            return new TypeRefItem
            {
                Type = CreateConcreteType(pointingToType, fatPtrTypeRef,
                    elementTypeRef: unsized.ElementTypeRef, metadataTypeRef: unsized.MetadataTypeRef)
            };
        }

        // Sized pointee → thin pointer: {ptr, ()} where () is the zero-sized empty-struct metadata.
        // Using the same {ptr, metadata} struct layout as fat pointers makes all pointer-like
        // types data-driven — no IsFat branches in load/store/extract paths.
        var emptyMetaTypeRef = LLVMTypeRef.CreateStruct([], false);
        var thinTypeRef = LLVMTypeRef.CreateStruct(
            [LLVMTypeRef.CreatePointer(pointingToTypeRef.TypeRef, 0), emptyMetaTypeRef], false);
        return new TypeRefItem
        {
            Type = CreateConcreteType(pointingToType, thinTypeRef, metadataTypeRef: emptyMetaTypeRef)
        };
    }

    protected abstract ConcreteDefinition.Type CreateConcreteType(
        ConcreteDefinition.Type pointingToType, LLVMTypeRef typeRef,
        LLVMTypeRef? elementTypeRef = null, LLVMTypeRef? metadataTypeRef = null);

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path is NormalLangPath nlp && nlp.GetFrontGenerics().Length > 0)
            return nlp.GetFrontGenerics();
        return null;
    }

    public override void Analyze(SemanticAnalyzer analyzer) { }
}

public class RefTypeDefinition : PointerTypeDefinitionBase
{
    public RefTypeDefinition(RefKind kind)
    {
        Kind = kind;
    }

    public static NormalLangPath GetRefModule()
    {
        return new NormalLangPath(null, ["Std", "Reference"]);
    }

    public static string GetRefName(RefKind kind) => kind switch
    {
        RefKind.Shared => "shared",
        RefKind.Mut => "mut_",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public RefKind Kind { get; }

    /// <summary>
    /// Checks if a type path represents any reference type (&amp;, &amp;mut).
    /// </summary>
    public static bool IsReferenceType(LangPath? typePath)
    {
        return typePath is NormalLangPath nlp
               && nlp.Contains(GetRefModule());
    }

    /// <summary>
    /// Extracts the RefKind from a type path that contains a reference module segment.
    /// Used across deref, method dispatch, and borrow checking.
    /// Returns Shared as default if no specific kind is found.
    /// </summary>
    public static RefKind ExtractRefKindFromPath(LangPath? typePath)
    {
        if (typePath is NormalLangPath nlp)
            foreach (RefKind rk in Enum.GetValues<RefKind>())
                if (nlp.PathSegments.Any(s => s is NormalLangPath.NormalPathSegment nps && nps.Text == GetRefName(rk)))
                    return rk;
        return RefKind.Shared;
    }

    /// <summary>
    /// Like ExtractRefKindFromPath but returns null if the path doesn't contain a reference module.
    /// Used for auto-ref detection on self parameters.
    /// </summary>
    public static RefKind? TryExtractRefKindFromPath(LangPath? typePath)
    {
        if (typePath is NormalLangPath nlp && nlp.Contains(GetRefModule()))
            return ExtractRefKindFromPath(typePath);
        return null;
    }

    public override string Name => GetRefName(Kind);
    public override NormalLangPath Module => GetRefModule();

    protected override ConcreteDefinition.Type CreateConcreteType(
        ConcreteDefinition.Type pointingToType, LLVMTypeRef typeRef,
        LLVMTypeRef? elementTypeRef, LLVMTypeRef? metadataTypeRef)
        => new ConcreteDefinition.RefType(this, pointingToType, typeRef, elementTypeRef, metadataTypeRef);
}
