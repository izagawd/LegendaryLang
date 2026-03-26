using System.Collections.Immutable;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;

namespace LegendaryLang.Semantics;

public class SemanticException : Exception
{
    public SemanticException(string message) : base(message)
    {
    }

    public SemanticException()
    {
    }
}

public class TraitBoundViolationException : SemanticException
{
    public LangPath TypePath { get; }
    public LangPath TraitPath { get; }
    public TraitBoundViolationException(LangPath typePath, LangPath traitPath)
        : base($"The type '{typePath}' does not implement trait '{traitPath}'")
    {
        TypePath = typePath;
        TraitPath = traitPath;
    }
}

public class TraitNotFoundException : SemanticException
{
    public LangPath TraitPath { get; }
    public TraitNotFoundException(LangPath traitPath, string location)
        : base($"Trait '{traitPath}' not found\n{location}")
    {
        TraitPath = traitPath;
    }
}

public class TraitMethodNotImplementedException : SemanticException
{
    public string MethodName { get; }
    public LangPath TraitPath { get; }
    public TraitMethodNotImplementedException(string methodName, LangPath traitPath, string location)
        : base($"Method '{methodName}' from trait '{traitPath}' is not implemented\n{location}")
    {
        MethodName = methodName;
        TraitPath = traitPath;
    }
}

public class TraitExtraMethodException : SemanticException
{
    public string MethodName { get; }
    public LangPath TraitPath { get; }
    public TraitExtraMethodException(string methodName, LangPath traitPath, string location)
        : base($"Method '{methodName}' is not defined in trait '{traitPath}'\n{location}")
    {
        MethodName = methodName;
        TraitPath = traitPath;
    }
}

public class FunctionNotFoundException : SemanticException
{
    public LangPath FunctionPath { get; }
    public FunctionNotFoundException(LangPath functionPath, string location)
        : base($"Cannot find function {functionPath}\n{location}")
    {
        FunctionPath = functionPath;
    }
}

public class TypeMismatchException : SemanticException
{
    public LangPath ExpectedType { get; }
    public LangPath FoundType { get; }
    public string Context { get; }
    public TypeMismatchException(LangPath expectedType, LangPath foundType, string context, string location)
        : base($"{context}: expected '{expectedType}', found '{foundType}'\n{location}")
    {
        ExpectedType = expectedType;
        FoundType = foundType;
        Context = context;
    }
}

public class GenericParamCountException : SemanticException
{
    public int Expected { get; }
    public int Found { get; }
    public GenericParamCountException(int expected, int found, string location)
        : base($"Incorrect number of generic parameters: {found}, expected: {expected}\n{location}")
    {
        Expected = expected;
        Found = found;
    }
}

public class UndefinedVariableException : SemanticException
{
    public LangPath VariablePath { get; }
    public UndefinedVariableException(LangPath variablePath, string location)
        : base($"Path to variable '{variablePath}' not found, or the path is not a variable\n{location}")
    {
        VariablePath = variablePath;
    }
}

public class ReturnTypeMismatchException : SemanticException
{
    public LangPath ExpectedType { get; }
    public LangPath FoundType { get; }
    public ReturnTypeMismatchException(LangPath expectedType, LangPath foundType, string location)
        : base($"Return type of function does not match its definition. Expected: '{expectedType}', found: '{foundType}'\n{location}")
    {
        ExpectedType = expectedType;
        FoundType = foundType;
    }
}

public class UseAfterMoveException : SemanticException
{
    public LangPath VariablePath { get; }
    public UseAfterMoveException(LangPath variablePath, string location)
        : base($"Use of moved value '{variablePath}'\n{location}")
    {
        VariablePath = variablePath;
    }
}

public class CannotInferGenericArgsException : SemanticException
{
    public string TypeOrFunctionName { get; }
    public CannotInferGenericArgsException(string name, string location)
        : base($"Cannot infer generic type arguments for '{name}'. Consider adding explicit type annotations.\n{location}")
    {
        TypeOrFunctionName = name;
    }
}

public class InferredTypeMismatchException : SemanticException
{
    public LangPath ExpectedType { get; }
    public LangPath InferredType { get; }
    public InferredTypeMismatchException(LangPath expected, LangPath inferred, string location)
        : base($"Inferred type '{inferred}' conflicts with declared type '{expected}'\n{location}")
    {
        ExpectedType = expected;
        InferredType = inferred;
    }
}

public class DuplicateDefinitionException : SemanticException
{
    public LangPath DefinitionPath { get; }
    public DuplicateDefinitionException(LangPath definitionPath, string location)
        : base($"Duplicate definition '{definitionPath}'\n{location}")
    {
        DefinitionPath = definitionPath;
    }
}

public class BorrowInvalidatedException : SemanticException
{
    public string VariableName { get; }
    public BorrowInvalidatedException(string variableName, string location)
        : base($"Cannot use '{variableName}': the value it borrows from has been invalidated (shadowed or out of scope)\n{location}")
    {
        VariableName = variableName;
    }
}

