using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang;

public static class Extensions
{
    public static unsafe sbyte* ToCString(this string str)
    {
        return (sbyte*)Marshal.StringToHGlobalAnsi(str).ToPointer();
    }
}

/// <summary>
///     Base type that represents either the type, or variable reference of a path
///     like how in c#, if u have a type Foo in namespace bar it will be in
///     Bar.Foo. that wouldb e the type. if Foo has a static variable Dog, it will be in
///     Bar.Foo.Dog. that would be a variable ref
/// </summary>
public interface IRefItem
{

}

public interface IHasType
{
    Type Type { get; }
}

public class FunctionRefItem : IRefItem
{
 
    public required Function Function { get; init; }

}

public class TypeRefItem : IRefItem, IHasType
{
    public LLVMTypeRef TypeRef => Type.TypeRef;

    public required Type Type { get; init; }
}
/// <summary>
/// Represents a value. eg: a variable
/// </summary>
public class ValueRefItem : IRefItem, IHasType
{
    public required LLVMValueRef ValueRef { get; init; }
    public required Type Type { get; init; }

    /// <summary>
    /// </summary>
    /// <param name="context"></param>
    /// <returns>Pointer to stack allocation</returns>
    public LLVMValueRef StackAllocate(CodeGenContext context)
    {
        return Type.AssignToStack(context, this);
    }

    /// <summary>
    /// Loads a value 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public LLVMValueRef LoadValue(CodeGenContext context)
    {
        return Type.LoadValue(context, this);
    }
}

public class CodeGenContext
{

    private readonly Stack<Dictionary<LangPath, IRefItem>> ScopeItems = new();

    private ValueRefItem Void;

    private List<IDefinition> TopLevelDefinitions;

    /// <summary>
    /// Stores all impl definitions for trait method resolution, scoped so that
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

    /// <summary>
    /// Caches LLVM named struct types by their LangPath to prevent duplicate creation.
    /// LLVM auto-suffixes duplicate names (Foo, Foo.0, Foo.1...) which causes type mismatches
    /// when the same logical type is looked up through different code paths (e.g. Self → Foo).
    /// Keyed by LangPath so different modules with same type name stay distinct.
    /// </summary>
    private readonly Dictionary<LangPath, LLVMTypeRef> _namedStructCache = new();

    /// <summary>
    /// Persistent cache of resolved TypeRefItems by monomorphized LangPath.
    /// Unlike scope-based caching, this survives scope push/pop cycles.
    /// Prevents CreateRefDefinition from being called twice for the same type,
    /// which would create duplicate C# Type wrappers (and potentially duplicate LLVM types
    /// if the LangPath key differs slightly between calls).
    /// </summary>
    private readonly Dictionary<LangPath, IRefItem> _typeRefCache = new();
    private readonly Stack<Dictionary<LangPath, IRefItem>> _functionRefCache = new();

    private IRefItem? GetCachedFunction(LangPath key)
    {
        foreach (var scope in _functionRefCache)
            if (scope.TryGetValue(key, out var val))
                return val;
        return null;
    }

    private void CacheFunction(LangPath key, IRefItem value)
    {
        _functionRefCache.Peek()[key] = value;
    }

    /// <summary>
    /// Gets or creates an LLVM named struct type. If a struct with this path was already created,
    /// returns the cached version. Otherwise creates a new one and caches it.
    /// Returns (typeRef, isNew) so the caller knows whether to set the body.
    /// </summary>
    public (LLVMTypeRef typeRef, bool isNew) GetOrCreateNamedStruct(LangPath path)
    {
        if (_namedStructCache.TryGetValue(path, out var existing))
            return (existing, false);

        var structType = LLVMContext.CreateNamedStruct(path.ToString());
        _namedStructCache[path] = structType;
        return (structType, true);
    }

    /// <summary>
    /// Tracks droppable variables per scope.
    /// Each entry: (variable name, drop flag alloca, type path, value pointer alloca)
    /// Uses a List (not Dictionary) so shadowed variables each get their own entry.
    /// The drop flag is an i1 alloca: true = needs drop, false = was moved.
    /// </summary>
    private readonly Stack<List<(string name, LLVMValueRef dropFlag, LangPath typePath, LLVMValueRef valuePtr)>> ScopeDropFlags = new();

    /// <summary>
    /// Checks whether a type implements the Drop trait at codegen time.
    /// </summary>
    public bool IsTypeDrop(LangPath typePath)
    {
        var dropTraitPath = SemanticAnalyzer.DropTraitPath;

        // Check if this is a generic param with a Drop trait bound
        if (typePath is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            foreach (var bounds in TraitBoundsStack)
                foreach (var (tp, _) in bounds)
                    if (tp == dropTraitPath) return true;
        }

        // Check concrete impls
        return ImplDefinitions.Any(i =>
        {
            var implTraitBase = LangPath.StripGenerics(i.TraitPath);
            if (implTraitBase != dropTraitPath) return false;
            var bindings = i.TryMatchConcreteType(typePath);
            return bindings != null;
        });
    }

