using System.Collections.Immutable;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Parse.Statements;

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

public class UseWhileBorrowedException : SemanticException
{
    public string Source { get; }
    public string Borrower { get; }
    public RefKind BorrowKind { get; }
    public UseWhileBorrowedException(string source, string borrower, RefKind borrowKind, string location)
        : base($"Cannot use '{source}' because it was borrowed as &{RefTypeDefinition.GetRefName(borrowKind)} by '{borrower}'\n{location}")
    {
        Source = source;
        Borrower = borrower;
        BorrowKind = borrowKind;
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

public class SupertraitNotImplementedException : SemanticException
{
    public LangPath TypePath { get; }
    public LangPath TraitPath { get; }
    public LangPath SupertraitPath { get; }
    public SupertraitNotImplementedException(LangPath typePath, LangPath traitPath, LangPath supertraitPath, string location)
        : base($"Type '{typePath}' implements '{traitPath}' but does not implement supertrait '{supertraitPath}'\n{location}")
    {
        TypePath = typePath;
        TraitPath = traitPath;
        SupertraitPath = supertraitPath;
    }
}

public class CopyDropConflictException : SemanticException
{
    public LangPath TypePath { get; }
    public CopyDropConflictException(LangPath typePath, string location)
        : base($"Type '{typePath}' cannot implement both Copy and Drop\n{location}")
    {
        TypePath = typePath;
    }
}

public class DropGenericsMismatchException : SemanticException
{
    public LangPath TypePath { get; }
    public string Details { get; }
    public DropGenericsMismatchException(LangPath typePath, string details, string location)
        : base($"{details}\n{location}")
    {
        TypePath = typePath;
        Details = details;
    }
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
    /// When true, PathExpression.Analyze skips the "use while exclusively borrowed" check.
    /// Set by PointerGetterExpression so that re-borrowing (e.g., &amp;uniq a when a is already
    /// &amp;uniq-borrowed) is handled by the existing borrow compatibility rules, not the source-use check.
    /// </summary>
    public bool SuppressUseWhileBorrowedChecks { get; set; }

    /// <summary>
    /// Tracks which traits are in scope (imported via `use` or defined in the current file).
    /// Trait methods can only be called when their trait is in scope.
    /// Scoped so that inner blocks can import traits without leaking to outer scope.
    /// Stores base paths (generics stripped).
    /// Uses List because NormalLangPath overrides Equals but not GetHashCode.
    /// </summary>
    private readonly Stack<List<LangPath>> _traitsInScope = new();

    public void ImportTrait(LangPath traitPath)
    {
        var stripped = LangPath.StripGenerics(traitPath);
        if (_traitsInScope.Count > 0 && !IsTraitInScope(stripped))
            _traitsInScope.Peek().Add(stripped);
    }

    public bool IsTraitInScope(LangPath traitPath)
    {
        var stripped = LangPath.StripGenerics(traitPath);
        foreach (var scope in _traitsInScope)
            if (scope.Any(p => p.Equals(stripped)))
                return true;
        return false;
    }

    /// <summary>
    /// NLL: stack of live-variable sets, one per block scope.
    /// Each entry tracks variables referenced from the current analysis point onward
    /// within that block. A variable is considered live if it appears in ANY scope on the stack.
    /// Using a stack handles variable shadowing correctly — an inner block's "x" is a different
    /// binding than the outer block's "x", and each scope tracks its own liveness independently.
    /// </summary>
    private readonly Stack<HashSet<string>> _liveVariablesStack = new();

    /// <summary>
    /// Pushes a new live-variable set for the current block scope.
    /// Called by BlockExpression at block entry.
    /// </summary>
    public void PushLiveVariables(HashSet<string> liveVars)
    {
        _liveVariablesStack.Push(liveVars);
    }

    /// <summary>
    /// Pops the current block scope's live-variable set.
    /// Called by BlockExpression at block exit.
    /// </summary>
    public void PopLiveVariables()
    {
        if (_liveVariablesStack.Count > 0)
            _liveVariablesStack.Pop();
    }

    /// <summary>
    /// Replaces the top of the live-variables stack as we advance through a block's items.
    /// </summary>
    public void UpdateLiveVariables(HashSet<string> liveVars)
    {
        if (_liveVariablesStack.Count > 0)
            _liveVariablesStack.Pop();
        _liveVariablesStack.Push(liveVars);
    }

    /// <summary>
    /// Checks whether a variable is referenced from the current point onward
    /// in any enclosing block scope. If the stack is empty, conservatively returns true.
    /// </summary>
    public bool IsVariableLive(string varName)
    {
        if (_liveVariablesStack.Count == 0) return true;
        foreach (var scope in _liveVariablesStack)
            if (scope.Contains(varName)) return true;
        return false;
    }

    /// <summary>
    /// Checks if a variable is live, either directly referenced or transitively
    /// kept alive by another variable that borrows from it.
    /// e.g., rr borrows r → if rr is live, r is transitively live.
    /// </summary>
    public bool IsVariableLiveTransitive(string varName)
    {
        var visited = new HashSet<string>();
        return IsVariableLiveTransitiveInner(varName, visited);
    }

    private bool IsVariableLiveTransitiveInner(string varName, HashSet<string> visited)
    {
        if (!visited.Add(varName)) return false; // cycle
        if (IsVariableLive(varName)) return true;

        foreach (var scope in _borrowScopes)
            if (scope.BorrowSources.TryGetValue(varName, out var borrowers))
                foreach (var borrower in borrowers)
                    if (IsVariableLiveTransitiveInner(borrower, visited))
                        return true;

        return false;
    }

    /// <summary>
    /// Collects all simple variable names referenced in an AST subtree.
    /// Used to pre-compute liveness for NLL borrow checking.
    /// </summary>
    public static void CollectReferencedVariables(ISyntaxNode node, HashSet<string> result)
    {
        // ChainExpression: any chain references its root variable.
        // f.get(), rr.val, x.method() all reference their root.
        // Over-collecting (e.g., Box.New adds "Box") is safe — no variable named "Box".
        if (node is ChainExpression chain)
            result.Add(chain.RootName);
        else if (node is PathExpression pe && pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
            result.Add(nlp.PathSegments[0].ToString());

        foreach (var child in node.Children)
            CollectReferencedVariables(child, result);
    }

    /// <summary>
    /// Names of the current function's parameters. References to these can be returned.
    /// </summary>
    private HashSet<string> _functionParameterNames = new();

    /// <summary>
    /// Variables that hold references pointing to local variables (not parameters).
    /// If returned, this would be a dangling reference.
    /// Scoped so that names don't leak after scope exit and falsely flag later same-named variables.
    /// </summary>
    private readonly Stack<HashSet<string>> _referencesToLocalsStack = new();

    /// <summary>
    /// Scoped borrow tracker — each scope has its own layer of borrow registrations.
    /// Lookups search all scopes (borrows cross scope boundaries).
    /// PopScope automatically cleans up borrows created in the exiting scope.
    /// </summary>
    private class BorrowScope
    {
        public readonly Dictionary<string, HashSet<string>> BorrowSources = new();
        public readonly Dictionary<string, string> BorrowerToSource = new();
        public readonly Dictionary<string, List<(string borrower, RefKind kind)>> ActiveBorrows = new();
        public readonly HashSet<string> InvalidatedBorrows = new();
        public readonly Dictionary<string, LangPath> BorrowerTypes = new();
    }

    private readonly Stack<BorrowScope> _borrowScopes = new();

    /// <summary>
    /// Tracks which scope level each variable was declared at,
    /// so we know which borrows to invalidate when a scope is popped.
    /// Stack of sets of variable names declared in that scope.
    /// </summary>
    private readonly Stack<HashSet<string>> _scopeVariables = new();

    /// <summary>
    /// Register that <paramref name="borrower"/> borrows from <paramref name="source"/>.
    /// Registered in the current (innermost) scope.
    /// </summary>
    public void RegisterBorrow(string source, string borrower, RefKind kind = RefKind.Shared, LangPath? borrowerType = null)
    {
        var scope = _borrowScopes.Peek();

        if (!scope.BorrowSources.TryGetValue(source, out var set))
        {
            set = new HashSet<string>();
            scope.BorrowSources[source] = set;
        }
        set.Add(borrower);
        scope.BorrowerToSource[borrower] = source;

        if (borrowerType != null)
            scope.BorrowerTypes[borrower] = borrowerType;

        // If borrower was previously invalidated in any scope, clear it (fresh borrow)
        foreach (var s in _borrowScopes)
            s.InvalidatedBorrows.Remove(borrower);

        // Track active borrows with their kind for compatibility checking
        if (!scope.ActiveBorrows.TryGetValue(source, out var activeList))
        {
            activeList = new List<(string, RefKind)>();
            scope.ActiveBorrows[source] = activeList;
        }
        activeList.Add((borrower, kind));
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
    /// NLL-style: invalidate any existing borrows from <paramref name="source"/>
    /// that conflict with <paramref name="newKind"/>. Searches all scopes.
    /// </summary>
    public void InvalidateConflictingBorrows(string source, RefKind newKind)
    {
        foreach (var scope in _borrowScopes)
        {
            if (!scope.ActiveBorrows.TryGetValue(source, out var activeList))
                continue;


            var toInvalidate = activeList
                .Where(b => !AreRefKindsCompatible(b.kind, newKind))
                .Select(b => b.borrower)
                .ToList();


            foreach (var borrower in toInvalidate)
            {
                // Invalidate in the borrower's own scope
                foreach (var s in _borrowScopes)
                    if (s.BorrowerToSource.ContainsKey(borrower))
                    {
                        s.InvalidatedBorrows.Add(borrower);
                        break;
                    }
                activeList.RemoveAll(b => b.borrower == borrower);
            }
        }
    }

    /// <summary>
    /// Invalidate all borrows from <paramref name="source"/> across all scopes.
    /// Called on shadowing or scope exit.
    /// </summary>
    public void InvalidateBorrowsFrom(string source)
    {
        foreach (var scope in _borrowScopes)
        {
            if (scope.BorrowSources.TryGetValue(source, out var borrowers))
            {
                foreach (var b in borrowers)
                {
                    // Mark invalidated in borrower's own scope
                    foreach (var s in _borrowScopes)
                        if (s.BorrowerToSource.ContainsKey(b))
                        {
                            s.InvalidatedBorrows.Add(b);
                            break;
                        }
                }
                borrowers.Clear();
            }
            scope.ActiveBorrows.Remove(source);
        }
    }

    /// <summary>
    /// Check if a variable's borrow has been invalidated (searches all scopes).
    /// </summary>
    public bool IsBorrowInvalidated(string variableName)
    {
        foreach (var scope in _borrowScopes)
            if (scope.InvalidatedBorrows.Contains(variableName))
            {
                return true;
            }
        return false;
    }

    /// <summary>
    /// Checks a variable for use-after-move, borrow invalidation, and exclusive borrow conflicts.
    /// Shared between ChainExpression and PathExpression variable access analysis.
    /// </summary>
    public void CheckVariableUsage(string varName, LangPath path, string locationString)
    {
        if (!SuppressMoveChecks && IsMoved(varName))
            AddException(new UseAfterMoveException(path, locationString));

        if (IsBorrowInvalidated(varName))
            AddException(new BorrowInvalidatedException(varName, locationString));

        if (!SuppressUseWhileBorrowedChecks)
        {
            var exclusiveBorrow = GetActiveExclusiveBorrow(varName);
            if (exclusiveBorrow != null)
                AddException(new UseWhileBorrowedException(
                    varName, exclusiveBorrow.Value.borrower, exclusiveBorrow.Value.kind, locationString));
        }
    }

    /// <summary>
    /// Returns the first active exclusive (&amp;uniq) borrow on the given source variable
    /// that is still active. For reference/Copy borrowers, NLL applies (borrow expires at last use).
    /// For non-Copy, non-reference borrowers, borrow persists until moved or scope exit
    /// (because Drop could access the borrowed data at scope exit).
    /// </summary>
    public (string borrower, RefKind kind)? GetActiveExclusiveBorrow(string sourceName)
    {
        foreach (var scope in _borrowScopes)
        {
            if (!scope.ActiveBorrows.TryGetValue(sourceName, out var activeList))
                continue;

            foreach (var (borrower, kind) in activeList)
            {
                if (kind != RefKind.Uniq) continue;

                // Look up borrower's type to determine NLL eligibility
                LangPath? borrowerType = null;
                foreach (var s in _borrowScopes)
                    if (s.BorrowerTypes.TryGetValue(borrower, out var bt))
                    { borrowerType = bt; break; }

                bool isNllEligible = borrowerType != null
                    && (RefTypeDefinition.IsReferenceType(borrowerType) || IsTypeCopy(borrowerType));

                if (isNllEligible)
                {
                    // References & Copy types: NLL — borrow expires when borrower is no longer live
                    if (IsVariableLiveTransitive(borrower))
                        return (borrower, kind);
                }
                else
                {
                    // Non-Copy, non-reference: borrow persists until moved or scope exit
                    if (!IsMoved(borrower))
                        return (borrower, kind);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Releases all borrows held BY a given borrower variable across all scopes.
    /// Called when the borrower goes out of scope.
    /// </summary>
    public void ReleaseBorrowsBy(string borrowerName)
    {
        foreach (var scope in _borrowScopes)
        {
            if (scope.BorrowerToSource.TryGetValue(borrowerName, out var source))
            {
                // Clean up from all scopes that might reference this source
                foreach (var s in _borrowScopes)
                {
                    if (s.ActiveBorrows.TryGetValue(source, out var activeList))
                        activeList.RemoveAll(b => b.borrower == borrowerName);
                    if (s.BorrowSources.TryGetValue(source, out var borrowers))
                        borrowers.Remove(borrowerName);
                }
                scope.BorrowerToSource.Remove(borrowerName);
            }
            scope.InvalidatedBorrows.Remove(borrowerName);
        }
    }

    /// <summary>
    /// Get the source variable that a given variable borrows from (searches all scopes).
    /// </summary>
    public string? GetBorrowSource(string borrower)
    {
        foreach (var scope in _borrowScopes)
            if (scope.BorrowerToSource.TryGetValue(borrower, out var source))
                return source;
        return null;
    }

    /// <summary>
    /// Get the full borrow info (source + kind) for a borrower variable.
    /// Returns null if the variable has no registered borrows.
    /// </summary>
    public (string source, RefKind kind)? GetBorrowInfo(string borrower)
    {
        foreach (var scope in _borrowScopes)
        {
            if (!scope.BorrowerToSource.TryGetValue(borrower, out var source)) continue;
            foreach (var s in _borrowScopes)
                if (s.ActiveBorrows.TryGetValue(source, out var activeList))
                    foreach (var (b, kind) in activeList)
                        if (b == borrower)
                            return (source, kind);
            return (source, RefKind.Shared);
        }
        return null;
    }

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

        var name = IExpression.TryGetSimpleVariableName(expr);
        if (name != null)
            return IsLocalBorrow(name) && IsVariableInCurrentScope(name);

        return false;
    }

    /// <summary>
    /// Set the current function's parameter names for lifetime analysis.
    /// </summary>
    public void SetFunctionParameters(IEnumerable<string> names)
    {
        _functionParameterNames = new HashSet<string>(names);
        _parameterLifetimes.Clear();
    }

    /// <summary>
    /// Register lifetime annotations for function parameters.
    /// </summary>
    public void SetParameterLifetimes(Dictionary<string, string> lifetimes)
    {
        _parameterLifetimes = new Dictionary<string, string>(lifetimes);
    }
    private Dictionary<string, string> _parameterLifetimes = new();

    /// <summary>
    /// Get the declared lifetime of a parameter, or null if none.
    /// </summary>
    public string? GetParameterLifetime(string paramName)
    {
        return _parameterLifetimes.TryGetValue(paramName, out var lt) ? lt : null;
    }

    /// <summary>
    /// Check if a variable is a function parameter (not a local).
    /// </summary>
    public bool IsFunctionParameter(string name) => _functionParameterNames.Contains(name);

    /// <summary>
    /// Mark a variable as holding a reference to a local (cannot be returned).
    /// </summary>
    public void MarkAsLocalBorrow(string name)
    {
        if (_referencesToLocalsStack.Count > 0)
            _referencesToLocalsStack.Peek().Add(name);
    }

    /// <summary>
    /// Check if a variable holds a reference to a local variable.
    /// Searches all scopes since a borrow from an inner scope could be visible in outer.
    /// </summary>
    public bool IsLocalBorrow(string name)
    {
        foreach (var scope in _referencesToLocalsStack)
            if (scope.Contains(name))
                return true;
        return false;
    }

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
        var name = IExpression.TryGetSimpleVariableName(expr);
        if (name != null)
            return IsLocalBorrow(name);

        return false;
    }

    /// <summary>
    /// Tracks trait bounds for generic parameters currently in scope.
    /// Maps generic param name -> (traitPath, genericParamName, assocTypeConstraints)
    /// </summary>
    private readonly Stack<List<(LangPath traitPath, string genericParamName, Dictionary<string, LangPath>? assocConstraints)>> TraitBoundsStack = new();

    /// <summary>
    /// <summary>
    /// Impl definitions for trait method validation, scoped so that
    /// impls defined inside blocks are only visible within that block.
    /// </summary>
    private readonly Stack<List<ImplDefinition>> _implDefinitionsStack = new();

    /// <summary>
    /// All impl definitions visible from the current scope (iterates all levels).
    /// </summary>
    public IEnumerable<ImplDefinition> ImplDefinitions =>
        _implDefinitionsStack.SelectMany(scope => scope);

    /// <summary>
    /// Register an impl definition in the current (deepest) scope.
    /// </summary>
    public void AddImplDefinition(ImplDefinition impl)
    {
        _implDefinitionsStack.Peek().Add(impl);
    }

    public Stack<ParseResult> ParseResults = new();

    public SemanticAnalyzer(IEnumerable<ParseResult> parseResults, NormalLangPath? crateRoot = null)
    {
        ParseResults = new Stack<ParseResult>(parseResults);
        CrateRoot = crateRoot;
    }

    /// <summary>
    /// The package root module path (e.g., "code" for a package in code/).
    /// Used by the 'crate' keyword in use statements.
    /// </summary>
    public NormalLangPath? CrateRoot { get; }

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
        var seen = new HashSet<LangPath>();
        foreach (var bounds in TraitBoundsStack)
            foreach (var (traitPath, paramName, _) in bounds)
                if (paramName == genericParamName)
                {
                    var lookupPath = LangPath.StripGenerics(traitPath);
                    if (GetDefinition(lookupPath) is TraitDefinition td && seen.Add(td.TypePath))
                    {
                        yield return td;
                        // Also yield supertraits transitively
                        foreach (var superTd in GetSupertraitsTransitive(td, seen))
                            yield return superTd;
                    }
                }
    }

    private IEnumerable<TraitDefinition> GetSupertraitsTransitive(TraitDefinition td, HashSet<LangPath> seen)
    {
        foreach (var supertrait in td.Supertraits)
        {
            var lookupPath = LangPath.StripGenerics(supertrait.TraitPath);
            if (GetDefinition(lookupPath) is TraitDefinition superTd && seen.Add(superTd.TypePath))
            {
                yield return superTd;
                foreach (var deeper in GetSupertraitsTransitive(superTd, seen))
                    yield return deeper;
            }
        }
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
    /// Checks if a path resolves to a trait method call (TraitName.Method)
    /// and returns the method's return type path
    /// </summary>
    /// <summary>
    /// Shared helper: resolves a call path to its TraitDefinition, method name, and parent path.
    /// Used by ResolveTraitMethodSignature, ResolveTraitMethodReturnType, etc.
    /// </summary>
    public (TraitDefinition traitDef, string methodName, LangPath parentPath)?
        ResolveTraitMethodLookup(NormalLangPath path)
    {
        if (path.PathSegments.Length < 2) return null;

        var workingPath = path;
        if (workingPath.GetFrontGenerics().Length > 0)
            workingPath = workingPath.PopGenerics()!;
        if (workingPath.PathSegments.Length < 2) return null;

        var lastSeg = workingPath.GetLastPathSegment();
        if (lastSeg == null) return null;
        var methodName = lastSeg.ToString();
        var parentPath = workingPath.Pop();
        if (parentPath == null || parentPath.PathSegments.Length == 0) return null;

        // Case 1: TraitName.Method
        var traitLookupPath = LangPath.StripGenerics(parentPath);
        var traitDef = GetDefinition(traitLookupPath) as TraitDefinition;

        // Case 2: T.Method — generic param with trait bounds
        if (traitDef == null && parentPath is NormalLangPath nlpP && nlpP.PathSegments.Length == 1)
            traitDef = GetTraitBoundsFor(nlpP.PathSegments[0].ToString())
                .FirstOrDefault(td => td.GetMethod(methodName) != null);

        // Case 3: ConcreteType.Method — search impls
        // Strip generics for the type lookup (Wrapper(i32) → Wrapper)
        // but keep the full parentPath for impl matching
        if (traitDef == null)
        {
            var typeLookupPath = LangPath.StripGenerics(parentPath);
            var typeDef = GetDefinition(typeLookupPath);
            if (typeDef != null && typeDef is not TraitDefinition)
            {
                foreach (var impl in ImplDefinitions)
                {
                    var bindings = impl.TryMatchConcreteType(parentPath);
                    if (bindings != null && impl.CheckBounds(bindings, this))
                    {
                        var implTraitLookup = LangPath.StripGenerics(impl.TraitPath);
                        var candidateDef = GetDefinition(implTraitLookup) as TraitDefinition;
                        if (candidateDef?.GetMethod(methodName) != null)
                        { traitDef = candidateDef; break; }
                    }
                }
            }
        }

        if (traitDef == null || traitDef.GetMethod(methodName) == null) return null;
        return (traitDef, methodName, parentPath);
    }

    /// <summary>
    /// Resolves the TraitMethodSignature for a given call path.
    /// </summary>
    public TraitMethodSignature? ResolveTraitMethodSignature(NormalLangPath path)
    {
        var lookup = ResolveTraitMethodLookup(path);
        return lookup?.traitDef.GetMethod(lookup.Value.methodName);
    }

    public LangPath? ResolveTraitMethodReturnType(NormalLangPath path)
    {
        var lookup = ResolveTraitMethodLookup(path);
        if (lookup == null) return null;

        var (traitDef, methodName, parentPath) = lookup.Value;
        var foundMethod = traitDef.GetMethod(methodName)!;
        var foundReturnType = foundMethod.ReturnTypePath;

        // If the return type is "Self", substitute it with the generic parameter
        // that has this trait as its bound
        if (foundReturnType is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            var retName = nlp.PathSegments[0].ToString();

            if (retName == "Self")
            {
                // For generic param case: T.Method where T: Trait → return T
                var traitTypePath = (traitDef as IDefinition).TypePath;
                foreach (var bounds in TraitBoundsStack)
                    foreach (var (tp, paramName, _) in bounds)
                        if (tp == traitTypePath)
                            return new NormalLangPath(null, [paramName]);

                // For concrete type case (i32.Default): parentPath is the concrete type
                // Check if parentPath is NOT a trait definition — if so, it's the concrete type
                var parentDef = GetDefinition(LangPath.StripGenerics(parentPath));
                if (parentDef != null && parentDef is not TraitDefinition)
                    return parentPath;

                // For qualified call ((i32 as Trait).method): return raw "Self"
                // FunctionCallKind.AnalyzeCall handles substitution via QualifiedAsType
                return foundReturnType;
            }

            // Check if it's an associated type of this trait
            var assocType = traitDef.GetAssociatedType(retName);
            if (assocType != null)
                return foundReturnType;
        }

        // Handle Self.AssocType return types (e.g., Self.Bruh)
        // Resolve to concrete associated type for concrete type calls
        if (foundReturnType is NormalLangPath nlpMulti && nlpMulti.PathSegments.Length == 2
            && nlpMulti.PathSegments[0] is NormalLangPath.NormalPathSegment firstSeg
            && nlpMulti.PathSegments[1] is NormalLangPath.NormalPathSegment secondSeg
            && firstSeg.Text == "Self")
        {
            var assocName = secondSeg.Text;
            var parentDef = GetDefinition(LangPath.StripGenerics(parentPath));
            if (parentDef != null && parentDef is not TraitDefinition)
            {
                var traitTypePath = (traitDef as IDefinition).TypePath;
                var resolved = ResolveAssociatedType(parentPath, traitTypePath, assocName);
                if (resolved != null) return resolved;
            }
        }

        // Handle (Self as Trait).AssocType return types
        if (foundReturnType is QualifiedAssocTypePath qpRet)
        {
            var resolved = ResolveQualifiedTypePath(foundReturnType);
            if (resolved != foundReturnType) return resolved;

            // If Self wasn't resolved, substitute with parentPath
            if (qpRet.ForType is NormalLangPath forNlp && forNlp.PathSegments.Length == 1
                && forNlp.PathSegments[0].ToString() == "Self")
            {
                var parentDef = GetDefinition(LangPath.StripGenerics(parentPath));
                if (parentDef != null && parentDef is not TraitDefinition)
                {
                    var traitTypePath = (traitDef as IDefinition).TypePath;
                    var resolvedAssoc = ResolveAssociatedType(parentPath, traitTypePath, qpRet.AssociatedTypeName);
                    if (resolvedAssoc != null) return resolvedAssoc;
                }
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
        // Check if typePath is a generic param or associated type with this trait as a bound
        if (typePath is NormalLangPath nlp)
        {
            // Build the lookup key: "T" for generic params, "Self.Output" for associated types
            string? paramKey = null;
            if (nlp.PathSegments.Length == 1)
                paramKey = nlp.PathSegments[0].ToString();
            else if (nlp.PathSegments.Length == 2)
            {
                // Check if first segment is a known generic param (e.g., Self, T)
                var firstSeg = nlp.PathSegments[0].ToString();
                foreach (var bounds in TraitBoundsStack)
                    if (bounds.Any(b => b.genericParamName == firstSeg))
                    {
                        paramKey = $"{firstSeg}.{nlp.PathSegments[1]}";
                        break;
                    }
            }

            if (paramKey != null)
            {
                // Direct bound check
                foreach (var bounds in TraitBoundsStack)
                    foreach (var (tp, pName, _) in bounds)
                        if (pName == paramKey && tp == traitPath)
                            return true;

                // Check via supertraits: if T: Foo<U> and Foo<X>: Bar<X>, then T satisfies Bar<U>
                foreach (var bounds in TraitBoundsStack)
                    foreach (var (tp, pName, _) in bounds)
                        if (pName == paramKey)
                        {
                            var (boundTraitBase, boundGenericArgs) = LangPath.SplitGenerics(tp);
                            var boundTraitDef = GetDefinition(boundTraitBase) as TraitDefinition;
                            if (boundTraitDef != null && HasSupertraitTransitive(
                                    boundTraitDef, traitPath, boundTraitDef.GenericParameters, boundGenericArgs))
                                return true;
                        }
            }
        }

        // Handle qualified associated type paths: (T as Trait).AssocType
        if (typePath is QualifiedAssocTypePath qap)
        {
            var traitBasePath = LangPath.StripGenerics(qap.TraitPath);
            var traitDef = GetDefinition(traitBasePath) as TraitDefinition;
            if (traitDef != null)
            {
                var assocType = traitDef.GetAssociatedType(qap.AssociatedTypeName);
                if (assocType != null)
                {
                    bool hasMetaSized = assocType.TraitBounds.Any(b =>
                        LangPath.StripGenerics(b).Equals(MetaSizedTraitPath));
                    // Check explicit bounds
                    foreach (var atBound in assocType.TraitBounds)
                    {
                        if (LangPath.StripGenerics(atBound).Equals(LangPath.StripGenerics(traitPath)))
                            return true;
                        // Check supertrait chain on the bound
                        var atBoundDef = GetDefinition(LangPath.StripGenerics(atBound)) as TraitDefinition;
                        if (atBoundDef != null && HasSupertraitTransitive(atBoundDef, traitPath))
                            return true;
                    }
                    // Implicit Sized (type → Sized → MetaSized)
                    if (!hasMetaSized)
                    {
                        if (LangPath.StripGenerics(traitPath).Equals(SizedTraitPath))
                            return true;
                        if (LangPath.StripGenerics(traitPath).Equals(MetaSizedTraitPath))
                            return true;
                        // Check if Sized has the target as supertrait
                        var sizedDef = GetDefinition(SizedTraitPath) as TraitDefinition;
                        if (sizedDef != null && HasSupertraitTransitive(sizedDef, traitPath))
                            return true;
                    }
                }
            }
        }

        // Handle tuple types: all tuples are Sized/MetaSized if all components are
        if (typePath is TupleLangPath tuplePath)
        {
            if (LangPath.StripGenerics(traitPath).Equals(SizedTraitPath)
                || LangPath.StripGenerics(traitPath).Equals(MetaSizedTraitPath))
            {
                return tuplePath.TypePaths.All(tp => TypeImplementsTrait(tp, traitPath));
            }
        }

        // Handle struct/enum types: Sized if all fields/variant payloads are Sized.
        // This handles generics, recursion, associated types — all cases.
        if (LangPath.StripGenerics(traitPath).Equals(SizedTraitPath)
            || LangPath.StripGenerics(traitPath).Equals(MetaSizedTraitPath))
        {
            var (typeBase, typeGenericArgs) = LangPath.SplitGenerics(typePath);
            var typeDef = GetDefinition(typeBase);
            if (typeDef is StructTypeDefinition std)
            {
                // Guard against infinite recursion (e.g., struct A { b: A })
                _sizedCheckVisited ??= [];
                if (!_sizedCheckVisited.Add(typePath)) return false;
                try
                {
                    var subs = BuildGenericSubstitutions(std.GenericParameters, typeGenericArgs);
                    return std.Fields.All(f =>
                        f.TypePath == null || TypeImplementsTrait(SubstituteType(f.TypePath, subs), traitPath));
                }
                finally { _sizedCheckVisited.Remove(typePath); }
            }
            if (typeDef is EnumTypeDefinition etd)
            {
                _sizedCheckVisited ??= [];
                if (!_sizedCheckVisited.Add(typePath)) return false;
                try
                {
                    var subs = BuildGenericSubstitutions(etd.GenericParameters, typeGenericArgs);
                    return etd.Variants.All(v =>
                        v.FieldTypes.All(ft =>
                            ft == null || TypeImplementsTrait(SubstituteType(ft, subs), traitPath)));
                }
                finally { _sizedCheckVisited.Remove(typePath); }
            }
        }

        // Strip generics from traitPath for base comparison
        var (traitBase, traitGenericArgs) = LangPath.SplitGenerics(traitPath);

        // Check concrete and generic impls
        return ImplDefinitions.Any(i =>
        {
            // Compare trait base paths
            var (implTraitBase, implTraitGenericArgs) = LangPath.SplitGenerics(i.TraitPath);

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
    /// Substitutes the trait's generic params with the provided args in supertrait paths.
    /// </summary>
    public bool HasSupertraitTransitive(TraitDefinition traitDef, LangPath targetTrait,
        ImmutableArray<GenericParameter> genericParams = default, ImmutableArray<LangPath> genericArgs = default)
    {
        foreach (var supertrait in traitDef.Supertraits)
        {
            // Substitute generic params in the supertrait path
            var resolvedSupertrait = supertrait.TraitPath;
            if (!genericParams.IsDefault && !genericArgs.IsDefault
                && genericParams.Length > 0 && genericArgs.Length > 0)
                resolvedSupertrait = FieldAccessExpression.SubstituteGenerics(
                    supertrait.TraitPath, genericParams, genericArgs);

            if (resolvedSupertrait == targetTrait) return true;

            // Recurse: check supertraits of supertraits
            var (superBase, superGenericArgs) = LangPath.SplitGenerics(resolvedSupertrait);
            var superDef = GetDefinition(superBase) as TraitDefinition;
            if (superDef != null && HasSupertraitTransitive(
                    superDef, targetTrait, superDef.GenericParameters, superGenericArgs))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Resolves an associated type for a given implementing type and trait.
    /// E.g., for (i32 as Add).Output, finds the impl of Add for i32 and returns Output's concrete type.
    /// Also handles stripping trait generics for matching.
    /// First checks trait bound associated type constraints (e.g., T: Add&lt;T, Output = T&gt;).
    /// </summary>
    public LangPath? ResolveAssociatedType(LangPath forType, LangPath traitPath, string associatedTypeName)
    {
        // Check trait bound associated type constraints first
        // e.g., for T: Add(T, Output = T), resolving (T as Add(T)).Output gives T
        if (forType is NormalLangPath nlpFor && nlpFor.PathSegments.Length == 1)
        {
            var paramName = nlpFor.PathSegments[0].ToString();
            foreach (var bounds in TraitBoundsStack)
                foreach (var (tp, pName, assocConstraints) in bounds)
                {
                    if (pName != paramName || assocConstraints == null) continue;

                    // Direct match: bound trait == requested trait
                    if (tp == traitPath)
                        if (assocConstraints.TryGetValue(associatedTypeName, out var constrainedType))
                            return constrainedType;

                    // Supertrait match: bound is Deref(Target=Foo), looking for Receiver.Target
                    // If bound's trait extends the requested trait, the constraint still applies
                    var (boundTraitBase, _) = LangPath.SplitGenerics(tp);
                    var boundTraitDef = GetDefinition(boundTraitBase) as TraitDefinition;
                    if (boundTraitDef != null && HasSupertraitTransitive(
                            boundTraitDef, traitPath, boundTraitDef.GenericParameters, LangPath.SplitGenerics(tp).genericArgs))
                        if (assocConstraints.TryGetValue(associatedTypeName, out var constrainedType2))
                            return constrainedType2;
                }
        }

        // Strip trait generics for lookup
        var traitLookupPath = LangPath.StripGenerics(traitPath);

        foreach (var impl in ImplDefinitions)
        {
            // Match trait path (strip generics on impl trait path too)
            var implTraitLookup = LangPath.StripGenerics(impl.TraitPath);

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
    /// Finds the impl of a supertrait for the given type and returns all associated type
    /// assignments, with generic parameters substituted. Used when validating a subtrait impl
    /// to inherit associated types from supertrait impls.
    /// e.g., impl Receiver for Box&lt;T&gt; { type Target = T; } → returns [("Target", T)]
    /// when called for Box&lt;T&gt; + Receiver from the Deref impl validation.
    /// </summary>
    public List<(string Name, LangPath Value)> ResolveAssociatedTypesFromImpl(
        LangPath forType, LangPath traitPath,
        ImmutableArray<GenericParameter> implGenericParams,
        Dictionary<string, LangPath> existingSubstitutions)
    {
        var results = new List<(string, LangPath)>();
        var traitBase = LangPath.StripGenerics(traitPath);

        foreach (var impl in ImplDefinitions)
        {
            var implTraitBase = LangPath.StripGenerics(impl.TraitPath);
            if (implTraitBase != traitBase) continue;

            var bindings = impl.TryMatchConcreteType(forType);
            if (bindings == null) continue;

            foreach (var at in impl.AssociatedTypeAssignments)
            {
                // Substitute impl generic params in the associated type value
                var value = at.ConcreteType;
                if (bindings.Count > 0 && impl.GenericParameters.Length > 0)
                {
                    var genArgs = TypeInference.BuildGenericArgs(impl.GenericParameters, bindings);
                    if (genArgs != null)
                        value = FieldAccessExpression.SubstituteGenerics(
                            value, impl.GenericParameters, genArgs.Value);
                }
                results.Add((at.Name, value));
            }
            break;
        }

        return results;
    }
    /// Handles:
    /// - QualifiedAssocTypePath: (i32 as Add(i32)).Output → i32
    /// - T.Output where T is a generic param with trait bound → resolved via constraints
    /// - ConcreteType.Output where a unique impl provides Output → resolved via impl search
    /// Returns the resolved path, or the original path if no resolution applies.
    /// </summary>
    public LangPath ResolveQualifiedTypePath(LangPath path)
    {
        // Case 1: Explicit qualified path (Type as Trait).AssocType
        if (path is QualifiedAssocTypePath qp)
        {
            var resolvedFor = ResolveQualifiedTypePath(qp.ForType);
            var resolvedTrait = ResolveQualifiedTypePath(qp.TraitPath);
            var result = ResolveAssociatedType(resolvedFor, resolvedTrait, qp.AssociatedTypeName);
            return result ?? path;
        }

        // Case 2 & 3: NormalLangPath with T.AssocType or ConcreteType.AssocType
        if (path is NormalLangPath nlp && nlp.PathSegments.Length >= 2)
        {
            var firstName = nlp.PathSegments[0].ToString();
            var lastName = nlp.GetLastPathSegment()?.ToString();
            if (lastName == null) return path;

            // Case 2: first segment is a generic param (T.Output)
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
                        var traitBasePath = LangPath.StripGenerics(tp);
                        var traitDef = GetDefinition(traitBasePath) as TraitDefinition;
                        if (traitDef?.GetAssociatedType(lastName) != null)
                            return new QualifiedAssocTypePath(paramPath, tp, lastName);
                    }
                }
            }

            // Case 3: ConcreteType.AssocType — search impls for a unique match
            var parentPath = nlp.Pop();
            if (parentPath != null && parentPath.PathSegments.Length > 0
                && nlp.GetLastPathSegment() is NormalLangPath.NormalPathSegment lastSeg)
            {
                var assocName = lastSeg.ToString();
                // Skip if parent is a trait definition — use (Self as Trait).AssocType instead
                var typeDef = GetDefinition(LangPath.StripGenerics(parentPath));
                if (typeDef is not TraitDefinition)
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
                            $"Use qualified syntax: ({parentPath} as Trait).{assocName}\n" +
                            $"{path.FirstIdentifierToken?.GetLocationStringRepresentation() ?? ""}"));
                        return matches[0];
                    }
                }
            }
        }

        // Recurse into generic type arguments to resolve nested associated types
        // e.g., &((&T).Target) → the inner (&T).Target is inside a generic arg of the ref type
        if (path is NormalLangPath nlpGeneric)
        {
            bool changed = false;
            var newSegs = new List<NormalLangPath.PathSegment>();
            foreach (var seg in nlpGeneric.PathSegments)
            {
                if (seg is NormalLangPath.NormalPathSegment { HasGenericArgs: true } nps)
                {
                    var newTypes = nps.GenericArgs!.Value.Select(tp =>
                    {
                        var resolved = ResolveQualifiedTypePath(tp);
                        if (resolved != tp) changed = true;
                        return resolved;
                    }).ToImmutableArray();
                    newSegs.Add(nps.WithGenericArgs(newTypes));
                }
                else
                {
                    newSegs.Add(seg);
                }
            }
            if (changed)
                return new NormalLangPath(nlpGeneric.FirstIdentifierToken, newSegs);
        }

        return path;
    }

    /// <summary>
    /// Checks whether a type implements the Copy trait.
    /// Copy types are bitwise-copied on assignment; non-Copy types are moved.
    /// </summary>
    public bool IsTypeCopy(LangPath? typePath)
        => typePath == null || TypeImplementsTrait(typePath, CopyTraitPath);

    /// <summary>
    /// Checks whether a type implements the Drop trait.
    /// Drop types have their drop() method called when they go out of scope.
    /// </summary>
    public bool IsTypeDrop(LangPath? typePath)
        => typePath != null && TypeImplementsTrait(typePath, DropTraitPath);

    /// <summary>
    /// The canonical path of the Copy marker trait: Std.Marker.Copy
    /// </summary>
    public static readonly NormalLangPath CopyTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Marker", "Copy" });

    /// <summary>
    /// The canonical path of the Drop trait: Std.Ops.Drop
    /// </summary>
    public static readonly NormalLangPath DropTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Ops", "Drop" });

    /// <summary>
    /// The canonical path of the MutReassign trait: Std.Marker.MutReassign.
    /// Types implementing this can be reassigned through &amp;mut references.
    /// </summary>
    public static readonly NormalLangPath MutReassignTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Marker", "MutReassign" });

    public static readonly NormalLangPath AddTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Ops", "Add" });
    public static readonly NormalLangPath SubTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Ops", "Sub" });
    public static readonly NormalLangPath MulTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Ops", "Mul" });
    public static readonly NormalLangPath DivTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Ops", "Div" });

    public static readonly NormalLangPath PartialEqTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Ops", "PartialEq" });
    public static readonly NormalLangPath EqTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Ops", "Eq" });
    public static readonly NormalLangPath PartialOrdTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Ops", "PartialOrd" });
    public static readonly NormalLangPath OrdTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Ops", "Ord" });

    public static readonly NormalLangPath ReceiverTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Deref", "Receiver" });

    public static readonly NormalLangPath DerefTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Deref", "Deref" });
    public static readonly NormalLangPath DerefConstTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Deref", "DerefConst" });
    public static readonly NormalLangPath DerefMutTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Deref", "DerefMut" });
    public static readonly NormalLangPath DerefUniqTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Deref", "DerefUniq" });

    /// <summary>
    /// Returns the deref trait required for a given reference kind.
    /// &amp; → Deref, &amp;const → DerefConst, &amp;mut → DerefMut, &amp;uniq → DerefUniq
    /// </summary>
    public static NormalLangPath GetDerefTraitForRefKind(RefKind kind) => kind switch
    {
        RefKind.Shared => DerefTraitPath,
        RefKind.Const => DerefConstTraitPath,
        RefKind.Mut => DerefMutTraitPath,
        RefKind.Uniq => DerefUniqTraitPath,
        _ => DerefTraitPath
    };

    /// <summary>
    /// Returns the deref method name for a given reference kind.
    /// </summary>
    public static string GetDerefMethodForRefKind(RefKind kind) => kind switch
    {
        RefKind.Shared => "deref",
        RefKind.Const => "deref_const",
        RefKind.Mut => "deref_mut",
        RefKind.Uniq => "deref_uniq",
        _ => "deref"
    };

    /// <summary>
    /// Checks if a raw pointer kind can produce a reference of the given kind.
    /// Follows the deref trait hierarchy: *uniq → all, *const → &amp;/&amp;const, etc.
    /// </summary>
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
        if (expr.TypePath != null && !IsTypeCopy(expr.TypePath))
        {
            var varName = IExpression.TryGetSimpleVariableName(expr);
            if (varName != null)
                MarkAsMoved(varName);
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

    /// <summary>
    /// Searches all registered definitions for one whose last path segment matches the given name.
    /// Used as a fallback when ChainExpression roots aren't resolved during path resolution.
    /// </summary>
    public IDefinition? FindDefinitionByName(string name)
    {
        foreach (var scope in DefinitionsStackMap)
            foreach (var (path, def) in scope)
                if (path is NormalLangPath nlp && nlp.GetLastPathSegment()?.ToString() == name)
                    return def;
        return null;
    }

    /// <summary>
    /// Checks if the given path is a valid module prefix (i.e., some registered definition
    /// has a path that starts with this prefix). Used to validate module-level imports like "use Std.Ops;".
    /// </summary>
    public bool IsModulePath(NormalLangPath modulePath)
    {
        var prefix = modulePath.ToString() + ".";
        foreach (var scope in DefinitionsStackMap)
            foreach (var (path, _) in scope)
                if (path.ToString().StartsWith(prefix))
                    return true;
        return false;
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
        _referencesToLocalsStack.Push(new HashSet<string>());
        _implDefinitionsStack.Push(new List<ImplDefinition>());
        _borrowScopes.Push(new BorrowScope());
        _traitsInScope.Push(new List<LangPath>());
        CurrentScopeDepth++;
    }

    


    public void PopScope()
    {   
        DefinitionsStackMap.Pop();
     
        VariableToTypeMapper.Pop();
        MovedVariablesStack.Pop();
        _referencesToLocalsStack.Pop();
        _implDefinitionsStack.Pop();
        _traitsInScope.Pop();
        CurrentScopeDepth--;

        // Invalidate borrows from variables going out of scope
        if (_scopeVariables.Count > 0)
        {
            var exiting = _scopeVariables.Pop();
            foreach (var varName in exiting)
            {
                InvalidateBorrowsFrom(varName);
                ReleaseBorrowsBy(varName);
            }
        }

        // Pop the borrow scope — any borrows registered in this scope are gone
        if (_borrowScopes.Count > 0)
            _borrowScopes.Pop();
    }

    public static readonly NormalLangPath MetaSizedTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Marker", "MetaSized" });

    public static readonly NormalLangPath SizedTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "Std", "Marker", "Sized" });

    /// <summary>
    /// Recursion guard for struct/enum Sized checks in TypeImplementsTrait.
    /// Prevents infinite loops from recursive type definitions.
    /// </summary>
    private HashSet<LangPath>? _sizedCheckVisited;

    /// <summary>
    /// Builds a substitution map from generic parameter names to concrete type arguments.
    /// </summary>
    private static Dictionary<string, LangPath> BuildGenericSubstitutions(
        ImmutableArray<GenericParameter> genericParams, ImmutableArray<LangPath> genericArgs)
    {
        var subs = new Dictionary<string, LangPath>();
        for (int i = 0; i < genericParams.Length && i < genericArgs.Length; i++)
            subs[genericParams[i].Name] = genericArgs[i];
        return subs;
    }

    /// <summary>
    /// Substitutes generic parameters in a type path using the given substitution map.
    /// E.g., with {T → i32}, substitutes T → i32 in field types.
    /// </summary>
    private static LangPath SubstituteType(LangPath typePath, Dictionary<string, LangPath> subs)
    {
        if (subs.Count == 0) return typePath;
        if (typePath is NormalLangPath nlp)
        {
            // Direct substitution: T → i32
            if (nlp.PathSegments.Length == 1 && subs.TryGetValue(nlp.PathSegments[0].ToString(), out var sub))
                return sub;
            // Shorthand associated type: T.Output → (T_substituted).Output
            // Stays as a 2-segment path for TypeImplementsTrait to resolve via bounds
            if (nlp.PathSegments.Length == 2 && subs.TryGetValue(nlp.PathSegments[0].ToString(), out _))
            {
                // Keep as-is — TypeImplementsTrait handles "T.Output" via bounds stack
                return typePath;
            }
            // Substitute in generics: Wrapper(T) → Wrapper(i32)
            if (nlp.GetFrontGenerics().Length > 0)
            {
                var newGenerics = nlp.GetFrontGenerics().Select(g => SubstituteType(g, subs)).ToImmutableArray();
                return nlp.PopGenerics()!.AppendGenerics(newGenerics);
            }
        }
        if (typePath is TupleLangPath tlp)
            return new TupleLangPath(tlp.TypePaths.Select(t => SubstituteType(t, subs)).ToImmutableArray());
        if (typePath is QualifiedAssocTypePath qap)
            return new QualifiedAssocTypePath(
                SubstituteType(qap.ForType, subs),
                SubstituteType(qap.TraitPath, subs),
                qap.AssociatedTypeName);
        return typePath;
    }

    /// <summary>
    /// Builds the trait bounds list for a set of generic parameters, adding implicit Sized
    /// where needed. If a generic param has no explicit MetaSized bound, Sized is added
    /// implicitly (T:! type → T: Sized). If MetaSized is explicit, Sized is NOT added
    /// (the user opted out, allowing unsized types).
    /// Shared by FunctionDefinition and ImplDefinition — no duplication.
    /// </summary>
    /// <summary>
    /// Validates that all parameters have Sized types. Unsized types cannot be passed by value.
    /// Shared by FunctionDefinition and TraitDefinition — no duplication.
    /// </summary>
    public void ValidateParamsSized(IEnumerable<VariableDefinition> parameters, string locationString,
        LangPath? returnType = null)
    {
        foreach (var param in parameters)
        {
            if (param.TypePath != null
                && !TypeImplementsTrait(param.TypePath, SizedTraitPath))
            {
                AddException(new SemanticException(
                    $"Parameter '{param.Name}' has unsized type '{param.TypePath}' — " +
                    $"cannot pass unsized types by value\n" + locationString));
            }
        }

        if (returnType != null && !returnType.Equals(LangPath.VoidBaseLangPath)
            && !TypeImplementsTrait(returnType, SizedTraitPath))
        {
            AddException(new SemanticException(
                $"Return type '{returnType}' is unsized — " +
                $"cannot return unsized types by value\n" + locationString));
        }
    }

    /// <summary>
    /// Validates lifetime annotations on a function or trait method signature.
    /// Shared by FunctionDefinition and TraitDefinition.
    /// </summary>
    public void ValidateLifetimeAnnotations(
        ImmutableArray<VariableDefinition> parameters,
        Dictionary<int, string> argumentLifetimes,
        string? returnLifetime, LangPath? returnTypePath,
        ImmutableArray<string> lifetimeParameters,
        string name, string locationString, string kind)
    {
        // Check all argument lifetimes are declared
        foreach (var (_, lt) in argumentLifetimes)
            if (!lifetimeParameters.Contains(lt))
                AddException(new SemanticException(
                    $"Undeclared lifetime '{lt}' in parameter of {kind} '{name}'\n" + locationString));

        // Check return lifetime is declared ('static is a built-in, not user-declared)
        if (returnLifetime != null && returnLifetime != "static"
            && !lifetimeParameters.Contains(returnLifetime))
            AddException(new SemanticException(
                $"Undeclared lifetime '{returnLifetime}' in return type of {kind} '{name}'\n" + locationString));

        // Elision / explicit lifetime checks on reference-returning signatures
        if (returnTypePath is NormalLangPath nlpRet
            && nlpRet.Contains(RefTypeDefinition.GetRefModule()))
        {
            if (returnLifetime != null && returnLifetime != "static")
            {
                // Explicit: return lifetime must appear on at least one param
                // Either as a direct ref lifetime (&'a T) or as a type lifetime arg (Foo['a])
                bool returnLifetimeOnParam = argumentLifetimes.Values.Any(lt => lt == returnLifetime)
                    || parameters.Any(p => p.TypePath is NormalLangPath nlpP
                        && nlpP.LifetimeArgs.Contains(returnLifetime));
                if (!returnLifetimeOnParam)
                    AddException(new SemanticException(
                        $"Return lifetime '{returnLifetime}' does not appear on any parameter in {kind} '{name}'\n" +
                        locationString));
            }
            else if (returnLifetime == null)
            {
                // Implicit: check elision ambiguity
                var refParamCount = parameters.Count(p =>
                    p.TypePath is NormalLangPath nlpP && nlpP.Contains(RefTypeDefinition.GetRefModule()));
                var hasSelfRefParam = parameters.Any(p =>
                    p.Name == "self"
                    && p.TypePath is NormalLangPath nlpS
                    && nlpS.Contains(RefTypeDefinition.GetRefModule()));
                if (refParamCount > 1 && !hasSelfRefParam)
                    AddException(new SemanticException(
                        $"{char.ToUpper(kind[0]) + kind[1..]} '{name}' returns a reference but has {refParamCount} reference parameters. " +
                        $"Cannot determine which input the output borrows from — explicit lifetime annotations are required\n" +
                        locationString));
            }
        }
    }

    public static List<(LangPath traitPath, string paramName, Dictionary<string, LangPath>? assocConstraints)>
        BuildGenericBoundsWithImplicitSized(ImmutableArray<GenericParameter> genericParams)
    {
        var result = new List<(LangPath, string, Dictionary<string, LangPath>?)>();
        foreach (var gp in genericParams)
        {
            foreach (var tb in gp.TraitBounds)
            {
                var assocConstraints = tb.AssociatedTypeConstraints.Count > 0
                    ? tb.AssociatedTypeConstraints : null;
                result.Add((tb.TraitPath, gp.Name, assocConstraints));
            }

            // Implicit Sized unless MetaSized is explicitly specified (opt-out)
            bool hasMetaSized = gp.TraitBounds.Any(tb =>
                LangPath.StripGenerics(tb.TraitPath).Equals(MetaSizedTraitPath));
            if (!hasMetaSized)
                result.Add(((LangPath)SizedTraitPath, gp.Name, null));
        }
        return result;
    }

    /// <summary>
    /// Generates synthetic MetaSized and Sized impls for all registered type definitions.
    /// Called after ResolvePaths so all paths are fully resolved.
    /// Sized types get Metadata = (), unsized types get Metadata = usize.
    /// </summary>
    private void GenerateMetaSizedAndSizedImpls()
    {
        var usizePath = LangPath.PrimitivePath.Append("usize");

        // Collect ALL type definitions recursively — including those inside function bodies
        var allTypeDefs = new List<TypeDefinition>();
        foreach (var item in ParseResults.SelectMany(r => r.Items))
            CollectTypeDefinitions(item, allTypeDefs);

        foreach (var def in allTypeDefs)
        {
            // Skip traits — they don't get MetaSized/Sized impls
            if (def is TraitDefinition) continue;
            // Skip pointer types — handled separately with generic T:! MetaSized param
            if (def is PointerTypeDefinitionBase) continue;
            // Skip tuple types — void tuple handled explicitly below
            if (def is TupleTypeDefinition) continue;

            var typePath = def.TypePath;
            var isUnsized = def.IsUnsized;
            var metadataType = isUnsized ? (LangPath)usizePath : LangPath.VoidBaseLangPath;

            // Determine generic params from the type definition
            var genericParams = def switch
            {
                StructTypeDefinition std => std.GenericParameters,
                EnumTypeDefinition etd => etd.GenericParameters,
                _ => ImmutableArray<GenericParameter>.Empty
            };

            // For generic types, the ForTypePath needs generics applied: Wrapper(T)
            var forTypePath = typePath;
            if (genericParams.Length > 0 && forTypePath is NormalLangPath nlp)
                forTypePath = nlp.AppendGenerics(
                    genericParams.Select(gp => (LangPath)new NormalLangPath(null, [gp.Name]))
                        .ToImmutableArray());

            // impl MetaSized for T { let Metadata :! Copy = <metadata_type>; }
            var metaSizedImpl = new ImplDefinition(
                MetaSizedTraitPath, forTypePath,
                methods: [],
                token: null!,
                genericParameters: genericParams,
                associatedTypes: [new ImplAssociatedType
                {
                    Name = "Metadata", ConcreteType = metadataType, Token = null!
                }]);
            AddImplDefinition(metaSizedImpl);

            // impl Sized for T {} — for structs/enums, Sized is determined by field types
            // at check time in TypeImplementsTrait (handles generics, recursion, assoc types).
            // For primitives and other non-composite types, generate directly.
            if (!isUnsized && def is not StructTypeDefinition && def is not EnumTypeDefinition)
            {
                var sizedImpl = new ImplDefinition(
                    SizedTraitPath, forTypePath,
                    methods: [],
                    token: null!,
                    genericParameters: genericParams,
                    associatedTypes: []);
                AddImplDefinition(sizedImpl);
            }
        }

        // Generate MetaSized/Sized for pointer-like types (refs and raw ptrs)
        // All pointers are Sized with Metadata = () — T needs no bounds
        foreach (var def in ParseResults.SelectMany(r => r.Items.OfType<PointerTypeDefinitionBase>()))
        {
            var unboundedGP = new GenericParameter("T");

            var innerTypePath = new NormalLangPath(null, ["T"]);
            var forTypePath = ((NormalLangPath)def.TypePath).AppendGenerics([innerTypePath]);

            var metaSizedImpl = new ImplDefinition(
                MetaSizedTraitPath, forTypePath,
                methods: [],
                token: null!,
                genericParameters: [unboundedGP],
                associatedTypes: [new ImplAssociatedType
                {
                    Name = "Metadata", ConcreteType = LangPath.VoidBaseLangPath, Token = null!
                }]);
            AddImplDefinition(metaSizedImpl);

            var sizedImpl = new ImplDefinition(
                SizedTraitPath, forTypePath,
                methods: [],
                token: null!,
                genericParameters: [unboundedGP],
                associatedTypes: []);
            AddImplDefinition(sizedImpl);
        }

        // Generate MetaSized/Sized for tuple types — void tuple () is always available
        var voidMetaSized = new ImplDefinition(
            MetaSizedTraitPath, LangPath.VoidBaseLangPath,
            methods: [],
            token: null!,
            genericParameters: [],
            associatedTypes: [new ImplAssociatedType
            {
                Name = "Metadata", ConcreteType = LangPath.VoidBaseLangPath, Token = null!
            }]);
        AddImplDefinition(voidMetaSized);

        var voidSized = new ImplDefinition(
            SizedTraitPath, LangPath.VoidBaseLangPath,
            methods: [],
            token: null!,
            genericParameters: [],
            associatedTypes: []);
        AddImplDefinition(voidSized);
    }

    /// <summary>
    /// Recursively collects all TypeDefinitions from a syntax tree node,
    /// including those nested inside function bodies and block expressions.
    /// </summary>
    private static void CollectTypeDefinitions(ISyntaxNode node, List<TypeDefinition> results)
    {
        if (node is TypeDefinition td)
            results.Add(td);
        foreach (var child in node.Children)
            CollectTypeDefinitions(child, results);
    }

    private void ResolvePaths()
    {
        var pathShortcutContext = new PathResolver();
        
        pathShortcutContext.AddScope();
        // Register 'crate' as a shortcut to the package root module path
        if (CrateRoot != null)
            pathShortcutContext.AddToDeepestScope("crate", CrateRoot);
        var primitiveParsed = ParseResults.First(i => i.Items.Any(j => j is I32TypeDefinition));
        foreach (var i in primitiveParsed.Items.OfType<TypeDefinition>()
                     .Where(t => t is PrimitiveTypeDefinition or StrTypeDefinition))
        {
            var usings = new UseDefinition((NormalLangPath)i.TypePath, null);
            usings.RegisterUsings(pathShortcutContext);
        }
        // Auto-import specific items from std by exact path.
        // Everything else requires explicit path or 'use'.
        var autoImportPaths = new[]
        {
            new NormalLangPath(null, new NormalLangPath.PathSegment[] { "Std", "Alloc", "Box" }),
            new NormalLangPath(null, new NormalLangPath.PathSegment[] { "Std", "Marker", "Copy" }),
            new NormalLangPath(null, new NormalLangPath.PathSegment[] { "Std", "Marker", "MutReassign" }),
            new NormalLangPath(null, new NormalLangPath.PathSegment[] { "Std", "Marker", "MetaSized" }),
            new NormalLangPath(null, new NormalLangPath.PathSegment[] { "Std", "Marker", "Sized" }),
            new NormalLangPath(null, new NormalLangPath.PathSegment[] { "Std", "Core", "Option" }),
        };
        foreach (var path in autoImportPaths)
        {
            var usings = new UseDefinition(path, null);
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
            AddImplDefinition(impl);
            // Register each impl method as a definition so it can be found during codegen
            foreach (var method in impl.Methods)
                RegisterDefinitionAtDeepestScope(method.TypePath, method);
        }

        ResolvePaths();
        GenerateMetaSizedAndSizedImpls();

        foreach (var result in ParseResults)
        {
            AddScope();

            // Register traits defined in this file as in-scope
            foreach (var trait in result.Items.OfType<TraitDefinition>())
                ImportTrait(trait.TypePath);

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