public class NonExhaustiveMatchException : SemanticException
{
    public string VariantName { get; }
    public NonExhaustiveMatchException(string variantName, string location)
        : base($"Non-exhaustive match: variant '{variantName}' not covered\n{location}")
    {
        VariantName = variantName;
    }
}

public class DerefNonReferenceException : SemanticException
{
    public LangPath TypePath { get; }
    public DerefNonReferenceException(LangPath typePath, string location)
        : base($"Cannot dereference non-reference type '{typePath}'\n{location}")
    {
        TypePath = typePath;
    }
}

public class MoveOutOfReferenceException : SemanticException
{
    public LangPath TypePath { get; }
    public MoveOutOfReferenceException(LangPath typePath, string location)
        : base($"Cannot move out of shared reference '&{typePath}' — type '{typePath}' does not implement Copy\n{location}")
    {
        TypePath = typePath;
    }
}

public class DanglingReferenceException : SemanticException
{
    public DanglingReferenceException(string location)
        : base($"Borrowed value does not live long enough\n{location}")
    {
    }
}

public class BorrowConflictException : SemanticException
{
    public string Source { get; }
    public RefKind NewKind { get; }
    public RefKind ExistingKind { get; }
    public string ExistingBorrower { get; }
    public BorrowConflictException(string source, string existingBorrower, RefKind existingKind, RefKind newKind, string location)
        : base($"Cannot create &{RefTypeDefinition.GetRefName(newKind)} borrow of '{source}': " +
               $"conflicts with existing &{RefTypeDefinition.GetRefName(existingKind)} borrow '{existingBorrower}'\n{location}")
    {
        Source = source;
        ExistingBorrower = existingBorrower;
        NewKind = newKind;
        ExistingKind = existingKind;
    }
}

public class TraitImplBoundsMismatchException : SemanticException
{
    public TraitImplBoundsMismatchException(string details, string location)
        : base($"{details}\n{location}")
    {
        Details = details;
    }
    public string Details { get; }
}


public class SemanticAnalyzer
{
    private readonly Stack<Dictionary<LangPath, IDefinition>> DefinitionsStackMap = [];
    private readonly List<SemanticException> Exceptions = [];

    
    private readonly Stack<Dictionary<LangPath, LangPath>> VariableToTypeMapper = new();

    /// <summary>
    /// Tracks variables that have been moved (consumed by assignment or function call).
    /// Scoped so that moves inside an inner block don't affect outer variables with the same name.
    /// </summary>
    private readonly Stack<HashSet<string>> MovedVariablesStack = new();

    /// <summary>
    /// When true, PathExpression.Analyze skips the move check.
    /// Used by AssignVariableExpression so the LHS of assignment doesn't
    /// trigger use-after-move (assignment restores usability).
    /// </summary>
    public bool SuppressMoveChecks { get; set; }

    /// <summary>
    /// Names of the current function's parameters. References to these can be returned.
    /// </summary>
    private HashSet<string> _functionParameterNames = new();

    /// <summary>
    /// Variables that hold references pointing to local variables (not parameters).
    /// If returned, this would be a dangling reference.
    /// </summary>
    private readonly HashSet<string> _referencesToLocals = new();

    /// <summary>
    /// Tracks which variables borrow from which source.
    /// Key: source variable name, Value: set of borrowing variable names.
    /// When the source is shadowed or goes out of scope, all borrowers are invalidated.
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _borrowSources = new();

    /// <summary>
    /// Tracks active borrows per source variable with their RefKind.
    /// Key: source variable name, Value: list of (borrower name, RefKind).
    /// Used to enforce borrow compatibility rules.
    /// </summary>
    private readonly Dictionary<string, List<(string borrower, RefKind kind)>> _activeBorrows = new();

    /// <summary>
    /// Set of variable names whose borrows have been invalidated
    /// (the thing they borrowed from was shadowed or went out of scope).
    /// </summary>
    private readonly HashSet<string> _invalidatedBorrows = new();

    /// <summary>
    /// Tracks which scope level each variable was declared at,
    /// so we know which borrows to invalidate when a scope is popped.
    /// Stack of sets of variable names declared in that scope.
    /// </summary>
    private readonly Stack<HashSet<string>> _scopeVariables = new();

    /// <summary>
    /// Register that <paramref name="borrower"/> borrows from <paramref name="source"/>.
    /// </summary>
    public void RegisterBorrow(string source, string borrower, RefKind kind = RefKind.Shared)
    {
        if (!_borrowSources.TryGetValue(source, out var set))
        {
            set = new HashSet<string>();
            _borrowSources[source] = set;
        }
        set.Add(borrower);
        // If borrower was previously invalidated, clear it (fresh borrow)
        _invalidatedBorrows.Remove(borrower);

        // Track active borrows with their kind for compatibility checking
        if (!_activeBorrows.TryGetValue(source, out var activeList))
        {
            activeList = new List<(string, RefKind)>();
            _activeBorrows[source] = activeList;
        }
        activeList.Add((borrower, kind));
    }