    /// <summary>
    /// Register a variable as droppable in the current scope.
    /// Creates a drop flag (i1 alloca initialized to true).
    /// </summary>
    public void RegisterDroppable(string name, LangPath typePath, LLVMValueRef valuePtr)
    {
        if (ScopeDropFlags.Count == 0) return;
        var uniqueName = $"{name}_{ScopeDropFlags.Peek().Count}";
        var dropFlag = Builder.BuildAlloca(LLVMTypeRef.Int1, $"drop_flag_{uniqueName}");
        Builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1, false), dropFlag);
        ScopeDropFlags.Peek().Add((name, dropFlag, typePath, valuePtr));
    }

    /// <summary>
    /// Mark a variable as moved (clear its drop flag so drop won't be called).
    /// Marks the LATEST (most recent) entry with the given name.
    /// </summary>
    public void MarkDropFlagMoved(string name)
    {
        foreach (var scope in ScopeDropFlags)
        {
            for (int i = scope.Count - 1; i >= 0; i--)
            {
                if (scope[i].name == name)
                {
                    Builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 0, false), scope[i].dropFlag);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// If the expression is a simple variable path, mark its drop flag as moved.
    /// Called after a value is consumed (assignment, fn arg, etc.)
    /// No-op if the variable has no drop flag (e.g., Copy types aren't registered as droppable).
    /// </summary>
    public void TryMarkExpressionDropMoved(IExpression expr)
    {
        if (expr is ChainExpression { SimpleVariableName: string varName })
        {
            MarkDropFlagMoved(varName);
        }
        else if (expr is PathExpression pe
            && pe.Path is NormalLangPath nlp
            && nlp.PathSegments.Length == 1)
        {
            MarkDropFlagMoved(nlp.PathSegments[0].ToString());
        }
    }

    /// <summary>
    /// Emit drop calls for all droppable variables in the current scope.
    /// Called before popping a scope. Checks the drop flag before calling drop.
    /// Variables are dropped in reverse declaration order (LIFO).
    /// Drop order per variable: 1) call Drop.Drop if implemented, 2) drop each field recursively.
    /// </summary>
    public void EmitDropCalls()
    {
        if (ScopeDropFlags.Count == 0) return;
        var scope = ScopeDropFlags.Peek();
        if (scope.Count == 0) return;

        // Drop in reverse order of declaration (LIFO)
        for (int i = scope.Count - 1; i >= 0; i--)
        {
            var (name, dropFlag, typePath, valuePtr) = scope[i];
            var uniqueName = $"{name}_{i}";

            // Load the drop flag
            var flagVal = Builder.BuildLoad2(LLVMTypeRef.Int1, dropFlag, $"should_drop_{uniqueName}");

            // Get the current function for branching
            var currentBlock = Builder.InsertBlock;
            var function = currentBlock.Parent;

            var dropBlock = function.AppendBasicBlock($"drop_{uniqueName}");
            var afterBlock = function.AppendBasicBlock($"after_drop_{uniqueName}");

            Builder.BuildCondBr(flagVal, dropBlock, afterBlock);

            // Drop block: first call Drop.Drop if the type implements Drop,
            // then recursively drop fields that need dropping
            Builder.PositionAtEnd(dropBlock);

            if (IsTypeDrop(typePath))
                EmitSingleDropCall(typePath, valuePtr);

            EmitFieldDrops(typePath, valuePtr);

            Builder.BuildBr(afterBlock);

            // Continue after drop
            Builder.PositionAtEnd(afterBlock);
        }
    }

    /// <summary>
    /// Recursively drops struct fields that implement Drop or themselves have droppable fields.
    /// For each field of a struct, if the field type needs dropping, GEP to the field
    /// and emit a drop call. Called after the parent's own Drop.Drop (if any).
    /// </summary>
    private void EmitFieldDrops(LangPath typePath, LLVMValueRef valuePtr)
    {
        // ManuallyDrop<T> suppresses all field drops — that's its entire purpose
        if (IsManuallyDrop(typePath)) return;

        var typeRef = GetRefItemFor(typePath) as TypeRefItem;

        // Struct field drops
        if (typeRef?.Type is StructType structType)
        {
            for (int i = 0; i < structType.ResolvedFieldTypes?.Length; i++)
            {
                var fieldType = structType.ResolvedFieldTypes.Value[i];
                var fieldTypePath = fieldType.TypePath;

                bool fieldNeedsDrop = IsTypeDrop(fieldTypePath) || TypeHasDroppableFields(fieldTypePath);
                if (!fieldNeedsDrop) continue;

                var fieldPtr = Builder.BuildStructGEP2(structType.TypeRef, valuePtr, (uint)i);

                if (IsTypeDrop(fieldTypePath))
                    EmitSingleDropCall(fieldTypePath, fieldPtr);

                // Recurse into the field's own fields
                EmitFieldDrops(fieldTypePath, fieldPtr);
            }
            return;
        }

        // Enum variant field drops — switch on tag, drop the active variant's fields
        if (typeRef?.Type is EnumType enumType && enumType.HasPayloads && enumType.ResolvedVariants != null)
        {
            // Check if any variant has droppable fields
            bool anyDroppable = false;
            foreach (var (variant, fieldTypes) in enumType.ResolvedVariants)
                foreach (var ft in fieldTypes)
                    if (IsTypeDrop(ft.TypePath) || TypeHasDroppableFields(ft.TypePath))
                    { anyDroppable = true; break; }
            if (!anyDroppable) return;

            // Load the tag
            var tagPtr = Builder.BuildStructGEP2(enumType.TypeRef, valuePtr, 0);
            var tagVal = Builder.BuildLoad2(LLVMTypeRef.Int32, tagPtr, "drop_enum_tag");

            var currentBlock = Builder.InsertBlock;
            var function = currentBlock.Parent;
            var mergeBlock = function.AppendBasicBlock("enum_drop_merge");

            // Create switch on tag
            var switchInst = Builder.BuildSwitch(tagVal, mergeBlock,
                (uint)enumType.ResolvedVariants.Value.Length);

            foreach (var (variant, fieldTypes) in enumType.ResolvedVariants)
            {
                // Check if this variant has any droppable fields
                bool variantNeedsDrop = false;
                foreach (var ft in fieldTypes)
                    if (IsTypeDrop(ft.TypePath) || TypeHasDroppableFields(ft.TypePath))
                    { variantNeedsDrop = true; break; }
                if (!variantNeedsDrop) continue;

                var variantBlock = function.AppendBasicBlock($"enum_drop_{variant.Name}");
                switchInst.AddCase(
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)variant.Tag, false),
                    variantBlock);

                Builder.PositionAtEnd(variantBlock);

                // Get payload pointer
                var payloadPtr = Builder.BuildStructGEP2(enumType.TypeRef, valuePtr, 1);

                // Drop each field at its byte offset
                ulong offset = 0;
                for (int i = 0; i < fieldTypes.Length; i++)
                {
                    var fieldType = fieldTypes[i];
                    bool fieldNeedsDrop = IsTypeDrop(fieldType.TypePath) || TypeHasDroppableFields(fieldType.TypePath);

                    if (fieldNeedsDrop)
                    {
                        var fieldPtr = payloadPtr;
                        if (offset > 0)
                            fieldPtr = Builder.BuildGEP2(
                                LLVMTypeRef.Int8, payloadPtr,
                                [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, offset, false)]);

                        if (IsTypeDrop(fieldType.TypePath))
                            EmitSingleDropCall(fieldType.TypePath, fieldPtr);
                        EmitFieldDrops(fieldType.TypePath, fieldPtr);
                    }

                    unsafe
                    {
                        var dataLayout = LLVM.GetModuleDataLayout(Module);
                        offset += LLVM.StoreSizeOfType(dataLayout, fieldType.TypeRef);
                    }
                }

                Builder.BuildBr(mergeBlock);
            }

            Builder.PositionAtEnd(mergeBlock);
        }
    }

    /// <summary>
    /// Checks if a type is Std.Mem.ManuallyDrop — suppresses all drop propagation.
    /// Only the official ManuallyDrop from std is recognized, not user-defined types with the same name.
    /// </summary>
    private static readonly NormalLangPath ManuallyDropModule = new(null, ["Std", "Mem", "ManuallyDrop"]);

    private static bool IsManuallyDrop(LangPath typePath)
    {
        if (typePath is not NormalLangPath nlp) return false;
        // Strip generics so ManuallyDrop(Box(i32)) matches ManuallyDrop
        var stripped = nlp.PopGenerics() ?? nlp;
        return stripped.Contains(ManuallyDropModule);
    }

    /// <summary>
    /// Checks if a type is a struct with any field (transitively) that implements Drop.
    /// Used to determine if a type needs drop glue even if it doesn't implement Drop itself.
    /// </summary>
    public bool TypeHasDroppableFields(LangPath typePath)
    {
        if (IsManuallyDrop(typePath)) return false;

        var typeRef = GetRefItemFor(typePath) as TypeRefItem;

        // Struct fields
        if (typeRef?.Type is StructType structType && structType.ResolvedFieldTypes != null)
        {
            foreach (var fieldType in structType.ResolvedFieldTypes)
            {
                if (IsTypeDrop(fieldType.TypePath)) return true;
                if (TypeHasDroppableFields(fieldType.TypePath)) return true;
            }
        }

        // Enum variant fields
        if (typeRef?.Type is EnumType enumType && enumType.ResolvedVariants != null)
        {
            foreach (var (variant, fieldTypes) in enumType.ResolvedVariants)
                foreach (var ft in fieldTypes)
                {
                    if (IsTypeDrop(ft.TypePath)) return true;
                    if (TypeHasDroppableFields(ft.TypePath)) return true;
                }
        }

        return false;
    }

    /// <summary>
    /// Public entry point for emitting a drop call on a value at the given pointer.
    /// Used by IntrinsicCodeGen for Box.Drop.
    /// </summary>
    public void EmitSingleDropCallPublic(LangPath typePath, LLVMValueRef valuePtr)
    {
        EmitSingleDropCall(typePath, valuePtr);
    }

    /// <summary>
    /// Public entry point for recursively dropping struct fields.
    /// Used by IntrinsicCodeGen for Box.Drop to drop T's fields.
    /// </summary>
    public void EmitFieldDropsPublic(LangPath typePath, LLVMValueRef valuePtr)
    {
        EmitFieldDrops(typePath, valuePtr);
    }

    /// <summary>
    /// Destructs a value at a pointer: calls Drop (if implemented) then recursively drops fields.
    /// Used by the DestructPtr intrinsic to destruct heap-allocated values before deallocation.
    /// </summary>
    public void EmitDestruct(LangPath typePath, LLVMValueRef valuePtr)
    {
        if (IsTypeDrop(typePath))
            EmitSingleDropCall(typePath, valuePtr);
        EmitFieldDrops(typePath, valuePtr);
    }

    /// <summary>
    /// Emits a single drop call for a variable at the given pointer.
    /// Directly searches ImplDefinitions for the Drop impl and resolves the method.
    /// </summary>
    private void EmitSingleDropCall(LangPath typePath, LLVMValueRef valuePtr)
    {
        var dropTraitPath = SemanticAnalyzer.DropTraitPath;

        // Find the Drop impl for this concrete type
        ImplDefinition? dropImpl = null;
        Dictionary<string, LangPath>? implBindings = null;

        foreach (var impl in ImplDefinitions)
        {
            var implTraitBase = LangPath.StripGenerics(impl.TraitPath);
            if (implTraitBase != dropTraitPath) continue;

            var bindings = impl.TryMatchConcreteType(typePath);
            if (bindings != null)
            {
                dropImpl = impl;
                implBindings = bindings;
                break;
            }
        }

        if (dropImpl == null) return;

        var dropMethod = dropImpl.GetMethod("Drop");
        if (dropMethod == null) return;

        // Build a unique key for the drop method impl
        var implMethodPath = new NormalLangPath(null,
            [new NormalLangPath.NormalPathSegment($"impl_{dropImpl.TraitPath}_for_{typePath}"),
             new NormalLangPath.NormalPathSegment("Drop")]);

        // Check if the drop function was already created (reuse for multiple vars of same type)
        foreach (var scope in ScopeItems)
        {
            if (scope.TryGetValue(implMethodPath, out var existing) && existing is FunctionRefItem efr)
            {
                Builder.BuildCall2(efr.Function.FunctionType, efr.Function.FunctionValueRef, [valuePtr]);
                return;
            }
        }

        // Check function cache
        if (GetCachedFunction(implMethodPath) is FunctionRefItem cdr)
        {
            AddToScope(implMethodPath, cdr, 0);
            Builder.BuildCall2(cdr.Function.FunctionType, cdr.Function.FunctionValueRef, [valuePtr]);
            return;
        }

        // Set up the scope for generic resolution
        PushImplBindingsScope(implBindings, typePath);

        // Push trait bounds from the impl's generic parameters
        bool pushedTraitBounds = false;
        if (implBindings != null && implBindings.Count > 0)
        {
            var monoArgs = dropImpl.GenericParameters
                .Select(gp => implBindings.TryGetValue(gp.Name, out var bt)
                    ? (GetRefItemFor(bt) as TypeRefItem)?.Type.TypePath ?? bt
                    : (LangPath)new NormalLangPath(null, [gp.Name]))
                .ToImmutableArray();

            var traitBounds = new List<(LangPath, LangPath)>();
            for (int i = 0; i < dropImpl.GenericParameters.Length; i++)
            {
                var gp = dropImpl.GenericParameters[i];
                foreach (var bound in gp.TraitBounds)
                {
                    var resolvedBound = FieldAccessExpression.SubstituteGenerics(
                        bound.TraitPath, dropImpl.GenericParameters, monoArgs);
                    traitBounds.Add((resolvedBound, monoArgs[i]));
                }
            }
            if (traitBounds.Count > 0)
            {
                PushTraitBounds(traitBounds);
                pushedTraitBounds = true;
            }
        }

        var methodGenerics = ImmutableArray<LangPath>.Empty;
        var refItem = dropMethod.CreateRefDefinition(this, methodGenerics);

        PopScope();
        if (pushedTraitBounds) PopTraitBounds();

        if (refItem is FunctionRefItem funcRef)
        {
            if (implBindings != null && implBindings.Count > 0)
            {
                funcRef.Function.ImplGenericBindings = implBindings;
                funcRef.Function.ImplGenericParameters = dropImpl.GenericParameters;
            }
            UnimplementedFunctions.Push(funcRef);
            CacheFunction(implMethodPath, refItem);
            AddToScope(implMethodPath, refItem, 0);

            Builder.BuildCall2(funcRef.Function.FunctionType, funcRef.Function.FunctionValueRef, [valuePtr]);
        }
    }

    /// <summary>
    /// During generic function codegen, maps trait paths to concrete types
    /// based on current generic param trait bounds
    /// </summary>
    private readonly Stack<List<(LangPath traitPath, LangPath concreteType)>> TraitBoundsStack = new();

    public void PushTraitBounds(List<(LangPath, LangPath)> bounds)
    {
        TraitBoundsStack.Push(bounds);
    }

    public void PopTraitBounds()
    {
        if (TraitBoundsStack.Count > 0)
            TraitBoundsStack.Pop();
    }

    /// <summary>
    /// Checks if a trait bound is currently in scope (e.g., T: Add&lt;i32&gt;).
    /// </summary>
    public bool HasTraitBound(LangPath traitPath)
    {
        foreach (var bounds in TraitBoundsStack)
            foreach (var (tp, _) in bounds)
                if (tp == traitPath)
                    return true;
        return false;
    }

    /// <summary>
    /// Pushes a new scope with impl generic bindings and Self registered.
    /// Returns true if a scope was pushed (caller must call PopScope()).
    /// </summary>
    private bool PushImplBindingsScope(Dictionary<string, LangPath>? implBindings, LangPath? selfType)
    {
        if ((implBindings == null || implBindings.Count == 0) && selfType == null)
            return false;

        AddScope();
        if (implBindings != null)
        {
            foreach (var (paramName, boundType) in implBindings)
            {
                var boundRefItem = GetRefItemFor(boundType);
                if (boundRefItem != null)
                    AddToDeepestScope(new NormalLangPath(null, [paramName]), boundRefItem);
            }
        }
        if (selfType != null)
        {
            var selfRefItem = GetRefItemFor(selfType);
            if (selfRefItem != null)
                AddToDeepestScope(new NormalLangPath(null, ["Self"]), selfRefItem);
        }
        return true;
    }

    /// <summary>
    /// Gets the concrete type associated with a trait bound, if any
    /// </summary>
    private LangPath? GetConcreteTypeForTrait(LangPath traitPath)
    {
        foreach (var bounds in TraitBoundsStack)
            foreach (var (tp, ct) in bounds)
            {
                if (tp == traitPath) return ct;
                // Also match after stripping generics (e.g., Add(i32) matches Add)
                if (LangPath.StripGenerics(tp) == traitPath) return ct;
            }
        return null;
    }

    /// <summary>
    /// Resolves a trait method call path (e.g., TraitName.Method or T.Method) to a FunctionRefItem
    /// </summary>
    /// <summary>
    /// Convenience reference to ImplDefinition.InherentSentinel
    /// </summary>
    private static readonly NormalLangPath InherentSentinel = ImplDefinition.InherentSentinel;

    private TraitDefinition? FindMethodInSupertraits(TraitDefinition td, string methodName)
    {
        foreach (var supertrait in td.Supertraits)
        {
            var lookupPath = LangPath.StripGenerics(supertrait.TraitPath);
            var superDef = DefinitionsCollection.OfType<TraitDefinition>()
                .FirstOrDefault(t => (t as IDefinition).TypePath == lookupPath);
            if (superDef != null)
            {
                if (superDef.GetMethod(methodName) != null)
                    return superDef;
                var deeper = FindMethodInSupertraits(superDef, methodName);
                if (deeper != null) return deeper;
            }
        }
        return null;
    }

    /// <summary>
    /// Shared helper: checks scope cache, then creates and registers an impl method function.
    /// Used by ResolveInherentMethod, ResolveTraitMethodCall, and EmitSingleDropCall.
    /// </summary>
    private IRefItem? CreateImplMethodRef(
        ImplDefinition impl, string methodName, LangPath concreteType,
        Dictionary<string, LangPath>? implBindings, ImmutableArray<LangPath> methodLevelGenerics,
        string implPathKey)
    {
        var methodSegment = methodLevelGenerics.Length > 0
            ? new NormalLangPath.NormalPathSegment(methodName, methodLevelGenerics)
            : new NormalLangPath.NormalPathSegment(methodName);
        var implMethodPath = new NormalLangPath(null,
            [new NormalLangPath.NormalPathSegment(implPathKey), methodSegment]);

        // Check if already created
        foreach (var scope in ScopeItems)
            if (scope.TryGetValue(implMethodPath, out var existing))
                return existing;

        // Check function cache
        var cachedImpl = GetCachedFunction(implMethodPath);
        if (cachedImpl != null)
        {
            AddToScope(implMethodPath, cachedImpl, 0);
            return cachedImpl;
        }

        var implMethod = impl.GetMethod(methodName);
        if (implMethod == null) return null;

        var pushedScope = PushImplBindingsScope(implBindings, concreteType);
        var refItem = implMethod.CreateRefDefinition(this, methodLevelGenerics);
        if (pushedScope) PopScope();

        if (refItem is FunctionRefItem functionRefItem)
        {
            if (implBindings != null && implBindings.Count > 0)
            {
                functionRefItem.Function.ImplGenericBindings = implBindings;
                functionRefItem.Function.ImplGenericParameters = impl.GenericParameters;
            }
            UnimplementedFunctions.Push(functionRefItem);
            CacheFunction(implMethodPath, refItem);
        }

        AddToScope(implMethodPath, refItem, 0);
        return refItem;
    }

    /// <summary>
    /// Resolves a method from an inherent impl (impl Type { fn method... }).
    /// </summary>
    private IRefItem? ResolveInherentMethod(LangPath concreteType, string methodName,
        ImmutableArray<LangPath> methodLevelGenerics)
    {
        foreach (var candidate in ImplDefinitions)
        {
            if (!candidate.IsInherent) continue;
            if (candidate.GetMethod(methodName) == null) continue;

            var bindings = candidate.TryMatchConcreteType(concreteType);
            if (bindings != null && candidate.CheckBoundsCodeGen(bindings, this))
                return CreateImplMethodRef(candidate, methodName, concreteType, bindings,
                    methodLevelGenerics, $"impl_{concreteType}");
        }

        return null;
    }

    public IRefItem? ResolveTraitMethodCall(NormalLangPath path)
    {
        if (path.PathSegments.Length < 2) return null;

        // Strip trailing method-level generics if present, but remember them
        var (workingPath, methodLevelGenerics) = LangPath.SplitGenerics(path);

        if (workingPath is not NormalLangPath workingNlp || workingNlp.PathSegments.Length < 2) return null;

        var lastSeg = workingNlp.GetLastPathSegment();
        if (lastSeg == null) return null;
        var methodName = lastSeg.ToString();
        var parentPath = workingNlp.Pop();
        if (parentPath == null || parentPath.PathSegments.Length == 0) return null;

        LangPath? resolvedTraitPath = null;
        LangPath? concreteType = null;

        // Case 1: TraitName.Method — parent is a trait directly (strip generics for lookup)
        var traitLookupPath = LangPath.StripGenerics(parentPath);
        var traitDef = DefinitionsCollection.OfType<TraitDefinition>()
            .FirstOrDefault(t => (t as IDefinition).TypePath == traitLookupPath);
        if (traitDef != null)
        {
            resolvedTraitPath = (traitDef as IDefinition).TypePath;
            concreteType = GetConcreteTypeForTrait(resolvedTraitPath);
        }
        else
        {
            // Case 2: T.Method — parent is a generic param with a trait bound
            // T is in scope as a TypeRefItem pointing to the concrete type
            var refItem = GetRefItemFor(parentPath, false);
            if (refItem is TypeRefItem typeRef)
            {
                concreteType = typeRef.Type.TypePath;
                // Search all trait bounds for one whose concrete type matches,
                // then verify that trait has the requested method
                foreach (var bounds in TraitBoundsStack)
                {
                    foreach (var (tp, ct) in bounds)
                    {
                        if (ct == concreteType)
                        {
                            var lookupTp = LangPath.StripGenerics(tp);
                            var candidateTrait = DefinitionsCollection.OfType<TraitDefinition>()
                                .FirstOrDefault(t => (t as IDefinition).TypePath == lookupTp);
                            if (candidateTrait?.GetMethod(methodName) != null)
                            {
                                resolvedTraitPath = lookupTp;
                                break;
                            }
                            // Check supertraits for the method
                            if (candidateTrait != null)
                            {
                                var superTd = FindMethodInSupertraits(candidateTrait, methodName);
                                if (superTd != null)
                                {
                                    resolvedTraitPath = superTd.TypePath;
                                    break;
                                }
                            }
                        }
                    }
                    if (resolvedTraitPath != null) break;
                }

                // Case 3: ConcreteType.Method — no trait bounds, search impls directly
                // Handles i32.Default() syntax and inherent impls
                if (resolvedTraitPath == null)
                {
                    foreach (var i in ImplDefinitions)
                    {
                        var match = i.TryMatchConcreteType(concreteType);
                        if (match != null && i.CheckBoundsCodeGen(match, this))
                        {
                            if (i.IsInherent)
                            {
                                // Inherent impl: check method directly on the impl
                                if (i.GetMethod(methodName) != null)
                                {
                                    resolvedTraitPath = InherentSentinel;
                                    break;
                                }
                            }
                            else
                            {
                                var implTraitLookup = LangPath.StripGenerics(i.TraitPath);
                                var candidateTrait = DefinitionsCollection.OfType<TraitDefinition>()
                                    .FirstOrDefault(t => (t as IDefinition).TypePath == implTraitLookup);
                                if (candidateTrait?.GetMethod(methodName) != null)
                                {
                                    resolvedTraitPath = implTraitLookup;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }

        if (resolvedTraitPath == null || concreteType == null) return null;

        // For inherent impls, find the impl directly and resolve the method
        if (resolvedTraitPath == InherentSentinel)
        {
            return ResolveInherentMethod(concreteType, methodName, methodLevelGenerics);
        }

        // Verify the trait actually has this method
        var resolvedTrait = DefinitionsCollection.OfType<TraitDefinition>()
            .FirstOrDefault(t => (t as IDefinition).TypePath == resolvedTraitPath);
        if (resolvedTrait?.GetMethod(methodName) == null) return null;

        // Capture expected trait generics from the parentPath (e.g., Add(Foo) → [Foo])
        // Only use when parent IS the trait (Case 1). For concrete type calls (Case 2/3),
        // the parent generics are TYPE generics, not trait generics.
        var expectedTraitGenerics = ImmutableArray<LangPath>.Empty;
        if (traitDef != null)
        {
            (_, expectedTraitGenerics) = LangPath.SplitGenerics(parentPath);
        }

        // Find the impl definition for this trait + concrete type (supports generic impls)
        ImplDefinition? impl = null;
        Dictionary<string, LangPath>? implBindings = null;
        foreach (var candidate in ImplDefinitions)
        {
            // Compare trait base paths (strip generics)
            var (candidateTraitBase, candidateTraitGenerics) = LangPath.SplitGenerics(candidate.TraitPath);

            if (candidateTraitBase != resolvedTraitPath) continue;
            var bindings = candidate.TryMatchConcreteType(concreteType);
            if (bindings == null) continue;

            // Also verify trait generic args match (e.g., Add(Foo) vs Add(i32))
            if (expectedTraitGenerics.Length > 0)
            {
                if (candidateTraitGenerics.Length != expectedTraitGenerics.Length) continue;
                var freeVars = candidate.GenericParameters.Select(g => g.Name).ToHashSet();
                bool genericsMatch = true;
                for (int idx = 0; idx < expectedTraitGenerics.Length; idx++)
                {
                    if (!TypeInference.TryUnify(candidateTraitGenerics[idx], expectedTraitGenerics[idx], freeVars, bindings))
                    {
                        genericsMatch = false;
                        break;
                    }
                }
                if (!genericsMatch) continue;
            }

            if (candidate.CheckBoundsCodeGen(bindings, this))
            {
                impl = candidate;
                implBindings = bindings;
                break;
            }
        }
        if (impl == null) return null;

        return CreateImplMethodRef(impl, methodName, concreteType, implBindings,
            methodLevelGenerics, $"impl_{impl.TraitPath}_for_{concreteType}");
    }

    CodeGenContext(IEnumerable<IDefinition> definitions, NormalLangPath mainLangModule)
    {
        MainLangModule = mainLangModule;
        TopLevelDefinitions = definitions.ToList();
    }

    /// <summary>
    /// Top-level impl definitions collected during construction,
    /// registered into the scoped stack when BuildModule creates the first scope.
    /// </summary>
    private readonly List<ImplDefinition> _topLevelImpls = new();

    CodeGenContext(IEnumerable<ParseResult> results, NormalLangPath mainLangModule) : this(
        results.SelectMany(i => i.Items.OfType<IDefinition>())
            .Concat(results.SelectMany(i => i.Items.OfType<ImplDefinition>()
                .SelectMany(impl => impl.Methods))),
        mainLangModule)
    {
        // Collect top-level impl definitions — registered into scoped stack in BuildModule
        foreach (var result in results)
            foreach (var impl in result.Items.OfType<ImplDefinition>())
                _topLevelImpls.Add(impl);
    }

    public NormalLangPath MainLangModule { get; }

    public LLVMBuilderRef Builder { get; private set; }

    public LLVMModuleRef Module { get; private set; }


    private readonly Stack<List<IDefinition>> DefinitionsStack = new();

    private IEnumerable<IDefinition> DefinitionsCollection
    {
        get
        {

            foreach (var i in DefinitionsStack)
            {
                foreach (var j in i)
                {
                    yield return j;
                }
            }
        }
    }

    public bool HasIdent(LangPath langPath)
    {
        foreach (var scope in ScopeItems)
            if (scope.TryGetValue(langPath, out var symbol))
                return true;

        return false;
    }

    private Stack<FunctionRefItem> UnimplementedFunctions = new();

    public 
    IRefItem? SetupIfPossible(LangPath ident)
    {
        // Check persistent type cache first — survives scope push/pop cycles
        if (_typeRefCache.TryGetValue(ident, out var cached))
        {
            // Re-register in current scope for fast lookup
            var scope = GetScope(ident);
            if (scope != null)
                AddToScope(ident, cached, scope.Value);
            return cached;
        }

        // Check function cache — reuse already-compiled monomorphized functions
        var cachedFn = GetCachedFunction(ident);
        if (cachedFn != null)
        {
            var scope = GetScope(ident);
            if (scope != null)
                AddToScope(ident, cachedFn, scope.Value);
            return cachedFn;
        }

        var first = DefinitionsCollection.OfType<IMonomorphizable>().FirstOrDefault(i =>
        {
            
            return ident.IsMonomorphizedFrom(i.TypePath);
        });
        if (first == null && ident is TupleLangPath tupleLangPath)
        {
            first = new TupleTypeDefinition(tupleLangPath.TypePaths);
            DefinitionsStack.Last().Add(first);
        } 
        // generate and store the type if not already, and it is defined
        if (first != null)
        {

            var genericArguments = ident.GetGenericArguments();
            var refItem = first.CreateRefDefinition(this, genericArguments);
            if (refItem is FunctionRefItem functionRefItem)
            {
                UnimplementedFunctions.Push(functionRefItem);
                // Cache function definitions persistently by their comptime params
                CacheFunction(ident, refItem);
            }

            // Cache type definitions persistently
            if (refItem is TypeRefItem)
                _typeRefCache[ident] = refItem;

            var scope = GetScope(ident).Value;
            AddToScope(ident, refItem, scope);
            return refItem;
        }

        // Try to resolve as a trait method call
        if (ident is NormalLangPath normalPath)
        {
            var traitResult = ResolveTraitMethodCall(normalPath);
            if (traitResult != null) return traitResult;
        }

        return null;
        
    }
    
    public IRefItem? GetRefItemFor(LangPath ident, bool monomorphizePath = true)
    {
        if (monomorphizePath) ident = ident.Monomorphize(this) ?? ident;

        foreach (var scope in ScopeItems)
        {
            if (scope.TryGetValue(ident, out var symbol))
                return symbol;

        }
          
        return SetupIfPossible(ident);

    }


    public void AddToDeepestScope(LangPath symb, IRefItem refItem)
    {
        ScopeItems.Peek()[symb] = refItem;
    }
    public void AddToDeepestScope(IDefinition definition)
    {
        DefinitionsStack.Peek().Add(definition);
    }
    /// <summary>
    /// </summary>
    /// <param name="symb"></param>
    /// <param name="refItem"></param>
    /// <param name="scope">NOTE: The index fort the most outerscope would be 1 in this case</param>
    public void AddToScope(LangPath symb, IRefItem refItem, int scope)
    {
        ScopeItems.Reverse().Skip(scope).First().Add(symb, refItem);
    }
    
    public int? GetScope(LangPath path)
    {
        var scope = 0;

        foreach (var i in DefinitionsStack.Reverse())
        {
            foreach (var j in i )
            {
                if (path.IsMonomorphizedFrom(j.TypePath))
                {
                    return scope;
                }
            }
        
            scope++;
        }

        return null;
    }

    public void AddScope()
    {
        DefinitionsStack.Push(new List<IDefinition>());
        ScopeItems.Push(new Dictionary<LangPath, IRefItem>());
        ScopeDropFlags.Push(new List<(string, LLVMValueRef, LangPath, LLVMValueRef)>());
        _implDefinitionsStack.Push(new List<ImplDefinition>());
        _functionRefCache.Push(new Dictionary<LangPath, IRefItem>());
    }

    public void PopScope()
    {
        ScopeItems.Pop();
        DefinitionsStack.Pop();
        ScopeDropFlags.Pop();
        _implDefinitionsStack.Pop();
        _functionRefCache.Pop();
    }


    public ValueRefItem GetVoid()
    {
        return Void;
    }

    public static unsafe string? FromByte(sbyte* value)
    {
        return Marshal.PtrToStringAnsi((IntPtr)value);
    }

    private void SetupVoid()
    {


        var typeRef =(TypeRefItem) GetRefItemFor(new TupleLangPath([]));
      
        Void = new ValueRefItem()
        {
            Type = typeRef.Type,
            ValueRef = LLVMValueRef.CreateConstStruct([],false)
        };
    }

    public static Func<int>? CodeGenMain(IEnumerable<ParseResult> results, NormalLangPath mainLangModule,
        bool showLLVMIR = false, bool optimized = false)
    {
        var context = new CodeGenContext(results, mainLangModule);
        return context.CodeGenInst(showLLVMIR, optimized);
    }

    public static bool CodeGenToObjectFile(IEnumerable<ParseResult> results, NormalLangPath mainLangModule,
        string objectFilePath, bool showLLVMIR = false, bool optimized = false)
    {
        var context = new CodeGenContext(results, mainLangModule);
        return context.CodeGenToObjectFileInst(objectFilePath, showLLVMIR, optimized);
    }

    void HandleOptimizations()
    {
        unsafe
        {
            LLVMOpaquePassBuilderOptions* options = LLVM.CreatePassBuilderOptions();
            sbyte* passPipeline = null;

            try
            {
                LLVM.PassBuilderOptionsSetVerifyEach(options, 1);
                LLVM.PassBuilderOptionsSetDebugLogging(options, 0);
                LLVM.PassBuilderOptionsSetLoopInterleaving(options, 1);
                LLVM.PassBuilderOptionsSetLoopVectorization(options, 1);
                LLVM.PassBuilderOptionsSetSLPVectorization(options, 1);
                LLVM.PassBuilderOptionsSetLoopUnrolling(options, 1);
                LLVM.PassBuilderOptionsSetMergeFunctions(options, 1);
                LLVM.PassBuilderOptionsSetInlinerThreshold(options, 225);

                // Easiest choice:
                passPipeline = ToUtf8("default<O2>");
               
                LLVMOpaqueError* error = LLVM.RunPasses(Module, passPipeline, CreateHostTargetMachine(), options);
                ThrowIfError(error);
            }
            finally
            {
                if (passPipeline != null)
                    Marshal.FreeCoTaskMem((nint)passPipeline);

                LLVM.DisposePassBuilderOptions(options);
            }   
        }

    }
    static unsafe sbyte* ToUtf8(string text)
    {
        return (sbyte*)Marshal.StringToCoTaskMemUTF8(text);
    }

    static unsafe void ThrowIfError(LLVMOpaqueError* error)
    {
        if (error == null)
            return;

        sbyte* messagePtr = LLVM.GetErrorMessage(error);
        try
        {
            string message = Marshal.PtrToStringUTF8((nint)messagePtr) ?? "LLVM pass pipeline failed.";
            throw new InvalidOperationException(message);
        }
        finally
        {
            LLVM.DisposeErrorMessage(messagePtr);
        }
    }
    unsafe LLVMOpaqueTargetMachine* CreateHostTargetMachine()
    {
        if (LLVM.InitializeNativeTarget() != 0)
            throw new InvalidOperationException("LLVMInitializeNativeTarget failed.");

        if (LLVM.InitializeNativeAsmPrinter() != 0)
            throw new InvalidOperationException("LLVMInitializeNativeAsmPrinter failed.");

        sbyte* triple = LLVM.GetDefaultTargetTriple();
        sbyte* cpu = LLVM.GetHostCPUName();
        sbyte* features = LLVM.GetHostCPUFeatures();

        try
        {
            LLVMTarget* target;
            sbyte* errorMessage = null;

            if (LLVM.GetTargetFromTriple(triple, &target, &errorMessage) != 0)
            {
                string message = Marshal.PtrToStringUTF8((nint)errorMessage) ?? "LLVMGetTargetFromTriple failed.";
                if (errorMessage != null)
                    LLVM.DisposeMessage(errorMessage);

                throw new InvalidOperationException(message);
            }

            LLVMOpaqueTargetMachine* targetMachine = LLVM.CreateTargetMachine(
                target,
                triple,
                cpu,
                features,
                LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
                LLVMRelocMode.LLVMRelocDefault,
                LLVMCodeModel.LLVMCodeModelDefault);

            if (targetMachine == null)
                throw new InvalidOperationException("LLVMCreateTargetMachine returned null.");

            return targetMachine;
        }
        finally
        {
            LLVM.DisposeMessage(triple);
            LLVM.DisposeMessage(cpu);
            LLVM.DisposeMessage(features);
        }
    }
    
    public LLVMContextRef LLVMContext => Module.Context;

    /// <summary>
    /// Shared codegen: builds the LLVM module with all functions.
    /// Returns the main function's LLVM ValueRef, or null on failure.
    /// </summary>
    private unsafe LLVMValueRef? BuildModule(bool showLLVMIR, bool optimized)
    {
        const string MODULE_NAME = "LEGENDARY_LANGUAGE";
        Module = LLVM.ModuleCreateWithName(MODULE_NAME.ToCString());

      
        Builder = LLVM.CreateBuilderInContext(LLVMContext);
        

        AddScope();
        foreach (var i in TopLevelDefinitions)
        {
            AddToDeepestScope(i);
        }
        foreach (var impl in _topLevelImpls)
        {
            AddImplDefinition(impl);
        }
        SetupVoid();

 
        var mainDef = DefinitionsCollection.OfType<FunctionDefinition>().First(i =>
        {
            return i.Module == MainLangModule && i.Name == "main";
        });
        var mainDefRefItem = (FunctionRefItem) mainDef.CreateRefDefinition(this,[]);
        
        AddToDeepestScope(new NormalLangPath(null, [..MainLangModule, "main"]), mainDefRefItem);
         mainDef.ImplementMonomorphized(this,mainDefRefItem.Function);
    
        
        while (UnimplementedFunctions.Count > 0)
        {
            var get = UnimplementedFunctions.Pop();
 
            if (get is FunctionRefItem functionRef)
            {
                functionRef.Function.Definition.ImplementMonomorphized(this,functionRef.Function);
            }
     
        }
        if (optimized)
        {
               HandleOptimizations();
        }

        if (showLLVMIR) 
            Console.WriteLine(FromByte(LLVM.PrintModuleToString(Module)));

        sbyte* idk;
        if (LLVM.VerifyModule(Module, LLVMVerifierFailureAction.LLVMPrintMessageAction, &idk) != 0)
        {
            var errMsg = Marshal.PtrToStringAnsi((IntPtr)idk);
            Console.WriteLine("LLVM Module Verification Failed:\n" + errMsg);
            return null;
        }

        var mainFnPath = mainDefRefItem.Function.FullPath;
        LLVMValueRef mainFnPtr = LLVM.GetNamedFunction(Module, mainFnPath.ToString().ToCString());

        if (mainFnPtr.Handle == IntPtr.Zero)
        {
            Console.WriteLine("main function not found!");
            return null;
        }

        return mainFnPtr;
    }

    private unsafe Func<int>? CodeGenInst(bool showLLVMIR = false, bool optimized = false)
    {
        var mainFnPtr = BuildModule(showLLVMIR, optimized);
        if (mainFnPtr == null) return null;

        // Use MCJIT which resolves malloc/free/calloc through the system's dynamic linker.
        // The interpreter can't resolve external C functions, but MCJIT can.
        LLVM.LinkInMCJIT();
        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
        LLVM.InitializeAllAsmParsers();
        LLVM.InitializeAllAsmPrinters();

        LLVMMCJITCompilerOptions options;
        LLVM.InitializeMCJITCompilerOptions(&options, (nuint)sizeof(LLVMMCJITCompilerOptions));

        LLVMOpaqueExecutionEngine* enginePtr;
        sbyte* errorMsg;
        if (LLVM.CreateMCJITCompilerForModule(&enginePtr, Module, &options,
                (nuint)sizeof(LLVMMCJITCompilerOptions), &errorMsg) != 0)
        {
            Console.WriteLine($"Failed to create MCJIT: {FromByte(errorMsg)}");
            return null;
        }
        var engine = (LLVMExecutionEngineRef)enginePtr;

        if (showLLVMIR)
            Console.WriteLine(FromByte(LLVM.PrintModuleToString(Module)));

        // Capture the function name for GetFunctionAddress (must be a string, not LLVMValueRef)
        var mainFnName = mainFnPtr.Value.Name;

        var mainFunctionDelegate = () =>
        {
            // Get the main function's address from the JIT and call it natively
            var addr = LLVM.GetFunctionAddress(engine, mainFnName.ToCString());
            if (addr == 0)
            {
                Console.Error.WriteLine($"Failed to get function address for: {mainFnName}");
                return -1;
            }
            // Call the JIT'd function directly via function pointer
            var fn = (delegate* unmanaged[Cdecl]<int>)(nint)addr;
            return fn();
        };
        return mainFunctionDelegate;
    }

    /// <summary>
    /// Builds the LLVM module, adds a C-compatible main() wrapper, and emits an object file.
    /// </summary>
    private unsafe bool CodeGenToObjectFileInst(string objectFilePath, bool showLLVMIR = false, bool optimized = false)
    {
        var userMainPtr = BuildModule(showLLVMIR, optimized);
        if (userMainPtr == null) return false;

        // Create a C-compatible main() entry point that calls the user's main
        var i32Type = LLVMTypeRef.Int32;
        var mainFuncType = LLVMTypeRef.CreateFunction(i32Type, []);
        var mainFunc = Module.AddFunction("main", mainFuncType);

        var entryBlock = mainFunc.AppendBasicBlock("entry");
        Builder.PositionAtEnd(entryBlock);

        // Call the user's main function
        var userMainFuncType = LLVMTypeRef.CreateFunction(i32Type, []);
        var result = Builder.BuildCall2(userMainFuncType, userMainPtr.Value, new LLVMValueRef[] { }, "result");
        Builder.BuildRet(result);

        // Verify again after adding the wrapper
        sbyte* verifyError;
        if (LLVM.VerifyModule(Module, LLVMVerifierFailureAction.LLVMPrintMessageAction, &verifyError) != 0)
        {
            var errMsg = Marshal.PtrToStringAnsi((IntPtr)verifyError);
            Console.WriteLine("LLVM Module Verification Failed after adding main wrapper:\n" + errMsg);
            return false;
        }

        if (showLLVMIR)
        {
            Console.WriteLine("\n=== Final IR with main wrapper ===");
            Console.WriteLine(FromByte(LLVM.PrintModuleToString(Module)));
        }

        // Initialize LLVM target for the host platform
        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
        LLVM.InitializeAllAsmParsers();
        LLVM.InitializeAllAsmPrinters();

        var triple = LLVM.GetDefaultTargetTriple();
        LLVM.SetTarget(Module, triple);

        LLVMSharp.Interop.LLVMTarget* rawTarget;
        sbyte* targetError;
        if (LLVM.GetTargetFromTriple(triple, &rawTarget, &targetError) != 0)
        {
            var errMsg = FromByte(targetError);
            Console.WriteLine($"Failed to get target: {errMsg}");
            return false;
        }

        var cpu = "generic".ToCString();
        var features = "".ToCString();
        var machine = LLVM.CreateTargetMachine(
            rawTarget, triple, cpu, features,
            LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
            LLVMRelocMode.LLVMRelocDefault,
            LLVMCodeModel.LLVMCodeModelDefault);

        if (machine == null)
        {
            Console.WriteLine("Failed to create target machine");
            return false;
        }

        // Set the module data layout from the target machine
        var dataLayout = LLVM.CreateTargetDataLayout(machine);
        LLVM.SetModuleDataLayout(Module, dataLayout);

        // Emit object file
        sbyte* emitError;
        if (LLVM.TargetMachineEmitToFile(machine, Module, objectFilePath.ToCString(),
                LLVMCodeGenFileType.LLVMObjectFile, &emitError) != 0)
        {
            var errMsg = FromByte(emitError);
            Console.WriteLine($"Failed to emit object file: {errMsg}");
            return false;
        }

        Console.WriteLine($"Object file emitted: {objectFilePath}");
        return true;
    }
}