using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Parse.Expressions;

public class DerefExpression : IExpression
{
    public DerefExpression(IExpression inner, Token token)
    {
        Inner = inner;
        DerefToken = token;
    }

    public IExpression Inner { get; }
    public Token DerefToken { get; }
    public IEnumerable<ISyntaxNode> Children => [Inner];
    public Token Token => DerefToken;

    public LangPath? TypePath { get; private set; }
    public bool IsTemporary => false; // always accesses a stable place:
        // - if Inner is a named variable, it's already a local alloca
        // - if Inner is a temporary smart pointer, CodeGen spills it to an
        //   anonymous local "_" before derefing, so the place is still stable

    /// <summary>
    /// Tries to extract the pointee type from a reference or raw pointer type path.
    /// Returns null if the path is not a dereferenceable type.
    /// </summary>
    public static LangPath? TryGetPointeeType(LangPath? typePath, out bool isReference, out RefKind? rawPtrKind)
    {
        isReference = false;
        rawPtrKind = null;
        if (typePath is not NormalLangPath nlp || nlp.PathSegments.Length < 3)
            return null;

        // Check for reference types: Std.Reference.kind(T)
        if (nlp.Contains(RefTypeDefinition.GetRefModule()))
        {
            isReference = true;
            var generics = nlp.GetFrontGenerics();
            return generics.Length == 1 ? generics[0] : null;
        }

        // Check for raw pointer types: std.rawptr.kind(T)
        if (nlp.Contains(RawPtrTypeDefinition.GetRawPtrModule()))
        {
            isReference = false;
            // Extract the raw pointer kind from the path
            foreach (RefKind kind in Enum.GetValues<RefKind>())
                if (nlp.PathSegments.Any(s => s is NormalLangPath.NormalPathSegment nps && nps.Text == RawPtrTypeDefinition.GetRawPtrName(kind)))
                {
                    rawPtrKind = kind;
                    break;
                }
            var generics = nlp.GetFrontGenerics();
            return generics.Length == 1 ? generics[0] : null;
        }

        return null;
    }

    // Keep backward-compatible overload
    public static LangPath? TryGetPointeeType(LangPath? typePath, out bool isReference)
    {
        return TryGetPointeeType(typePath, out isReference, out _);
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        Inner.Analyze(analyzer);

        var pointeeType = TryGetPointeeType(Inner.TypePath, out var isReference, out var ptrKind);
        if (pointeeType != null)
        {
            TypePath = pointeeType;

            if (isReference)
            {
                // Reference deref — capability comes from the reference kind
                SourceDerefKind = RefTypeDefinition.ExtractRefKindFromPath(Inner.TypePath);

                // Flag non-Copy deref ONLY for value types.
                // If the pointee is itself a reference or raw pointer, deref just accesses it
                // (not a move). e.g., *self where self: &&uniq T → produces &uniq T (fine).
                var isPointeeRef = TryGetPointeeType(TypePath, out _, out _) != null;
                if (!isPointeeRef && !analyzer.IsTypeCopy(TypePath))
                    IsNonCopyRefDeref = true;
            }
            else if (ptrKind != null)
            {
                // Raw pointer deref — compiler magic, kind maps directly to capability
                SourceDerefKind = ptrKind.Value;
            }

            return;
        }

        // Trait-based deref (smart pointers like Box<T>).
        // Check Receiver for Target type, then check which Deref* trait is implemented.
        if (Inner.TypePath != null)
        {
            var receiverPath = SemanticAnalyzer.ReceiverTraitPath;
            if (analyzer.TypeImplementsTrait(Inner.TypePath, receiverPath))
            {
                var target = analyzer.ResolveAssociatedType(Inner.TypePath, receiverPath, "Target");
                if (target != null)
                {
                    IsDerefTrait = true;
                    TypePath = target;

                    // Determine capability from the highest implemented deref trait
                    if (analyzer.TypeImplementsTrait(Inner.TypePath, SemanticAnalyzer.DerefUniqTraitPath))
                        SourceDerefKind = RefKind.Uniq;
                    else if (analyzer.TypeImplementsTrait(Inner.TypePath, SemanticAnalyzer.DerefMutTraitPath))
                        SourceDerefKind = RefKind.Mut;
                    else if (analyzer.TypeImplementsTrait(Inner.TypePath, SemanticAnalyzer.DerefConstTraitPath))
                        SourceDerefKind = RefKind.Const;
                    else if (analyzer.TypeImplementsTrait(Inner.TypePath, SemanticAnalyzer.DerefTraitPath))
                        SourceDerefKind = RefKind.Shared;
                    else
                    {
                        // Only Receiver, no Deref — method dispatch only, bare *expr not allowed
                        analyzer.AddException(new SemanticException(
                            $"Type '{Inner.TypePath}' implements Receiver but not Deref — " +
                            $"cannot dereference with *. Use method calls instead.\n{Token.GetLocationStringRepresentation()}"));
                    }

                    if (!analyzer.IsTypeCopy(target))
                        IsNonCopySmartPointerDeref = true;

                    return;
                }
            }
        }

        analyzer.AddException(new DerefNonReferenceException(
            Inner.TypePath!, Token.GetLocationStringRepresentation()));
        TypePath = Inner.TypePath;
    }