    /// <summary>
    /// Check borrow compatibility rules. Returns the conflicting RefKind if incompatible, null if ok.
    /// Rules:
    ///   &amp;T + &amp;T: ok,  &amp;T + &amp;const: ok,  &amp;T + &amp;mut: ok,  &amp;T + &amp;uniq: CONFLICT
    ///   &amp;const + &amp;const: ok,  &amp;const + &amp;mut: CONFLICT
    ///   &amp;mut + &amp;mut: ok,  &amp;mut + &amp;uniq: CONFLICT
    ///   &amp;uniq + anything: CONFLICT
    /// </summary>
    public (string borrower, RefKind existingKind)? CheckBorrowCompatibility(string source, RefKind newKind)
    {
        if (!_activeBorrows.TryGetValue(source, out var activeList))
            return null;

        foreach (var (borrower, existingKind) in activeList)
        {
            if (!AreRefKindsCompatible(existingKind, newKind))
                return (borrower, existingKind);
        }
        return null;
    }

    private static bool AreRefKindsCompatible(RefKind a, RefKind b)
    {
        // &uniq is incompatible with everything
        if (a == RefKind.Uniq || b == RefKind.Uniq) return false;
        // &const and &mut are incompatible with each other
        if ((a == RefKind.Const && b == RefKind.Mut) || (a == RefKind.Mut && b == RefKind.Const))
            return false;
        return true;
    }

    /// <summary>
    /// Invalidate all borrows from <paramref name="source"/> (called on shadowing or scope exit).
    /// </summary>
    public void InvalidateBorrowsFrom(string source)
    {
        if (_borrowSources.TryGetValue(source, out var borrowers))
        {
            foreach (var b in borrowers)
                _invalidatedBorrows.Add(b);
            _borrowSources.Remove(source);
        }
        _activeBorrows.Remove(source);
    }

    /// <summary>
    /// Check if a variable's borrow has been invalidated.
    /// </summary>
    public bool IsBorrowInvalidated(string variableName) => _invalidatedBorrows.Contains(variableName);

    /// <summary>
    /// Track a variable declared in the current scope for lifetime tracking.
    /// </summary>
    public void TrackScopeVariable(string name)
    {
        if (_scopeVariables.Count > 0)
            _scopeVariables.Peek().Add(name);
    }

    /// <summary>
    /// Check if a variable was declared in the current (innermost) scope.
    /// </summary>
    public bool IsVariableInCurrentScope(string name)
    {
        return _scopeVariables.Count > 0 && _scopeVariables.Peek().Contains(name);
    }

    /// <summary>
    /// Check if an expression borrows from a variable in the current scope
    /// (meaning the borrow would dangle if the block's value escapes).
    /// </summary>
    public bool IsExpressionBorrowingFromCurrentScope(IExpression expr)
    {
        if (expr is PointerGetterExpression pge && pge.BorrowOriginName != null)
            return IsVariableInCurrentScope(pge.BorrowOriginName);

        if (expr is PathExpression pe && pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            var name = nlp.PathSegments[0].ToString();
            return IsLocalBorrow(name) && IsVariableInCurrentScope(name);
        }

        return false;
    }

    /// <summary>
    /// Set the current function's parameter names for lifetime analysis.
    /// </summary>
    public void SetFunctionParameters(IEnumerable<string> names)
    {
        _functionParameterNames = new HashSet<string>(names);
        _referencesToLocals.Clear();
    }

    /// <summary>
    /// Check if a variable is a function parameter (not a local).
    /// </summary>
    public bool IsFunctionParameter(string name) => _functionParameterNames.Contains(name);

    /// <summary>
    /// Mark a variable as holding a reference to a local (cannot be returned).
    /// </summary>
    public void MarkAsLocalBorrow(string name) => _referencesToLocals.Add(name);

    /// <summary>
    /// Check if a variable holds a reference to a local variable.
    /// </summary>
    public bool IsLocalBorrow(string name) => _referencesToLocals.Contains(name);

    /// <summary>
    /// Check if an expression, when used as a return value, would return a dangling reference.
    /// </summary>
    public bool IsExpressionLocalBorrow(IExpression expr)
    {
        // Direct borrow of a local: &local_var
        if (expr is PointerGetterExpression pge)
        {
            if (pge.BorrowOriginName != null && !IsFunctionParameter(pge.BorrowOriginName))
                return true;
            return false;
        }

        // Variable that holds a local borrow: let r = &local; return r;
        if (expr is PathExpression pe && pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            var name = nlp.PathSegments[0].ToString();
            if (IsLocalBorrow(name))
                return true;
            return false;
        }

        return false;
    }

