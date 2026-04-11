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

    /// Dereferencing propagates the temporariness of the inner expression.
    /// A deref of a temporary is a semantic error, so this only matters for
    /// expressions that pass analysis — i.e. always false in valid programs.
    public bool IsTemporary => Inner.IsTemporary;

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
                // (not a move). e.g., *self where self: &&mut T → produces &mut T (fine).
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
                    if (Inner.IsTemporary && !analyzer.IsTypeCopy(Inner.TypePath))
                    {
                        analyzer.AddException(new SemanticException(
                            $"Cannot dereference a temporary smart pointer. " +
                            $"Bind it to a variable first.\n{Token.GetLocationStringRepresentation()}"));
                        TypePath = target;
                        return;
                    }

                    IsDerefTrait = true;
                    TypePath = target;

                    // Determine capability from the highest implemented deref trait
                    if (analyzer.TypeImplementsTrait(Inner.TypePath, SemanticAnalyzer.DerefMutTraitPath))
                        SourceDerefKind = RefKind.Mut;
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
    /// Follows the trait hierarchy: Deref(&amp;) / DerefMut(&amp;mut,&amp;).
    /// For raw pointers: maps from pointer kind. For references: maps from ref kind.
    /// For trait-based deref: determined by which Deref* trait is implemented.
    /// </summary>
    public RefKind? SourceDerefKind { get; private set; }

    /// <summary>
    /// Can a deref source of the given capability produce a reference of the requested kind?
    ///   Deref → &amp; only
    ///   DerefMut → &amp;mut, &amp;
    /// </summary>
    public static bool CanProduceRefKind(RefKind source, RefKind requested)
    {
        if (requested == RefKind.Shared) return true;
        if (requested == RefKind.Mut) return source == RefKind.Mut;
        return false;
    }

    public bool IsDerefTrait { get; private set; }
    public bool IsNonCopySmartPointerDeref { get; private set; }
    public bool IsNonCopyRefDeref { get; private set; }
    public bool IsNonCopyPlaceDeref => IsNonCopySmartPointerDeref || IsNonCopyRefDeref;

    /// <summary>
    /// Shared deref codegen — works for any dereferenceable type (RefType, RawPtrType, StructType with Deref impl).
    /// Used by both DerefExpression.CodeGen and MethodCallKind auto-deref.
    /// For struct types, calls the appropriate deref trait method (deref/deref_mut)
    /// regardless of internal fields — deref behaviour is defined solely by the trait implementation.
    /// </summary>
    public static ValueRefItem EmitDeref(CodeGenContext context, ValueRefItem inner, RefKind? derefKind = null)
    {
        Type pointeeType;
        LLVMValueRef ptrVal;

        if (inner.Type is RefType refType)
        {
            pointeeType = refType.PointingToType;

            // Unsized pointee (str, slices): the fat pointer IS the value.
            // Extracting just the data pointer would lose the metadata (length).
            // Return the reference itself — callers that need the raw pointer
            // (field access, load) will extract it themselves.
            if (pointeeType is UnsizedType)
                return inner;

            ptrVal = refType.ExtractDataPointer(context, inner);
        }
        else if (inner.Type is RawPtrType rawPtrType)
        {
            pointeeType = rawPtrType.PointingToType;

            if (pointeeType is UnsizedType)
                return inner;

            ptrVal = rawPtrType.ExtractDataPointer(context, inner);
        }
        else if (inner.Type is StructType structType)
        {
            // Trait-based deref: call the appropriate deref method (deref/deref_mut).
            // The method takes &Self and returns &T — we load through the returned reference to get T*.
            if (derefKind == null)
                throw new InvalidOperationException(
                    $"Cannot deref '{structType.TypePath}': no deref kind provided");

            var methodName = SemanticAnalyzer.GetDerefMethodForRefKind(derefKind.Value);

            // Build the correct self reference kind for this deref method:
            // deref → &Self, deref_mut → &mut Self
            var selfRefTypePath = RefTypeDefinition.GetRefModule()
                .Append(RefTypeDefinition.GetRefName(derefKind.Value))
                .AppendGenerics([structType.TypePath!]);
            var selfRefTypeRef = context.GetRefItemFor(selfRefTypePath) as TypeRefItem;
            if (selfRefTypeRef?.Type is not RefType selfRefType)
                throw new InvalidOperationException(
                    $"Cannot build self reference for '{structType.TypePath}'");

            var selfArg = selfRefType.WrapAsRef(context, inner);

            // Call deref method — returns &T (an alloca of RefType)
            var derefResult = context.EmitCall((NormalLangPath)structType.TypePath!, methodName, [selfArg]);

            // Load through the returned &T to get the raw T* pointer
            if (derefResult.Type is not RefType retRefType)
                throw new InvalidOperationException(
                    $"Expected '{methodName}' to return a reference, got '{derefResult.Type?.TypePath}'");

            ptrVal = retRefType.ExtractDataPointer(context, derefResult);
            pointeeType = retRefType.PointingToType;
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

        // When the inner expression is a temporary (e.g., *Gc.New(45)), the smart pointer
        // value is not bound to any variable, so it would never be dropped. Spill it to an
        // anonymous local so the drop system sees it and calls free at scope exit.
        // This applies only to trait-based deref (Box, smart pointers) — raw references and
        // raw pointers are not heap-allocated and do not need drop registration.
        
        // For standalone *expr, use deref (Shared) — the baseline read-only deref.
        // SourceDerefKind tracks the maximum capability (for CanProduceRefKind checks),
        // but the actual call should use deref unless a higher context requires deref_mut.
        var derefKind = SourceDerefKind.HasValue && IsDerefTrait ? RefKind.Shared : SourceDerefKind;
        return EmitDeref(codeGenContext, innerVal, derefKind);
    }

    public bool HasGuaranteedExplicitReturn => Inner.HasGuaranteedExplicitReturn;
}
