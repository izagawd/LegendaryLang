using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
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
    /// Stores all impl definitions for trait method resolution
    /// </summary>
    public List<ImplDefinition> ImplDefinitions { get; } = new();

    /// <summary>
    /// During generic function codegen, maps trait paths to concrete types
    /// based on current generic param trait bounds
    /// </summary>
    public readonly Stack<List<(LangPath traitPath, LangPath concreteType)>> TraitBoundsStack = new();

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
    /// Gets the concrete type associated with a trait bound, if any
    /// </summary>
    public LangPath? GetConcreteTypeForTrait(LangPath traitPath)
    {
        foreach (var bounds in TraitBoundsStack)
            foreach (var (tp, ct) in bounds)
            {
                if (tp == traitPath) return ct;
                // Also match after stripping generics (e.g., Add<i32> matches Add)
                if (tp is NormalLangPath nlpTp && nlpTp.GetFrontGenerics().Length > 0)
                    if (nlpTp.PopGenerics() == traitPath) return ct;
            }
        return null;
    }

    /// <summary>
    /// Resolves a trait method call path (e.g., TraitName::method or T::method) to a FunctionRefItem
    /// </summary>
    public IRefItem? ResolveTraitMethodCall(NormalLangPath path)
    {
        if (path.PathSegments.Length < 2) return null;

        // Strip trailing method-level generics (turbofish) if present
        var workingPath = path;
        if (workingPath.GetFrontGenerics().Length > 0)
            workingPath = workingPath.PopGenerics()!;

        if (workingPath.PathSegments.Length < 2) return null;

        var lastSeg = workingPath.GetLastPathSegment();
        if (lastSeg == null) return null;
        var methodName = lastSeg.ToString();
        var parentPath = workingPath.Pop();
        if (parentPath == null || parentPath.PathSegments.Length == 0) return null;

        LangPath? resolvedTraitPath = null;
        LangPath? concreteType = null;

        // Case 1: TraitName::method — parent is a trait directly (strip generics for lookup)
        var traitLookupPath = parentPath;
        if (parentPath is NormalLangPath nlpParentTrait && nlpParentTrait.GetFrontGenerics().Length > 0)
            traitLookupPath = nlpParentTrait.PopGenerics();
        var traitDef = DefinitionsCollection.OfType<TraitDefinition>()
            .FirstOrDefault(t => (t as IDefinition).TypePath == traitLookupPath);
        if (traitDef != null)
        {
            resolvedTraitPath = (traitDef as IDefinition).TypePath;
            concreteType = GetConcreteTypeForTrait(resolvedTraitPath);
        }
        else
        {
            // Case 2: T::method — parent is a generic param with a trait bound
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
                            var candidateTrait = DefinitionsCollection.OfType<TraitDefinition>()
                                .FirstOrDefault(t => (t as IDefinition).TypePath == tp);
                            if (candidateTrait?.GetMethod(methodName) != null)
                            {
                                resolvedTraitPath = tp;
                                break;
                            }
                        }
                    }
                    if (resolvedTraitPath != null) break;
                }

                // Case 3: ConcreteType::method — no trait bounds, search impls directly
                // Handles i32::default() syntax
                if (resolvedTraitPath == null)
                {
                    foreach (var i in ImplDefinitions)
                    {
                        var match = i.TryMatchConcreteType(concreteType);
                        if (match != null && i.CheckBoundsCodeGen(match, this))
                        {
                            var implTraitLookup = i.TraitPath;
                            if (implTraitLookup is NormalLangPath nlpImplT && nlpImplT.GetFrontGenerics().Length > 0)
                                implTraitLookup = nlpImplT.PopGenerics();
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

        if (resolvedTraitPath == null || concreteType == null) return null;

        // Verify the trait actually has this method
        var resolvedTrait = DefinitionsCollection.OfType<TraitDefinition>()
            .FirstOrDefault(t => (t as IDefinition).TypePath == resolvedTraitPath);
        if (resolvedTrait?.GetMethod(methodName) == null) return null;

        // Capture expected trait generics from the parentPath (e.g., Add<Foo> → [Foo])
        ImmutableArray<LangPath> expectedTraitGenerics = [];
        if (parentPath is NormalLangPath nlpParentWithGens && nlpParentWithGens.GetFrontGenerics().Length > 0)
            expectedTraitGenerics = nlpParentWithGens.GetFrontGenerics();

        // Find the impl definition for this trait + concrete type (supports generic impls)
        ImplDefinition? impl = null;
        Dictionary<string, LangPath>? implBindings = null;
        foreach (var candidate in ImplDefinitions)
        {
            // Compare trait base paths (strip generics)
            var candidateTraitBase = candidate.TraitPath;
            ImmutableArray<LangPath> candidateTraitGenerics = [];
            if (candidateTraitBase is NormalLangPath nlpCandTrait && nlpCandTrait.GetFrontGenerics().Length > 0)
            {
                candidateTraitGenerics = nlpCandTrait.GetFrontGenerics();
                candidateTraitBase = nlpCandTrait.PopGenerics();
            }

            if (candidateTraitBase != resolvedTraitPath) continue;
            var bindings = candidate.TryMatchConcreteType(concreteType);
            if (bindings == null) continue;

            // Also verify trait generic args match (e.g., Add<Foo> vs Add<i32>)
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

        // Build a unique key that includes the impl's full trait path (with generics) for uniqueness
        var implMethodPath = new NormalLangPath(null,
            [new NormalLangPath.NormalPathSegment($"impl_{impl.TraitPath}_for_{concreteType}"),
             new NormalLangPath.NormalPathSegment(methodName)]);

        // Check if already created
        foreach (var scope in ScopeItems)
            if (scope.TryGetValue(implMethodPath, out var existing))
                return existing;

        // Get the method from the impl
        var implMethod = impl.GetMethod(methodName);
        if (implMethod == null) return null;

        // For generic impls, push the bindings so T resolves to the concrete type
        bool pushedImplScope = false;
        if (implBindings != null && implBindings.Count > 0)
        {
            AddScope();
            foreach (var (paramName, boundType) in implBindings)
            {
                var boundRefItem = GetRefItemFor(boundType);
                if (boundRefItem != null)
                    AddToDeepestScope(new NormalLangPath(null, [paramName]), boundRefItem);
            }
            pushedImplScope = true;
        }

        // Create the function ref
        var refItem2 = implMethod.CreateRefDefinition(this, []);

        if (pushedImplScope)
            PopScope();
        if (refItem2 is FunctionRefItem functionRefItem)
        {
            // Store impl generic bindings so Function.CodeGen can replay them
            if (implBindings != null && implBindings.Count > 0)
            {
                functionRefItem.Function.ImplGenericBindings = implBindings;
                functionRefItem.Function.ImplGenericParameters = impl.GenericParameters;
            }
            UnimplementedFunctions.Push(functionRefItem);
        }

        // Store under the concrete-type-specific key at outermost scope
        AddToScope(implMethodPath, refItem2, 0);
        return refItem2;
    }

    CodeGenContext(IEnumerable<IDefinition> definitions, NormalLangPath mainLangModule)
    {
        MainLangModule = mainLangModule;
        TopLevelDefinitions = definitions.ToList();
    }

    CodeGenContext(IEnumerable<ParseResult> results, NormalLangPath mainLangModule) : this(
        results.SelectMany(i => i.Items.OfType<IDefinition>())
            .Concat(results.SelectMany(i => i.Items.OfType<ImplDefinition>()
                .SelectMany(impl => impl.Methods))),
        mainLangModule)
    {
        // Collect impl definitions from all parse results
        foreach (var result in results)
            foreach (var impl in result.Items.OfType<ImplDefinition>())
                ImplDefinitions.Add(impl);
    }

    public NormalLangPath MainLangModule { get; }

    public LLVMBuilderRef Builder { get; set; }

    public LLVMModuleRef Module { get; private set; }
    // Dictionary to map custom BaseLangPath's to LLVMTypeRef's (if needed)


    public Dictionary<LangPath, StructTypeDefinition> TypeMap { get; } = new();


    private readonly Stack<List<IDefinition>> DefinitionsStack = new();

    public IEnumerable<IDefinition> DefinitionsCollection
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

    public int GetScope(IDefinition definition)
    {
        return 0;
    }


    public bool HasIdent(LangPath langPath)
    {
        foreach (var scope in ScopeItems)
            if (scope.TryGetValue(langPath, out var symbol))
                return true;

        return false;
    }

    public Dictionary<LangPath,object> ToMonomorphize {get; private set;} = new();


    public Stack<FunctionRefItem> UnimplementedFunctions = new();

    public 
    IRefItem? SetupIfPossible(LangPath ident)
    {

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
            }

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

    public void AddToOuterScope(LangPath symb, IRefItem refItem, int scopeOut = 1)
    {
        var it = ScopeItems.Skip(scopeOut);
        var first = ScopeItems.Skip(scopeOut).First();
        ScopeItems.Skip(scopeOut).First().Add(symb, refItem);
    }

    public void AddScope()
    {
        DefinitionsStack.Push(new List<IDefinition>());
        ScopeItems.Push(new Dictionary<LangPath, IRefItem>());
    }

    public void PopScope()
    {
        ScopeItems.Pop();
        DefinitionsStack.Pop();
    }


    public ValueRefItem GetVoid()
    {
        return Void;
    }

    public StructTypeDefinition GetLangType(LangPath lang)
    {
        return TypeMap[lang];
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

    void HandleOptimizations()
    {
        var passManager = LLVMPassManagerRef.Create();

        passManager.Run(Module);
    }
    public LLVMContextRef LLVMContext => Module.Context;
    private unsafe Func<int>? CodeGenInst(bool showLLVMIR = false, bool optimized = false)
    {
        const string MODULE_NAME = "LEGENDARY_LANGUAGE";
        Module = LLVM.ModuleCreateWithName(MODULE_NAME.ToCString());

      
        Builder = LLVM.CreateBuilderInContext(LLVMContext);
        


        AddScope();
        foreach (var i in TopLevelDefinitions)
        {
            AddToDeepestScope(i);
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


        var engine = Module.CreateExecutionEngine();


        var mainFnPath = mainDefRefItem.Function.FullPath;
        Console.WriteLine(mainFnPath);
        Console.WriteLine(engine == null);
        LLVMValueRef mainFnPtr = LLVM.GetNamedFunction(Module, mainFnPath.ToString().ToCString());

        if (mainFnPtr.Handle == IntPtr.Zero)
        {
            Console.WriteLine("main function not found!");
            return null;
        }


        var mainFunctionDelegate = () =>
        {
            var val = engine.RunFunction(mainFnPtr, []);
            var gotten = LLVM.GenericValueToInt(val, 1);
            var
                returned = // this cast is neeeded, because it is actually an int not a ulong. Using normal c# cast would change the bits, which would give us an incorrect value
                    Unsafe.As<ulong, int>(ref gotten);
            return returned;
        };
        return mainFunctionDelegate;
    }
}