    /// <summary>
    /// Tracks trait bounds for generic parameters currently in scope.
    /// Maps generic param name -> (traitPath, genericParamName, assocTypeConstraints)
    /// </summary>
    private readonly Stack<List<(LangPath traitPath, string genericParamName, Dictionary<string, LangPath>? assocConstraints)>> TraitBoundsStack = new();

    /// <summary>
    /// All impl definitions for trait method validation
    /// </summary>
    public List<ImplDefinition> ImplDefinitions { get; } = new();

    public Stack<ParseResult> ParseResults = new();

    public SemanticAnalyzer(IEnumerable<ParseResult> parseResults)
    {
        ParseResults = new Stack<ParseResult>(parseResults);
    }

    public void PushTraitBounds(IEnumerable<(LangPath traitPath, string genericParamName, Dictionary<string, LangPath>? assocConstraints)> bounds)
    {
        TraitBoundsStack.Push(bounds.ToList());
    }

    public void PopTraitBounds()
    {
        if (TraitBoundsStack.Count > 0)
            TraitBoundsStack.Pop();
    }

    /// <summary>
    /// Gets all trait definitions associated with trait bounds for a given generic param name
    /// </summary>
    public IEnumerable<TraitDefinition> GetTraitBoundsFor(string genericParamName)
    {
        foreach (var bounds in TraitBoundsStack)
            foreach (var (traitPath, paramName, _) in bounds)
                if (paramName == genericParamName)
                    if (GetDefinition(traitPath) is TraitDefinition td)
                        yield return td;
    }

    /// <summary>
    /// Checks if a name is currently in scope as a generic parameter.
    /// </summary>
    public bool IsGenericParam(string name)
    {
        foreach (var bounds in TraitBoundsStack)
            foreach (var (_, paramName, _) in bounds)
                if (paramName == name)
                    return true;
        return false;
    }

    /// <summary>
    /// Checks if a path resolves to a trait method call (TraitName::method)
    /// and returns the method's return type path
    /// </summary>
    public LangPath? ResolveTraitMethodReturnType(NormalLangPath path)
    {
        if (path.PathSegments.Length < 2) return null;

        // Strip trailing method-level generics (turbofish) if present
        // e.g., T::kk::<U> → strip <U> to get T::kk
        var workingPath = path;
        if (workingPath.GetFrontGenerics().Length > 0)
            workingPath = workingPath.PopGenerics()!;

        if (workingPath.PathSegments.Length < 2) return null;

        var lastSeg = workingPath.GetLastPathSegment();
        if (lastSeg == null) return null;
        var methodName = lastSeg.ToString();
        var parentPath = workingPath.Pop();
        if (parentPath == null || parentPath.PathSegments.Length == 0) return null;

        // Case 1: TraitName::method — parent is a trait directly (strip generics for lookup)
        var traitLookupPath = parentPath;
        if (parentPath is NormalLangPath nlpParentTrait && nlpParentTrait.GetFrontGenerics().Length > 0)
            traitLookupPath = nlpParentTrait.PopGenerics();
        var traitDef = GetDefinition(traitLookupPath) as TraitDefinition;

        // Case 2: T::method — parent is a generic param with trait bound(s)
        if (traitDef == null && parentPath is NormalLangPath nlpParent && nlpParent.PathSegments.Length == 1)
        {
            var paramName = nlpParent.PathSegments[0].ToString();
            // Search all bounds for this param to find one that has the method
            traitDef = GetTraitBoundsFor(paramName)
                .FirstOrDefault(td => td.GetMethod(methodName) != null);
        }

        // Case 3: ConcreteType::method — parent is a concrete type, search impls for a trait with the method
        if (traitDef == null)
        {
            var typeDef = GetDefinition(parentPath);
            if (typeDef != null && typeDef is not TraitDefinition)
            {
                // Search all impls where ForTypePath pattern-matches this type
                foreach (var impl in ImplDefinitions)
                {
                    var bindings = impl.TryMatchConcreteType(parentPath);
                    if (bindings != null && impl.CheckBounds(bindings, this))
                    {
                        var implTraitLookup = impl.TraitPath;
                        if (implTraitLookup is NormalLangPath nlpImplT && nlpImplT.GetFrontGenerics().Length > 0)
                            implTraitLookup = nlpImplT.PopGenerics();
                        var implTraitDef = GetDefinition(implTraitLookup) as TraitDefinition;
                        if (implTraitDef?.GetMethod(methodName) != null)
                        {
                            traitDef = implTraitDef;
                            var method = traitDef.GetMethod(methodName);
                            var returnType = method!.ReturnTypePath;
                            if (returnType is NormalLangPath nlpSelf && nlpSelf.PathSegments.Length == 1
                                && nlpSelf.PathSegments[0].ToString() == "Self")
                            {
                                return parentPath;
                            }
                            return returnType;
                        }
                    }
                }
            }
        }

        if (traitDef == null) return null;

        var foundMethod = traitDef.GetMethod(methodName);
        if (foundMethod == null) return null;

        var foundReturnType = foundMethod.ReturnTypePath;

        // If the return type is "Self", substitute it with the generic parameter
        // that has this trait as its bound
        if (foundReturnType is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            var retName = nlp.PathSegments[0].ToString();

            if (retName == "Self")
            {
                var traitTypePath = (traitDef as IDefinition).TypePath;
                foreach (var bounds in TraitBoundsStack)
                    foreach (var (tp, paramName, _) in bounds)
                        if (tp == traitTypePath)
                            return new NormalLangPath(null, [paramName]);
            }

            // Check if it's an associated type of this trait
            var assocType = traitDef.GetAssociatedType(retName);
            if (assocType != null)
            {
                // Try to resolve via the concrete type from qualified call or trait bounds
                // This will be finalized in FunctionCallExpression.Analyze
                // Return a marker that includes trait info for later resolution
                return foundReturnType;
            }
        }

        return foundReturnType;
    }

