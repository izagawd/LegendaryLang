using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Definitions;

public class ImplAssociatedType
{
    public required string Name { get; init; }
    public required LangPath ConcreteType { get; set; }
    public required Token Token { get; init; }
    public List<LangPath> DeclaredBounds { get; init; } = new();
}

public class ImplDefinition : IItem, IAnalyzable, IPathResolvable
{
    public ImplDefinition(LangPath? traitPath, LangPath forTypePath,
        IEnumerable<FunctionDefinition> methods, Token token,
        IEnumerable<GenericParameter> genericParameters,
        IEnumerable<ImplAssociatedType> associatedTypes,
        IEnumerable<string>? lifetimeParameters = null,
        bool isInherent = false)
    {
        IsInherent = isInherent;
        TraitPath = traitPath ?? InherentSentinel;
        ForTypePath = forTypePath;
        Methods = methods.ToList();
        Token = token;
        GenericParameters = genericParameters.ToImmutableArray();
        AssociatedTypeAssignments = associatedTypes.ToImmutableArray();
        LifetimeParameters = lifetimeParameters?.ToImmutableArray() ?? [];
    }

    /// <summary>
    /// Sentinel path used as TraitPath for inherent impls (impl Type { ... } without a trait).
    /// This path won't match any real trait path.
    /// </summary>
    public static readonly NormalLangPath InherentSentinel =
        new(null, new NormalLangPath.PathSegment[] { "__inherent__" });

    /// <summary>
    /// True if this is an inherent impl (impl Type { ... }) rather than a trait impl (impl Trait for Type { ... }).
    /// </summary>
    public bool IsInherent { get; }

    public LangPath TraitPath { get; set; }
    public LangPath ForTypePath { get; set; }
    public List<FunctionDefinition> Methods { get; }
    public ImmutableArray<GenericParameter> GenericParameters { get; }
    public ImmutableArray<string> LifetimeParameters { get; }
    public ImmutableArray<ImplAssociatedType> AssociatedTypeAssignments { get; }

    // IItem
    bool ISyntaxNode.NeedsSemiColonAfterIfNotLastInBlock => false;

    // ISyntaxNode
    public IEnumerable<ISyntaxNode> Children => Methods;
    public Token Token { get; }

    /// <summary>
    /// Returns the set of impl generic param names for pattern matching.
    /// </summary>
    private HashSet<string> GetFreeVariableNames()
    {
        return GenericParameters.Select(gp => gp.Name).ToHashSet();
    }

    /// <summary>
    /// Tries to match a concrete type against this impl's ForTypePath pattern.
    /// Returns the bindings (generic_param_name → concrete_type) or null if no match.
    /// 
    /// For example, impl&lt;T&gt; Foo for Wrapper&lt;T&gt; matching against Wrapper&lt;i32&gt;
    /// returns {T → i32}.
    /// For non-generic impls, returns empty dict on exact match.
    /// </summary>
    public Dictionary<string, LangPath>? TryMatchConcreteType(LangPath concreteType)
    {
        var freeVars = GetFreeVariableNames();
        if (freeVars.Count == 0)
        {
            // Non-generic impl: exact match
            return ForTypePath == concreteType ? new Dictionary<string, LangPath>() : null;
        }

        var bindings = new Dictionary<string, LangPath>();
        if (TypeInference.TryUnify(ForTypePath, concreteType, freeVars, bindings))
        {
            // Verify all free vars are bound
            if (freeVars.All(v => bindings.ContainsKey(v)))
                return bindings;
        }
        return null;
    }

