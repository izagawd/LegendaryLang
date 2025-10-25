using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
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
    CodeGenContext(IEnumerable<IDefinition> definitions, NormalLangPath mainLangModule)
    {
        MainLangModule = mainLangModule;
        TopLevelDefinitions = definitions.ToList();
    }

    CodeGenContext(IEnumerable<ParseResult> results, NormalLangPath mainLangModule) : this(
        results.SelectMany(i => i.Items.OfType<IDefinition>()), mainLangModule)
    {
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
        ScopeItems.Peek().Add(symb, refItem);
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