    /// <summary>
    /// Checks whether a type has an impl block for the given trait.
    /// Handles concrete impls, generic impls via pattern matching,
    /// generic params with trait bounds, and generic trait paths (e.g., Add&lt;i32&gt;).
    /// </summary>
    public bool TypeImplementsTrait(LangPath typePath, LangPath traitPath)
    {
        // Check if typePath is a generic param with this trait as a bound
        if (typePath is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            var paramName = nlp.PathSegments[0].ToString();
            foreach (var bounds in TraitBoundsStack)
                foreach (var (tp, pName, _) in bounds)
                    if (pName == paramName && tp == traitPath)
                        return true;

            // Check via supertraits: if T: Foo and Foo: Bar, then T satisfies Bar
            foreach (var bounds in TraitBoundsStack)
                foreach (var (tp, pName, _) in bounds)
                    if (pName == paramName)
                    {
                        var boundTraitBase = tp;
                        if (tp is NormalLangPath nlpBound && nlpBound.GetFrontGenerics().Length > 0)
                            boundTraitBase = nlpBound.PopGenerics();
                        var boundTraitDef = GetDefinition(boundTraitBase) as TraitDefinition;
                        if (boundTraitDef != null && HasSupertraitTransitive(boundTraitDef, traitPath))
                            return true;
                    }
        }

        // Strip generics from traitPath for base comparison
        var traitBase = traitPath;
        ImmutableArray<LangPath> traitGenericArgs = [];
        if (traitPath is NormalLangPath nlpTrait && nlpTrait.GetFrontGenerics().Length > 0)
        {
            traitGenericArgs = nlpTrait.GetFrontGenerics();
            traitBase = nlpTrait.PopGenerics();
        }

        // Check concrete and generic impls
        return ImplDefinitions.Any(i =>
        {
            // Compare trait base paths
            var implTraitBase = i.TraitPath;
            ImmutableArray<LangPath> implTraitGenericArgs = [];
            if (i.TraitPath is NormalLangPath nlpImplTrait && nlpImplTrait.GetFrontGenerics().Length > 0)
            {
                implTraitGenericArgs = nlpImplTrait.GetFrontGenerics();
                implTraitBase = nlpImplTrait.PopGenerics();
            }

            if (implTraitBase != traitBase) return false;

            // Match the implementing type
            var bindings = i.TryMatchConcreteType(typePath);
            if (bindings == null) return false;

            // Also unify trait generic args if present
            if (traitGenericArgs.Length > 0 || implTraitGenericArgs.Length > 0)
            {
                if (traitGenericArgs.Length != implTraitGenericArgs.Length) return false;
                var freeVars = i.GenericParameters.Select(gp => gp.Name).ToHashSet();
                for (int idx = 0; idx < traitGenericArgs.Length; idx++)
                {
                    if (!TypeInference.TryUnify(implTraitGenericArgs[idx], traitGenericArgs[idx], freeVars, bindings))
                        return false;
                }
            }

            return i.CheckBounds(bindings, this);
        });
    }