    /// <summary>
    /// Checks whether all generic parameter bounds are satisfied for the given bindings,
    /// including associated type constraints (e.g., T: Add&lt;T, Output = T&gt;).
    /// </summary>
    public bool CheckBounds(Dictionary<string, LangPath> bindings, SemanticAnalyzer analyzer)
    {
        // Build generic args array for substitution
        var genericArgs = TypeInference.BuildGenericArgs(GenericParameters, bindings);

        foreach (var gp in GenericParameters)
        {
            if (!bindings.TryGetValue(gp.Name, out var boundType)) return false;
            foreach (var bound in gp.TraitBounds)
            {
                // Substitute generic params in the bound (e.g., Add(T) → Add(i32))
                var resolvedBound = genericArgs != null
                    ? FieldAccessExpression.SubstituteGenerics(bound.TraitPath, GenericParameters, genericArgs.Value)
                    : bound.TraitPath;
                if (!analyzer.TypeImplementsTrait(boundType, resolvedBound))
                    return false;

                // Validate associated type constraints (e.g., Output = T → Output = i32)
                foreach (var (atName, atType) in bound.AssociatedTypeConstraints)
                {
                    var resolvedAtType = genericArgs != null
                        ? FieldAccessExpression.SubstituteGenerics(atType, GenericParameters, genericArgs.Value)
                        : atType;
                    var actualAt = analyzer.ResolveAssociatedType(boundType, resolvedBound, atName);
                    if (actualAt != null && actualAt != resolvedAtType)
                        return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Checks whether all generic parameter bounds are satisfied for the given bindings (codegen version).
    /// </summary>
    public bool CheckBoundsCodeGen(Dictionary<string, LangPath> bindings, CodeGenContext context)
    {
        var genericArgs = TypeInference.BuildGenericArgs(GenericParameters, bindings);

        foreach (var gp in GenericParameters)
        {
            if (!bindings.TryGetValue(gp.Name, out var boundType)) return false;
            foreach (var bound in gp.TraitBounds)
            {
                // Substitute generic params in the bound (e.g., Add(T) → Add(i32))
                var resolvedBound = genericArgs != null
                    ? FieldAccessExpression.SubstituteGenerics(bound.TraitPath, GenericParameters, genericArgs.Value)
                    : bound.TraitPath;

                // Check if boundType is a generic param with this trait bound in scope
                if (boundType is NormalLangPath nlpBound && nlpBound.PathSegments.Length == 1)
                {
                    if (context.HasTraitBound(resolvedBound)) continue;
                }

                // Strip generics for base comparison
                var (resolvedBoundBase, resolvedBoundGenericArgs) = LangPath.SplitGenerics(resolvedBound);

                if (!context.ImplDefinitions.Any(i =>
                {
                    var (implTraitBase, implTraitGenericArgs) = LangPath.SplitGenerics(i.TraitPath);

                    if (implTraitBase != resolvedBoundBase) return false;

                    var match = i.TryMatchConcreteType(boundType);
                    if (match == null) return false;

                    // Unify trait generic args
                    if (resolvedBoundGenericArgs.Length > 0 || implTraitGenericArgs.Length > 0)
                    {
                        if (resolvedBoundGenericArgs.Length != implTraitGenericArgs.Length) return false;
                        var freeVars = i.GenericParameters.Select(g => g.Name).ToHashSet();
                        for (int idx = 0; idx < resolvedBoundGenericArgs.Length; idx++)
                            if (!TypeInference.TryUnify(implTraitGenericArgs[idx], resolvedBoundGenericArgs[idx], freeVars, match))
                                return false;
                    }

                    return true;
                }))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Transitively walks the supertrait chain, collecting associated type assignments
    /// from supertrait impls into the substitution map.
    /// </summary>
    private void CollectSupertraitAssociatedTypes(
        TraitDefinition traitDef, Dictionary<string, LangPath> substitutions,
        SemanticAnalyzer analyzer, HashSet<LangPath> visited)
    {
        foreach (var superBound in traitDef.Supertraits)
        {
            var superTraitPath = SubstituteAll(superBound.TraitPath, substitutions);
            var superBase = LangPath.StripGenerics(superTraitPath);
            if (superBase == null || !visited.Add(superBase)) continue;

            // Get associated types from the supertrait impl
            var resolved = analyzer.ResolveAssociatedTypesFromImpl(
                ForTypePath, superTraitPath, GenericParameters, substitutions);
            foreach (var (name, value) in resolved)
                substitutions.TryAdd(name, value);

            // Recurse into the supertrait's own supertraits
            var superDef = analyzer.GetDefinition(superBase) as TraitDefinition;
            if (superDef != null)
                CollectSupertraitAssociatedTypes(superDef, substitutions, analyzer, visited);
        }
    }

    /// <summary>
    /// Substitutes 'Self' with the concrete implementing type in a LangPath.
    /// E.g., Add&lt;Self&gt; with forType=i32 becomes Add&lt;i32&gt;.
    /// </summary>
    private static LangPath SubstituteSelf(LangPath path, LangPath forType)
    {
        return SubstituteAll(path, new Dictionary<string, LangPath> { ["Self"] = forType });
    }

    /// <summary>
    /// Substitutes named identifiers (Self, trait generic params, associated types) with concrete types.
    /// </summary>
    private static LangPath SubstituteAll(LangPath path, Dictionary<string, LangPath> substitutions,
        HashSet<string>? associatedTypeNames = null)
    {
        if (path is QualifiedAssocTypePath qp)
        {
            // If the associated type name has a direct substitution, resolve it
            // e.g., (Self as Foo).Bruh where Bruh → i32 → returns i32
            if (substitutions.TryGetValue(qp.AssociatedTypeName, out var assocReplacement))
                return assocReplacement;

            var newFor = SubstituteAll(qp.ForType, substitutions, associatedTypeNames);
            var newTrait = SubstituteAll(qp.TraitPath, substitutions, associatedTypeNames);
            if (newFor != qp.ForType || newTrait != qp.TraitPath)
                return new QualifiedAssocTypePath(newFor, newTrait, qp.AssociatedTypeName, qp.FirstIdentifierToken);
            return path;
        }

        if (path is NormalLangPath nlp)
        {
            // Single identifier: substitute Self and generic params, but NOT bare associated type names.
            // Associated types must be qualified: Self.Target or (Self as Trait).Target
            if (nlp.PathSegments.Length == 1 && nlp.PathSegments[0] is NormalLangPath.NormalPathSegment ns
                && substitutions.TryGetValue(ns.Text, out var replacement)
                && (associatedTypeNames == null || !associatedTypeNames.Contains(ns.Text)))
                return replacement;

            // Handle Self.AssocType pattern (two segments where first is substitutable
            // and second is an associated type name in the substitutions)
            // e.g., Self.Bruh where Self → i32 and Bruh → i32 → returns i32
            if (nlp.PathSegments.Length == 2
                && nlp.PathSegments[0] is NormalLangPath.NormalPathSegment first
                && nlp.PathSegments[1] is NormalLangPath.NormalPathSegment second
                && substitutions.ContainsKey(first.Text)
                && substitutions.TryGetValue(second.Text, out var assocVal))
            {
                return assocVal;
            }

            // Handle expanded Self.AssocType — path resolution may have already resolved Self
            // to its concrete type, turning Self.Target into e.g. Std.Reference.shared(T).Target
            // Check: last segment is an assoc type name in substitutions, prefix matches Self's value
            if (nlp.PathSegments.Length > 2
                && nlp.PathSegments[^1] is NormalLangPath.NormalPathSegment lastSeg
                && substitutions.TryGetValue(lastSeg.Text, out var expandedAssocVal)
                && substitutions.TryGetValue("Self", out var selfExpanded))
            {
                var prefix = nlp.Pop();
                if (prefix != null && prefix.Equals(selfExpanded))
                    return expandedAssocVal;
            }

            // Recurse into path segments, substituting in generic args
            bool changed = false;
            var newSegs = new List<NormalLangPath.PathSegment>();
            foreach (var seg in nlp.PathSegments)
            {
                if (seg is NormalLangPath.NormalPathSegment { HasGenericArgs: true } nps)
                {
                    var newTypes = nps.GenericArgs!.Value.Select(tp =>
                    {
                        var sub = SubstituteAll(tp, substitutions, associatedTypeNames);
                        if (sub != tp) changed = true;
                        return sub;
                    }).ToImmutableArray();
                    newSegs.Add(nps.WithGenericArgs(newTypes));
                }
                else
                {
                    newSegs.Add(seg);
                }
            }
            if (changed)
                return new NormalLangPath(nlp.FirstIdentifierToken, newSegs);
        }
        return path;
    }

    /// <summary>
    /// Validates that all supertraits of the trait being implemented are also implemented
    /// for the implementing type. Checks transitively (if A: B and B: C, checks both B and C).
    /// Substitutes trait generic params in supertrait paths (e.g., trait Foo&lt;X&gt;: Bar&lt;X&gt;,
    /// impl Foo&lt;i32&gt; → checks Bar&lt;i32&gt;).
    /// </summary>
    private void ValidateSupertraits(TraitDefinition traitDef,
        Dictionary<string, LangPath> traitSubstitutions, SemanticAnalyzer analyzer)
    {
        var visited = new HashSet<LangPath>();
        ValidateSupertraitsRecursive(traitDef, traitSubstitutions, analyzer, visited);
    }

    private void ValidateSupertraitsRecursive(TraitDefinition traitDef,
        Dictionary<string, LangPath> traitSubstitutions, SemanticAnalyzer analyzer,
        HashSet<LangPath> visited)
    {
        foreach (var supertrait in traitDef.Supertraits)
        {
            // Substitute trait generic params (e.g., Bar<X> → Bar<i32>)
            var resolvedPath = SubstituteAll(supertrait.TraitPath, traitSubstitutions);

            if (!visited.Add(resolvedPath)) continue;

            if (!analyzer.TypeImplementsTrait(ForTypePath, resolvedPath))
            {
                analyzer.AddException(new SupertraitNotImplementedException(
                    ForTypePath, TraitPath, resolvedPath,
                    Token.GetLocationStringRepresentation()));
            }

            // Validate associated type constraints on the supertrait
            // e.g., trait Bar: Foo<Output = i32> — check that the impl of Foo has Output = i32
            foreach (var (atName, atType) in supertrait.AssociatedTypeConstraints)
            {
                var resolvedAtType = SubstituteAll(atType, traitSubstitutions);
                var actualAt = analyzer.ResolveAssociatedType(ForTypePath, resolvedPath, atName);
                if (actualAt != null && actualAt != resolvedAtType)
                {
                    analyzer.AddException(new SemanticException(
                        $"Supertrait '{resolvedPath}' requires associated type '{atName} = {resolvedAtType}' " +
                        $"but the impl for '{ForTypePath}' has '{atName} = {actualAt}'\n{Token.GetLocationStringRepresentation()}"));
                }
            }

            // Recurse: check supertraits of supertraits
            var superLookup = LangPath.StripGenerics(resolvedPath);
            var superDef = analyzer.GetDefinition(superLookup) as TraitDefinition;
            if (superDef != null)
            {
                // Build substitution map for the supertrait's own generic params
                var superSubs = new Dictionary<string, LangPath>(traitSubstitutions);
                if (resolvedPath is NormalLangPath nlpSuper)
                {
                    var superGenericArgs = nlpSuper.GetFrontGenerics();
                    for (int i = 0; i < superDef.GenericParameters.Length && i < superGenericArgs.Length; i++)
                        superSubs[superDef.GenericParameters[i].Name] = superGenericArgs[i];
                }
                ValidateSupertraitsRecursive(superDef, superSubs, analyzer, visited);
            }
        }
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Reject manual implementations of compiler-only traits
        if (Token != null && !IsInherent)
        {
            var traitBase = LangPath.StripGenerics(TraitPath);
            if (traitBase.Equals(SemanticAnalyzer.MetaSizedTraitPath) ||
                traitBase.Equals(SemanticAnalyzer.SizedTraitPath))
            {
                analyzer.AddException(new SemanticException(
                    $"Cannot manually implement '{traitBase}' — it is automatically implemented by the compiler\n" +
                    Token.GetLocationStringRepresentation()));
                return;
            }
        }

        // For inherent impls, skip all trait validation — just analyze method bodies
        if (IsInherent)
        {
            // Check for unused impl generic parameters
            foreach (var gp in GenericParameters)
            {
                if (!LangPath.GenericParamUsedInType(gp.Name, ForTypePath))
                    analyzer.AddException(new SemanticException(
                        $"Generic parameter '{gp.Name}' is never used in the implementing type '{ForTypePath}'\n{Token.GetLocationStringRepresentation()}"));
            }

            // Push impl generic bounds for method body analysis
            var bounds = SemanticAnalyzer.BuildGenericBoundsWithImplicitSized(GenericParameters);
            analyzer.PushTraitBounds(bounds);
            analyzer.PushImplLifetimes(LifetimeParameters);

            foreach (var method in Methods)
                method.Analyze(analyzer);

            analyzer.PopImplLifetimes();
            analyzer.PopTraitBounds();
            return;
        }

        // Validate that the trait exists — strip generics for lookup
        var traitLookupPath = LangPath.StripGenerics(TraitPath);
        var traitDef = analyzer.GetDefinition(traitLookupPath) as TraitDefinition;
        if (traitDef == null)
        {
            analyzer.AddException(new TraitNotFoundException(TraitPath, Token.GetLocationStringRepresentation()));
            return;
        }

        // Check for unused impl generic parameters
        foreach (var gp in GenericParameters)
        {
            if (!LangPath.GenericParamUsedInType(gp.Name, ForTypePath))
                analyzer.AddException(new SemanticException(
                    $"Generic parameter '{gp.Name}' is never used in the implementing type '{ForTypePath}'\n{Token.GetLocationStringRepresentation()}"));
        }

        // Validate that each trait method is implemented
        // Build substitution map: Self → ForTypePath, trait generics → concrete args, assoc types → concrete types
        var traitSubstitutions = new Dictionary<string, LangPath>();
        traitSubstitutions["Self"] = ForTypePath;
        // Trait generic params: trait Add(Rhs) with impl Add(i32) → Rhs=i32
        if (TraitPath is NormalLangPath nlpTraitWithGenerics && nlpTraitWithGenerics.GetFrontGenerics().Length > 0)
        {
            var traitGenericArgs = nlpTraitWithGenerics.GetFrontGenerics();
            for (int i = 0; i < traitDef.GenericParameters.Length && i < traitGenericArgs.Length; i++)
                traitSubstitutions[traitDef.GenericParameters[i].Name] = traitGenericArgs[i];
        }
        // Associated type assignments: type Output = i32 → Output=i32
        // Resolve Self in associated type values (e.g., type Bruh = Self → Bruh = i32)
        foreach (var at in AssociatedTypeAssignments)
            traitSubstitutions[at.Name] = SubstituteAll(at.ConcreteType,
                new Dictionary<string, LangPath> { ["Self"] = ForTypePath });

        // Resolve associated types from supertrait impls (transitively).
        // e.g., DerefMut: Deref, Deref: Receiver { type Target; }
        // Target is defined in Receiver, two levels up from DerefMut.
        if (traitDef != null)
        {
            var visited = new HashSet<LangPath>();
            CollectSupertraitAssociatedTypes(traitDef, traitSubstitutions, analyzer, visited);
        }

        // Build set of associated type names — these must be qualified (Self.Target, not bare Target)
        var associatedTypeNames = new HashSet<string>();
        foreach (var at in AssociatedTypeAssignments)
            associatedTypeNames.Add(at.Name);
        // Also include supertrait associated types (Target from Receiver, etc.)
        // But exclude Self, impl generic params, AND trait generic params (Rhs, etc.)
        var traitGenericNames = traitDef?.GenericParameters.Select(gp => gp.Name).ToHashSet()
            ?? new HashSet<string>();
        foreach (var kv in traitSubstitutions)
            if (kv.Key != "Self"
                && !GenericParameters.Any(gp => gp.Name == kv.Key)
                && !traitGenericNames.Contains(kv.Key))
                associatedTypeNames.Add(kv.Key);

        // Push impl generic bounds so TypeImplementsTrait can verify supertrait impls
        // for generic types (e.g., impl[T:! Copy] A for Wrapper(T) where trait A: B)
        var implBounds = SemanticAnalyzer.BuildGenericBoundsWithImplicitSized(GenericParameters);
        analyzer.PushTraitBounds(implBounds);

        // Validate that all supertraits (transitively) are implemented for the type.
        // Substitute trait generic params in supertrait paths (e.g., trait Foo<X>: Bar<X>,
        // impl Foo<i32> for MyType → check MyType implements Bar<i32>).
        ValidateSupertraits(traitDef, traitSubstitutions, analyzer);

        foreach (var traitMethod in traitDef.MethodSignatures)
        {
            var implMethod = Methods.FirstOrDefault(m => m.Name == traitMethod.Name);
            if (implMethod == null)
            {
                if (traitMethod.HasDefault)
                {
                    // Inject a synthetic FunctionDefinition from the trait's default body.
                    // Substitute Self and trait generic params with concrete types so the
                    // body is analyzed with concrete types (e.g., Self→i32, Rhs→i32).
                    var existingMethod = Methods.FirstOrDefault();
                    var implModule = existingMethod?.Module
                        ?? new NormalLangPath(null, [new NormalLangPath.NormalPathSegment(
                            $"impl {TraitPath} for {ForTypePath}")]);

                    // Substitute parameter types: Self → concrete type, Rhs → concrete arg, etc.
                    var substitutedParams = traitMethod.Parameters
                        .Select(p => new VariableDefinition(p.IdentifierToken,
                            p.TypePath != null ? SubstituteAll(p.TypePath, traitSubstitutions) : null))
                        .ToImmutableArray();

                    var substitutedReturnType = SubstituteAll(
                        traitMethod.ReturnTypePath, traitSubstitutions);

                    var defaultMethod = new FunctionDefinition(
                        traitMethod.Name,
                        substitutedParams,
                        substitutedReturnType,
                        traitMethod.DefaultBody,
                        implModule,
                        traitMethod.GenericParameters,
                        traitMethod.Token,
                        traitMethod.LifetimeParameters,
                        traitMethod.ArgumentLifetimes,
                        traitMethod.ReturnLifetime)
                    { IsPreAnalyzed = true, TraitSubstitutions = new Dictionary<string, LangPath>(traitSubstitutions) };

                    Methods.Add(defaultMethod);
                    implMethod = defaultMethod;
                }
                else
                {
                    analyzer.AddException(new TraitMethodNotImplementedException(
                        traitMethod.Name, TraitPath, Token.GetLocationStringRepresentation()));
                    continue;
                }
            }

            if (implMethod.Arguments.Length != traitMethod.Parameters.Length)
            {
                analyzer.AddException(new SemanticException(
                    $"Method '{traitMethod.Name}' has {implMethod.Arguments.Length} parameters, " +
                    $"but the trait requires {traitMethod.Parameters.Length}\n{implMethod.Token.GetLocationStringRepresentation()}"));
            }
            else
            {
                // Validate parameter types match (substituting Self, trait generics, assoc types)
                for (int i = 0; i < traitMethod.Parameters.Length; i++)
                {
                    var traitParamType = traitMethod.Parameters[i].TypePath;
                    var implParamType = implMethod.Arguments[i].TypePath;
                    if (traitParamType == null || implParamType == null) continue;

                    var resolvedTraitParamType = SubstituteAll(traitParamType, traitSubstitutions, associatedTypeNames);
                    var resolvedImplParamType = SubstituteAll(implParamType, traitSubstitutions, associatedTypeNames);
                    if (resolvedTraitParamType != resolvedImplParamType)
                    {
                        // If trait expects &T and impl provides T, auto-adjust the impl param
                        // to &T. The codegen passes references, and the body auto-derefs.
                        if (RefTypeDefinition.IsReferenceType(resolvedTraitParamType)
                            && resolvedTraitParamType is NormalLangPath nlpTraitParam)
                        {
                            var innerType = nlpTraitParam.GetFrontGenerics();
                            if (innerType.Length == 1 && innerType[0] == resolvedImplParamType)
                            {
                                // Accept: adjust impl param type to match trait's reference type
                                implMethod.Arguments[i].TypePath = resolvedTraitParamType;
                                continue;
                            }
                        }

                        analyzer.AddException(new SemanticException(
                            $"Method '{traitMethod.Name}': parameter '{traitMethod.Parameters[i].Name}' has type '{resolvedImplParamType}' " +
                            $"but trait requires '{resolvedTraitParamType}'\n{implMethod.Token.GetLocationStringRepresentation()}"));
                    }
                }
            }

            // Validate return type matches
            {
                var resolvedTraitReturn = SubstituteAll(traitMethod.ReturnTypePath, traitSubstitutions, associatedTypeNames);
                var resolvedImplReturn = SubstituteAll(implMethod.ReturnTypePath, traitSubstitutions, associatedTypeNames);
                if (resolvedTraitReturn != resolvedImplReturn)
                {
                    analyzer.AddException(new SemanticException(
                        $"Method '{traitMethod.Name}': return type '{resolvedImplReturn}' " +
                        $"does not match trait's return type '{resolvedTraitReturn}'\n{implMethod.Token.GetLocationStringRepresentation()}"));
                }
            }

            // Validate generic parameter count matches
            if (implMethod.GenericParameters.Length != traitMethod.GenericParameters.Length)
            {
                analyzer.AddException(new TraitImplBoundsMismatchException(
                    $"Method '{traitMethod.Name}' has {implMethod.GenericParameters.Length} generic parameter(s), " +
                    $"but the trait requires {traitMethod.GenericParameters.Length}",
                    implMethod.Token.GetLocationStringRepresentation()));
            }
            else
            {
                // Validate that generic bounds match
                // Substitute Self → ForTypePath in trait bounds for comparison
                for (int i = 0; i < traitMethod.GenericParameters.Length; i++)
                {
                    var traitGp = traitMethod.GenericParameters[i];
                    var implGp = implMethod.GenericParameters[i];

                    // Resolve trait bounds by substituting Self, trait generics, and assoc types
                    var resolvedTraitBounds = traitGp.TraitBounds
                        .Select(tb => SubstituteAll(tb.TraitPath, traitSubstitutions))
                        .ToList();
                    var resolvedImplBounds = implGp.TraitBounds
                        .Select(tb => SubstituteAll(tb.TraitPath, traitSubstitutions))
                        .ToList();

                    // Impl must not add bounds that the trait didn't require
                    foreach (var implBoundPath in resolvedImplBounds)
                    {
                        if (!resolvedTraitBounds.Any(tb => tb == implBoundPath))
                        {
                            analyzer.AddException(new TraitImplBoundsMismatchException(
                                $"Method '{traitMethod.Name}': impl adds bound '{implBoundPath}' on generic parameter '{implGp.Name}' " +
                                $"which is not present in the trait definition",
                                implMethod.Token.GetLocationStringRepresentation()));
                        }
                    }

                    // Trait bounds must be present in the impl
                    foreach (var traitBoundPath in resolvedTraitBounds)
                    {
                        if (!resolvedImplBounds.Any(tb => tb == traitBoundPath))
                        {
                            analyzer.AddException(new TraitImplBoundsMismatchException(
                                $"Method '{traitMethod.Name}': impl is missing bound '{traitBoundPath}' on generic parameter '{implGp.Name}' " +
                                $"required by the trait definition",
                                implMethod.Token.GetLocationStringRepresentation()));
                        }
                    }
                }
            }
        }

        // Check for extra methods not in the trait
        foreach (var implMethod in Methods)
        {
            if (!traitDef.MethodSignatures.Any(m => m.Name == implMethod.Name))
            {
                analyzer.AddException(new TraitExtraMethodException(
                    implMethod.Name, TraitPath, implMethod.Token.GetLocationStringRepresentation()));
            }
        }

        // Analyze each method body — impl generic bounds already pushed above
        analyzer.PushImplLifetimes(LifetimeParameters);
        foreach (var method in Methods)
            method.Analyze(analyzer);
        analyzer.PopImplLifetimes();

        analyzer.PopTraitBounds();

        // Validate associated types
        foreach (var traitAT in traitDef.AssociatedTypes)
        {
            var implAT = AssociatedTypeAssignments.FirstOrDefault(a => a.Name == traitAT.Name);
            if (implAT == null)
            {
                analyzer.AddException(new SemanticException(
                    $"Missing associated type '{traitAT.Name}' in impl of '{TraitPath}'\n{Token.GetLocationStringRepresentation()}"));
            }
            else
            {
                // Validate: impl's declared bounds must match trait's declared bounds
                var traitBoundSet = traitAT.TraitBounds;
                var implBoundSet = implAT.DeclaredBounds;
                bool boundsMatch = traitBoundSet.Count == implBoundSet.Count
                    && traitBoundSet.All(tb => implBoundSet.Any(ib => ib.Equals(tb)));
                if (!boundsMatch)
                {
                    var traitBoundsStr = traitBoundSet.Count == 0 ? "type" : string.Join(" + ", traitBoundSet);
                    var implBoundsStr = implBoundSet.Count == 0 ? "type" : string.Join(" + ", implBoundSet);
                    analyzer.AddException(new SemanticException(
                        $"Associated type '{traitAT.Name}' bound mismatch: trait declares ':! {traitBoundsStr}' " +
                        $"but impl declares ':! {implBoundsStr}'\n{Token.GetLocationStringRepresentation()}"));
                }

                // Validate: concrete type satisfies the trait's bounds
                foreach (var bound in traitAT.TraitBounds)
                {
                    if (!analyzer.TypeImplementsTrait(implAT.ConcreteType, bound))
                    {
                        analyzer.AddException(new SemanticException(
                            $"Associated type '{traitAT.Name} = {implAT.ConcreteType}' does not satisfy bound '{bound}'\n{Token.GetLocationStringRepresentation()}"));
                    }
                }
            }
        }

        // Check for extra associated types not in the trait
        foreach (var implAT in AssociatedTypeAssignments)
        {
            if (!traitDef.AssociatedTypes.Any(a => a.Name == implAT.Name))
            {
                analyzer.AddException(new SemanticException(
                    $"Associated type '{implAT.Name}' is not defined in trait '{TraitPath}'\n{Token.GetLocationStringRepresentation()}"));
            }
        }

        // If implementing Copy, validate that all fields of the implementing type also implement Copy
        if (TraitPath == SemanticAnalyzer.CopyTraitPath)
        {
            var typeDef = analyzer.GetDefinition(ForTypePath);
            if (typeDef == null)
                typeDef = analyzer.GetDefinition(LangPath.StripGenerics(ForTypePath));

            if (typeDef is StructTypeDefinition structDef)
            {
                // Build a set of impl generic param names that have Copy bounds
                var copyBoundParams = GenericParameters
                    .Where(gp => gp.TraitBounds.Any(b => b.TraitPath == SemanticAnalyzer.CopyTraitPath))
                    .Select(gp => gp.Name)
                    .ToHashSet();

                foreach (var field in structDef.Fields)
                {
                    var fieldType = field.TypePath;

                    // Resolve qualified associated type paths (e.g., (i32 as Add(i32)).Output → i32)
                    fieldType = analyzer.ResolveQualifiedTypePath(fieldType);

                    // If the field type is a generic param of the struct...
                    if (fieldType is NormalLangPath nlpField && nlpField.PathSegments.Length == 1)
                    {
                        var paramName = nlpField.PathSegments[0].ToString();

                        // Check if it's one of the impl's generic params
                        if (GenericParameters.Any(gp => gp.Name == paramName))
                        {
                            // It must have a Copy bound on this impl
                            if (!copyBoundParams.Contains(paramName))
                            {
                                analyzer.AddException(new SemanticException(
                                    $"Cannot implement Copy for '{ForTypePath}': field '{field.Name}' has type '{paramName}' " +
                                    $"which does not have a Copy bound\n{Token.GetLocationStringRepresentation()}"));
                            }
                            continue;
                        }

                        // Check if it's one of the struct's own generic params
                        if (structDef.GenericParameters.Any(gp => gp.Name == paramName))
                        {
                            // For non-generic impls like `impl Copy for Wrapper<i32>`,
                            // substitute the generic arg and check
                            if (ForTypePath is NormalLangPath nlpForType)
                            {
                                var genericArgs = nlpForType.GetFrontGenerics();
                                for (int i = 0; i < structDef.GenericParameters.Length && i < genericArgs.Length; i++)
                                {
                                    if (structDef.GenericParameters[i].Name == paramName)
                                    {
                                        fieldType = genericArgs[i];
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // Check that the resolved field type implements Copy
                    if (!analyzer.IsTypeCopy(fieldType))
                    {
                        analyzer.AddException(new SemanticException(
                            $"Cannot implement Copy for '{ForTypePath}': field '{field.Name}' has type '{fieldType}' " +
                            $"which does not implement Copy\n{Token.GetLocationStringRepresentation()}"));
                    }
                }
            }

            // Enum Copy validation: all variant field types must implement Copy
            if (typeDef is EnumTypeDefinition enumDef)
            {
                var copyBoundParams = GenericParameters
                    .Where(gp => gp.TraitBounds.Any(b => b.TraitPath == SemanticAnalyzer.CopyTraitPath))
                    .Select(gp => gp.Name)
                    .ToHashSet();

                foreach (var variant in enumDef.Variants)
                {
                    foreach (var fieldType in variant.FieldTypes)
                    {
                        var resolved = analyzer.ResolveQualifiedTypePath(fieldType);

                        // Check generic params
                        if (resolved is NormalLangPath nlpField && nlpField.PathSegments.Length == 1)
                        {
                            var paramName = nlpField.PathSegments[0].ToString();
                            if (GenericParameters.Any(gp => gp.Name == paramName))
                            {
                                if (!copyBoundParams.Contains(paramName))
                                {
                                    analyzer.AddException(new SemanticException(
                                        $"Cannot implement Copy for '{ForTypePath}': variant '{variant.Name}' contains type '{paramName}' " +
                                        $"which does not have a Copy bound\n{Token.GetLocationStringRepresentation()}"));
                                }
                                continue;
                            }
                        }

                        if (!analyzer.IsTypeCopy(resolved))
                        {
                            analyzer.AddException(new SemanticException(
                                $"Cannot implement Copy for '{ForTypePath}': variant '{variant.Name}' contains type '{resolved}' " +
                                $"which does not implement Copy\n{Token.GetLocationStringRepresentation()}"));
                        }
                    }
                }
            }
        }

        // If implementing Copy, check that the type does NOT already implement Drop
        if (TraitPath == SemanticAnalyzer.CopyTraitPath)
        {
            if (analyzer.IsTypeDrop(ForTypePath))
            {
                analyzer.AddException(new CopyDropConflictException(
                    ForTypePath, Token.GetLocationStringRepresentation()));
            }
        }

        // If implementing Drop, validate constraints
        if (TraitPath == SemanticAnalyzer.DropTraitPath)
        {
            // Check that the type does NOT already implement Copy
            if (analyzer.IsTypeCopy(ForTypePath))
            {
                analyzer.AddException(new CopyDropConflictException(
                    ForTypePath, Token.GetLocationStringRepresentation()));
            }

            // Validate that the impl's generic constraints match the type definition's constraints exactly
            var typeDef = analyzer.GetDefinition(ForTypePath);
            if (typeDef == null)
                typeDef = analyzer.GetDefinition(LangPath.StripGenerics(ForTypePath));

            if (typeDef is StructTypeDefinition structDefDrop)
            {
                ValidateDropGenericConstraints(structDefDrop.GenericParameters, structDefDrop.LifetimeParameters, analyzer);
            }
            if (typeDef is EnumTypeDefinition enumDefDrop)
            {
                ValidateDropGenericConstraints(enumDefDrop.GenericParameters, enumDefDrop.LifetimeParameters, analyzer);
            }
        }

        // If implementing MutReassign, validate constraints:
        // - Structs: all fields must implement MutReassign
        // - Enums: must be flat (no variant with payload fields)
        if (LangPath.StripGenerics(TraitPath)?.Equals(SemanticAnalyzer.MutReassignTraitPath) == true)
        {
            var typeDef = analyzer.GetDefinition(ForTypePath)
                          ?? analyzer.GetDefinition(LangPath.StripGenerics(ForTypePath));

            if (typeDef is StructTypeDefinition structDefMut)
            {
                var mutBoundParams = GenericParameters
                    .Where(gp => gp.TraitBounds.Any(b =>
                        LangPath.StripGenerics(b.TraitPath)?.Equals(SemanticAnalyzer.MutReassignTraitPath) == true))
                    .Select(gp => gp.Name)
                    .ToHashSet();

                foreach (var field in structDefMut.Fields)
                {
                    var fieldType = analyzer.ResolveQualifiedTypePath(field.TypePath);

                    // Generic param check — must have MutReassign bound
                    if (fieldType is NormalLangPath nlpField && nlpField.PathSegments.Length == 1)
                    {
                        var paramName = nlpField.PathSegments[0].ToString();
                        if (GenericParameters.Any(gp => gp.Name == paramName))
                        {
                            if (!mutBoundParams.Contains(paramName))
                                analyzer.AddException(new SemanticException(
                                    $"Cannot implement MutReassign for '{ForTypePath}': field '{field.Name}' has type '{paramName}' " +
                                    $"which does not have a MutReassign bound\n{Token.GetLocationStringRepresentation()}"));
                            continue;
                        }

                        // Substitute struct generic params with concrete args
                        if (structDefMut.GenericParameters.Any(gp => gp.Name == paramName)
                            && ForTypePath is NormalLangPath nlpForType)
                        {
                            var genericArgs = nlpForType.GetFrontGenerics();
                            for (int i = 0; i < structDefMut.GenericParameters.Length && i < genericArgs.Length; i++)
                                if (structDefMut.GenericParameters[i].Name == paramName)
                                { fieldType = genericArgs[i]; break; }
                        }
                    }

                    if (!analyzer.TypeImplementsTrait(fieldType, SemanticAnalyzer.MutReassignTraitPath))
                        analyzer.AddException(new SemanticException(
                            $"Cannot implement MutReassign for '{ForTypePath}': field '{field.Name}' has type '{fieldType}' " +
                            $"which does not implement MutReassign\n{Token.GetLocationStringRepresentation()}"));
                }
            }

            if (typeDef is EnumTypeDefinition enumDefMut)
            {
                foreach (var variant in enumDefMut.Variants)
                {
                    if (variant.FieldTypes.Length > 0)
                    {
                        analyzer.AddException(new SemanticException(
                            $"Cannot implement MutReassign for enum '{ForTypePath}': variant '{variant.Name}' has payload fields. " +
                            $"Only flat enums (all unit variants) can implement MutReassign\n{Token.GetLocationStringRepresentation()}"));
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validates that the impl Drop generic constraints match the type definition's constraints exactly.
    /// The Drop impl must have the same generic parameters with the same bounds as the type,
    /// and the same lifetime parameters.
    /// </summary>
    private void ValidateDropGenericConstraints(
        System.Collections.Immutable.ImmutableArray<GenericParameter> typeGenericParams,
        System.Collections.Immutable.ImmutableArray<string> typeLifetimeParams,
        SemanticAnalyzer analyzer)
    {
        // Check lifetime parameter count matches
        if (LifetimeParameters.Length != typeLifetimeParams.Length)
        {
            analyzer.AddException(new DropGenericsMismatchException(
                ForTypePath,
                $"impl Drop for '{ForTypePath}' has {LifetimeParameters.Length} lifetime parameter(s), " +
                $"but the type has {typeLifetimeParams.Length}",
                Token.GetLocationStringRepresentation()));
        }

        // The impl must have the same number of generic parameters as the type
        if (GenericParameters.Length != typeGenericParams.Length)
        {
            analyzer.AddException(new DropGenericsMismatchException(
                ForTypePath,
                $"impl Drop for '{ForTypePath}' has {GenericParameters.Length} generic parameter(s), " +
                $"but the type has {typeGenericParams.Length}",
                Token.GetLocationStringRepresentation()));
            return;
        }

        // Each generic parameter's trait bounds must match exactly
        for (int i = 0; i < typeGenericParams.Length; i++)
        {
            var typeGp = typeGenericParams[i];
            var implGp = GenericParameters[i];

            var typeBounds = typeGp.TraitBounds.Select(tb => tb.TraitPath).OrderBy(p => p.ToString()).ToList();
            var implBounds = implGp.TraitBounds.Select(tb => tb.TraitPath).OrderBy(p => p.ToString()).ToList();

            if (typeBounds.Count != implBounds.Count)
            {
                analyzer.AddException(new DropGenericsMismatchException(
                    ForTypePath,
                    $"impl Drop for '{ForTypePath}': generic parameter '{implGp.Name}' has {implBounds.Count} bound(s), " +
                    $"but the type definition has {typeBounds.Count} bound(s) — they must match exactly",
                    Token.GetLocationStringRepresentation()));
                continue;
            }

            for (int j = 0; j < typeBounds.Count; j++)
            {
                if (typeBounds[j] != implBounds[j])
                {
                    analyzer.AddException(new DropGenericsMismatchException(
                        ForTypePath,
                        $"impl Drop for '{ForTypePath}': generic parameter '{implGp.Name}' has bound '{implBounds[j]}', " +
                        $"but the type definition has bound '{typeBounds[j]}' — they must match exactly",
                        Token.GetLocationStringRepresentation()));
                }
            }
        }
    }

    public void ResolvePaths(PathResolver resolver)
    {
        // Resolve the trait and type paths first
        if (!IsInherent)
            TraitPath = TraitPath.Resolve(resolver);
        ForTypePath = ForTypePath.Resolve(resolver);

        // Resolve generic param trait bounds
        foreach (var gp in GenericParameters)
            for (int i = 0; i < gp.TraitBounds.Count; i++)
                gp.TraitBounds[i] = gp.TraitBounds[i].Resolve(resolver);

        // Add a scope with Self mapped to the implementing type FIRST,
        // so that associated type values like `type Bruh = Self` resolve correctly.
        resolver.AddScope();
        if (ForTypePath is NormalLangPath nlp)
            resolver.AddToDeepestScope("Self", nlp);

        // NOW resolve associated type concrete types and declared bounds (Self is in scope)
        foreach (var at in AssociatedTypeAssignments)
        {
            at.ConcreteType = at.ConcreteType.Resolve(resolver);
            for (int i = 0; i < at.DeclaredBounds.Count; i++)
                at.DeclaredBounds[i] = at.DeclaredBounds[i].Resolve(resolver);
        }

        // Register associated type assignments as path shortcuts (Output → i32)
        foreach (var at in AssociatedTypeAssignments)
            if (at.ConcreteType is NormalLangPath nlpAt)
                resolver.AddToDeepestScope(at.Name, nlpAt);

        // Resolve paths in each method (Self in params/return types becomes the concrete type)
        foreach (var method in Methods)
            method.ResolvePaths(resolver);

        resolver.PopScope();
    }

    public FunctionDefinition? GetMethod(string methodName)
    {
        return Methods.FirstOrDefault(m => m.Name == methodName);
    }

    public static ImplDefinition Parse(Parser parser, NormalLangPath module)
    {
        var implToken = parser.Pop();
        if (implToken is not ImplToken)
            throw new ExpectedParserException(parser, ParseType.Impl, implToken);

        // Parse optional generic parameters — shared with FunctionDefinition/TraitDefinition
        var generics = FunctionSignatureParser.ParseImplicitGenericParams(parser);
        var genericParameters = generics?.GenericParameters.ToList() ?? new List<GenericParameter>();
        var lifetimeParameters = generics?.LifetimeParameters ?? [];

        var firstPath = LangPath.Parse(parser, true);

        LangPath? traitPath;
        LangPath forTypePath;
        bool isInherent;

        if (parser.Peek() is ForToken)
        {
            // Trait impl: impl TraitPath for TypePath { ... }
            parser.Pop(); // consume 'for'
            traitPath = firstPath;
            forTypePath = LangPath.Parse(parser, true);
            isInherent = false;
        }
        else
        {
            // Inherent impl: impl TypePath { ... }
            traitPath = null;
            forTypePath = firstPath;
            isInherent = true;
        }

        CurlyBrace.ParseLeft(parser);

        // Create a synthetic module for the impl methods
        var implModuleName = isInherent
            ? $"impl_{forTypePath}"
            : $"impl_{traitPath}_for_{forTypePath}";
        var implModule = new NormalLangPath(null,
            [new NormalLangPath.NormalPathSegment(implModuleName)]);

        var methods = new List<FunctionDefinition>();
        var associatedTypes = new List<ImplAssociatedType>();
        while (parser.Peek() is not RightCurlyBraceToken)
        {
            if (parser.Peek() is LetToken)
            {
                // Parse: let Output :! type = i32; or let Output :! Copy = i32;
                parser.Pop(); // consume 'let'
                var atName = Identifier.Parse(parser);
                var colonBang = parser.Pop();
                if (colonBang is not ColonBangToken)
                    throw new ExpectedParserException(parser, ParseType.ColonBang, colonBang);
                // Parse bounds — must match trait declaration
                var implBounds = FunctionSignatureParser.ParseComptimeBounds(parser);
                var eqTok = parser.Pop();
                if (eqTok is not EqualityToken)
                    throw new ExpectedParserException(parser, ParseType.Equality, eqTok);
                var concreteType = LangPath.Parse(parser, true);
                SemiColon.Parse(parser);
                associatedTypes.Add(new ImplAssociatedType
                {
                    Name = atName.Identity,
                    ConcreteType = concreteType,
                    Token = atName,
                    DeclaredBounds = implBounds.Select(b => b.TraitPath).ToList()
                });
            }
            else
            {
                methods.Add(FunctionDefinition.Parse(parser, implModule));
            }
        }

        CurlyBrace.Parseight(parser);

        return new ImplDefinition(traitPath, forTypePath, methods, (Token)implToken, genericParameters, associatedTypes, lifetimeParameters, isInherent);
    }
}