    /// <summary>
    /// The deref capability — what ref kinds &amp;*expr can produce.
    /// Follows the trait hierarchy: Deref(&amp;) / DerefConst(&amp;const,&amp;) / DerefMut(&amp;mut,&amp;) / DerefUniq(all).
    /// For raw pointers: maps from pointer kind. For references: maps from ref kind.
    /// For trait-based deref: determined by which Deref* trait is implemented.
    /// </summary>
    public RefKind? SourceDerefKind { get; private set; }

    /// <summary>
    /// Can a deref source of the given capability produce a reference of the requested kind?
    /// Follows the deref trait hierarchy exactly:
    ///   Deref → &amp; only
    ///   DerefConst: Deref → &amp;const, &amp;
    ///   DerefMut: Deref → &amp;mut, &amp;
    ///   DerefUniq: Deref+DerefConst+DerefMut → &amp;uniq, &amp;mut, &amp;const, &amp;
    /// </summary>
    public static bool CanProduceRefKind(RefKind source, RefKind requested)
    {
        if (requested == RefKind.Shared) return true;
        if (requested == RefKind.Const) return source is RefKind.Const or RefKind.Uniq;
        if (requested == RefKind.Mut) return source is RefKind.Mut or RefKind.Uniq;
        if (requested == RefKind.Uniq) return source == RefKind.Uniq;
        return false;
    }

    public bool IsDerefTrait { get; private set; }
    public bool IsNonCopySmartPointerDeref { get; private set; }
    public bool IsNonCopyRefDeref { get; private set; }
    public bool IsNonCopyPlaceDeref => IsNonCopySmartPointerDeref || IsNonCopyRefDeref;

    /// <summary>
    /// Shared deref codegen — works for any dereferenceable type (RefType, RawPtrType, StructType with Receiver).
    /// Used by both DerefExpression.CodeGen and MethodCallKind auto-deref.
    /// No type-specific logic is repeated.
    /// </summary>
    public static ValueRefItem EmitDeref(CodeGenContext context, ValueRefItem inner)
    {
        Type pointeeType;
        LLVMValueRef ptrVal;

        if (inner.Type is RefType refType)
        {
            ptrVal = refType.LoadValue(context, inner);
            pointeeType = refType.PointingToType;
        }
        else if (inner.Type is RawPtrType rawPtrType)
        {
            ptrVal = rawPtrType.LoadValue(context, inner);
            pointeeType = rawPtrType.PointingToType;
        }
        else if (inner.Type is StructType structType)
        {
            // Smart pointer deref: find the first raw pointer field and load through it.
            RawPtrType? rawField = null;
            uint fieldIdx = 0;
            for (int i = 0; i < (structType.ResolvedFieldTypes?.Length ?? 0); i++)
            {
                if (structType.ResolvedFieldTypes!.Value[i] is RawPtrType rpt)
                {
                    rawField = rpt;
                    fieldIdx = (uint)i;
                    break;
                }
            }
            if (rawField == null)
                throw new InvalidOperationException(
                    $"Cannot deref '{structType.TypePath}': no raw pointer field found");

            var fieldGep = context.Builder.BuildStructGEP2(
                structType.TypeRef, inner.ValueRef, fieldIdx);
            ptrVal = rawField.LoadValue(context, new ValueRefItem
            {
                Type = rawField,
                ValueRef = fieldGep
            });
            pointeeType = rawField.PointingToType;
        }
        else
        {
            throw new InvalidOperationException($"Cannot dereference type '{inner.Type.TypePath}'");
        }

        return new ValueRefItem
        {
            Type = pointeeType,
            ValueRef = ptrVal
        };
    }

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var innerVal = Inner.CodeGen(codeGenContext);

        // When the inner expression is a temporary (e.g., *Box.New(45)), the smart pointer
        // value is not bound to any variable, so it would never be dropped. Spill it to an
        // anonymous local so the drop system sees it and calls free at scope exit.
        // This applies only to trait-based deref (Box, smart pointers) — raw references and
        // raw pointers are not heap-allocated and do not need drop registration.
        if (IsDerefTrait && Inner.IsTemporary)
        {
            var spilledPtr = codeGenContext.SpillToAnonymousLocal(innerVal);
            innerVal = new ValueRefItem { Type = innerVal.Type, ValueRef = spilledPtr };
        }

        return EmitDeref(codeGenContext, innerVal);
    }

    public bool HasGuaranteedExplicitReturn => Inner.HasGuaranteedExplicitReturn;
}