    /// <summary>
    /// Checks if <paramref name="traitDef"/> has <paramref name="targetTrait"/> as a supertrait (transitively).
    /// </summary>
    private bool HasSupertraitTransitive(TraitDefinition traitDef, LangPath targetTrait)
    {
        foreach (var supertrait in traitDef.Supertraits)
        {
            if (supertrait == targetTrait) return true;
            // Recurse: check supertraits of supertraits
            var superBase = supertrait;
            if (supertrait is NormalLangPath nlpS && nlpS.GetFrontGenerics().Length > 0)
                superBase = nlpS.PopGenerics();
            var superDef = GetDefinition(superBase) as TraitDefinition;
            if (superDef != null && HasSupertraitTransitive(superDef, targetTrait))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Resolves an associated type for a given implementing type and trait.
    /// E.g., for &lt;i32 as Add&gt;::Output, finds the impl of Add for i32 and returns Output's concrete type.
    /// Also handles stripping trait generics for matching.
    /// First checks trait bound associated type constraints (e.g., T: Add&lt;T, Output = T&gt;).
    /// </summary>
    public LangPath? ResolveAssociatedType(LangPath forType, LangPath traitPath, string associatedTypeName)
    {
        // Check trait bound associated type constraints first
        // e.g., for T: Add<T, Output = T>, resolving <T as Add<T>>::Output gives T
        if (forType is NormalLangPath nlpFor && nlpFor.PathSegments.Length == 1)
        {
            var paramName = nlpFor.PathSegments[0].ToString();
            foreach (var bounds in TraitBoundsStack)
                foreach (var (tp, pName, assocConstraints) in bounds)
                    if (pName == paramName && tp == traitPath && assocConstraints != null)
                        if (assocConstraints.TryGetValue(associatedTypeName, out var constrainedType))
                            return constrainedType;
        }

        // Strip trait generics for lookup
        var traitLookupPath = traitPath;
        if (traitPath is NormalLangPath nlpTrait && nlpTrait.GetFrontGenerics().Length > 0)
            traitLookupPath = nlpTrait.PopGenerics();

        foreach (var impl in ImplDefinitions)
        {
            // Match trait path (strip generics on impl trait path too)
            var implTraitLookup = impl.TraitPath;
            if (implTraitLookup is NormalLangPath nlpImplTrait && nlpImplTrait.GetFrontGenerics().Length > 0)
                implTraitLookup = nlpImplTrait.PopGenerics();

            if (implTraitLookup != traitLookupPath) continue;

            var bindings = impl.TryMatchConcreteType(forType);
            if (bindings == null) continue;
            if (!impl.CheckBounds(bindings, this)) continue;

            var at = impl.AssociatedTypeAssignments.FirstOrDefault(a => a.Name == associatedTypeName);
            if (at != null)
            {
                // Substitute any impl generic params in the associated type
                var result = at.ConcreteType;
                foreach (var gp in impl.GenericParameters)
                    if (bindings.TryGetValue(gp.Name, out var bound))
                        result = FieldAccessExpression.SubstituteGenerics(result,
                            impl.GenericParameters, TypeInference.BuildGenericArgs(impl.GenericParameters, bindings) ?? []);
                return result;
            }
        }
        return null;
    }

    /// <summary>
    /// Resolves a type path that may contain qualified associated types.
    /// Handles:
    /// - QualifiedAssocTypePath: &lt;i32 as Add&lt;i32&gt;&gt;::Output → i32
    /// - T::Output where T is a generic param with trait bound → resolved via constraints
    /// - ConcreteType::Output where a unique impl provides Output → resolved via impl search
    /// Returns the resolved path, or the original path if no resolution applies.
    /// </summary>
    public LangPath ResolveQualifiedTypePath(LangPath path)
    {
        // Case 1: Explicit qualified path <Type as Trait>::AssocType
        if (path is QualifiedAssocTypePath qp)
        {
            var resolvedFor = ResolveQualifiedTypePath(qp.ForType);
            var resolvedTrait = ResolveQualifiedTypePath(qp.TraitPath);
            var result = ResolveAssociatedType(resolvedFor, resolvedTrait, qp.AssociatedTypeName);
            return result ?? path;
        }

        // Case 2 & 3: NormalLangPath with T::AssocType or ConcreteType::AssocType
        if (path is NormalLangPath nlp && nlp.PathSegments.Length >= 2)
        {
            var firstName = nlp.PathSegments[0].ToString();
            var lastName = nlp.GetLastPathSegment()?.ToString();
            if (lastName == null) return path;

            // Case 2: first segment is a generic param (T::Output)
            if (nlp.PathSegments.Length == 2
                && nlp.PathSegments[0] is NormalLangPath.NormalPathSegment
                && nlp.PathSegments[1] is NormalLangPath.NormalPathSegment
                && IsGenericParam(firstName))
            {
                var paramPath = new NormalLangPath(null, [firstName]);
                foreach (var bounds in TraitBoundsStack)
                {
                    foreach (var (tp, pName, assocConstraints) in bounds)
                    {
                        if (pName != firstName) continue;
                        // Check explicit constraint (e.g., Output = T)
                        if (assocConstraints != null && assocConstraints.TryGetValue(lastName, out var constrained))
                            return constrained;
                        // Check if the trait has this associated type
                        var traitBasePath = tp;
                        if (tp is NormalLangPath nlpTp && nlpTp.GetFrontGenerics().Length > 0)
                            traitBasePath = nlpTp.PopGenerics();
                        var traitDef = GetDefinition(traitBasePath) as TraitDefinition;
                        if (traitDef?.GetAssociatedType(lastName) != null)
                            return new QualifiedAssocTypePath(paramPath, tp, lastName);
                    }
                }
            }

            // Case 3: ConcreteType::AssocType — search impls for a unique match
            var parentPath = nlp.Pop();
            if (parentPath != null && parentPath.PathSegments.Length > 0
                && nlp.GetLastPathSegment() is NormalLangPath.NormalPathSegment lastSeg)
            {
                var assocName = lastSeg.ToString();
                var typeDef = GetDefinition(parentPath);
                if (typeDef != null && typeDef is not TraitDefinition)
                {
                    var matches = new List<LangPath>();
                    foreach (var impl in ImplDefinitions)
                    {
                        var bindings = impl.TryMatchConcreteType(parentPath);
                        if (bindings == null) continue;
                        var at = impl.AssociatedTypeAssignments.FirstOrDefault(a => a.Name == assocName);
                        if (at != null)
                        {
                            var result = at.ConcreteType;
                            if (impl.GenericParameters.Length > 0)
                            {
                                var args = TypeInference.BuildGenericArgs(impl.GenericParameters, bindings);
                                if (args != null)
                                    result = FieldAccessExpression.SubstituteGenerics(
                                        result, impl.GenericParameters, args.Value);
                            }
                            matches.Add(result);
                        }
                    }
                    if (matches.Count == 1)
                        return matches[0];
                    if (matches.Count > 1)
                    {
                        AddException(new SemanticException(
                            $"Ambiguous associated type '{assocName}' for type '{parentPath}'. " +
                            $"Use qualified syntax: <{parentPath} as Trait>::{assocName}\n" +
                            $"{path.FirstIdentifierToken?.GetLocationStringRepresentation() ?? ""}"));
                        return matches[0];
                    }
                }
            }
        }

        return path;
    }

    /// <summary>
    /// Checks whether a type implements the Copy trait.
    /// Copy types are bitwise-copied on assignment; non-Copy types are moved.
    /// Also recognizes generic params that have Copy as a trait bound.
    /// </summary>
    public bool IsTypeCopy(LangPath? typePath)
    {
        if (typePath == null) return true;
        var copyPath = GetCopyTraitPath();
        if (copyPath == null) return true;

        // Check concrete impl
        if (TypeImplementsTrait(typePath, copyPath)) return true;

        // References are Copy except &uniq T (which is unique/exclusive)
        if (typePath is NormalLangPath nlpPtr
            && nlpPtr.Contains(RefTypeDefinition.GetRefModule()))
        {
            // Check it's not &uniq — the path segment before the generic is the ref kind name
            var refKindName = RefTypeDefinition.GetRefName(RefKind.Uniq);
            bool isUniq = nlpPtr.PathSegments.Any(s => s.ToString() == refKindName);
            if (!isUniq) return true;
        }

        // Check if this is a generic param with a Copy trait bound
        if (typePath is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            var paramName = nlp.PathSegments[0].ToString();
            foreach (var bounds in TraitBoundsStack)
                foreach (var (tp, pName, _) in bounds)
                    if (pName == paramName && tp == copyPath)
                        return true;
        }

        return false;
    }

    /// <summary>
    /// The canonical path of the Copy marker trait: std::core::marker::Copy
    /// </summary>
    public static readonly NormalLangPath CopyTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "std", "core", "marker", "Copy" });

    public static readonly NormalLangPath AddTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "std", "core", "ops", "Add" });
    public static readonly NormalLangPath SubTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "std", "core", "ops", "Sub" });
    public static readonly NormalLangPath MulTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "std", "core", "ops", "Mul" });
    public static readonly NormalLangPath DivTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "std", "core", "ops", "Div" });

    private LangPath? GetCopyTraitPath()
    {
        return CopyTraitPath;
    }

    public void MarkAsMoved(string variableName)
    {
        if (MovedVariablesStack.Count > 0)
            MovedVariablesStack.Peek().Add(variableName);
    }

    public void UnmarkMoved(string variableName)
    {
        foreach (var scope in MovedVariablesStack)
            scope.Remove(variableName);
    }

    public bool IsMoved(string variableName)
    {
        foreach (var scope in MovedVariablesStack)
            if (scope.Contains(variableName))
                return true;
        return false;
    }

    /// <summary>
    /// If the expression is a simple variable reference to a non-Copy type, mark it as moved.
    /// </summary>
    public void TryMarkExpressionAsMoved(IExpression expr)
    {
        if (expr is PathExpression pe && expr.TypePath != null && !IsTypeCopy(expr.TypePath))
        {
            if (pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
            {
                MarkAsMoved(nlp.PathSegments[0].ToString());
            }
        }
    }

    public void AddException(SemanticException exception)
    {
        Exceptions.Add(exception);
    }


    public IDefinition? GetDefinition(LangPath langPath)
    {
        foreach (var def in DefinitionsStackMap)
        {
            if (def.TryGetValue(langPath, out var definition)) return definition;
        }
        return null;
    }

    public LangPath? GetVariableTypePath(LangPath variableLangPath)
    {
        foreach (var scope in VariableToTypeMapper)
            if (scope.TryGetValue(variableLangPath, out var symbol))
                return symbol;

        return null;
    }

    public void RegisterVariableType(LangPath variableLangPath, LangPath typPath, int? scope = null)
    {
        if (scope is null)
            VariableToTypeMapper.Peek()[variableLangPath] = typPath;
        else
            VariableToTypeMapper.Reverse().Skip(scope.Value).First()[variableLangPath] = typPath;
    }

    public void RegisterDefinitionAtDeepestScope(LangPath path, IDefinition definition)
    {
        DefinitionsStackMap.Peek()[path] = definition;

    }

    /// <summary>Current scope nesting depth. Deeper = shorter lifetime.</summary>
    public int CurrentScopeDepth { get; private set; }

    public void AddScope()
    {

        DefinitionsStackMap.Push(new ());
        VariableToTypeMapper.Push(new Dictionary<LangPath, LangPath>());
        MovedVariablesStack.Push(new HashSet<string>());
        _scopeVariables.Push(new HashSet<string>());
        CurrentScopeDepth++;
    }

    


    public void PopScope()
    {   
        DefinitionsStackMap.Pop();
     
        VariableToTypeMapper.Pop();
        MovedVariablesStack.Pop();
        CurrentScopeDepth--;

        // Invalidate borrows from variables going out of scope
        if (_scopeVariables.Count > 0)
        {
            var exiting = _scopeVariables.Pop();
            foreach (var varName in exiting)
                InvalidateBorrowsFrom(varName);
        }
    }

    private void ResolvePaths()
    {
        var pathShortcutContext = new PathResolver();
        
        pathShortcutContext.AddScope();
        var primitiveParsed = ParseResults.First(i => i.Items.Any(j => j is I32TypeDefinition));
        foreach (var i in primitiveParsed.Items.OfType<PrimitiveTypeDefinition>())
        {
            var usings = new UseDefinition((NormalLangPath)i.TypePath, null);
            usings.RegisterUsings(pathShortcutContext);
        }
        // Register std library definitions (like Copy) globally so they're available without 'use'
        // User definitions at deeper scopes will shadow these if names conflict
        foreach (var result in ParseResults.Where(r => r.File?.Path.StartsWith("std") == true))
            foreach (var def in result.Items.OfType<IDefinition>())
            {
                var usings = new UseDefinition((NormalLangPath)def.TypePath, null);
                usings.RegisterUsings(pathShortcutContext);
            }
        foreach (var result in ParseResults)
        {
            pathShortcutContext.AddScope();
            foreach (var i in result.Items.OfType<IDefinition>())
            {
                var usings = new UseDefinition((NormalLangPath) i.TypePath, null);
                usings.RegisterUsings(pathShortcutContext);
            }
            foreach (var useDefinition in result.Items.OfType<UseDefinition>())
            {
                useDefinition.RegisterUsings(pathShortcutContext);
            }
            foreach (var i in result.Items.OfType<IPathResolvable>())
            {
                i.ResolvePaths(pathShortcutContext);
            }
            pathShortcutContext.PopScope();
        }
        pathShortcutContext.PopScope();
    }
    /// <returns>Collection of semantic errors that occured</returns>
    public SemanticException[] Analyze()
    {
        AddScope();
        // registers path mapping — check for duplicates
        var seenDefinitions = new Dictionary<LangPath, IDefinition>();
        foreach (var i in ParseResults.SelectMany(i => i.Items.OfType<IDefinition>()))
        {
            if (seenDefinitions.TryGetValue(i.TypePath, out var existing))
            {
                AddException(new DuplicateDefinitionException(
                    i.TypePath, i.Token?.GetLocationStringRepresentation() ?? ""));
            }
            else
            {
                seenDefinitions[i.TypePath] = i;
            }
            RegisterDefinitionAtDeepestScope(i.TypePath, i);
        }

        // Collect impl definitions
        foreach (var impl in ParseResults.SelectMany(i => i.Items.OfType<ImplDefinition>()))
        {
            ImplDefinitions.Add(impl);
            // Register each impl method as a definition so it can be found during codegen
            foreach (var method in impl.Methods)
                RegisterDefinitionAtDeepestScope(method.TypePath, method);
        }

        ResolvePaths();

        foreach (var result in ParseResults)
        {
            AddScope();
      
            foreach (var i in result.Items.OfType<IAnalyzable>())
            {
                i.Analyze(this);
            }
            PopScope();
        }
        PopScope();

        return Exceptions.ToArray();
    }
}