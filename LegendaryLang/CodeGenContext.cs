﻿using System.Runtime.CompilerServices;
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
public abstract class IRefItem
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
    public CodeGenContext(IEnumerable<IDefinition> definitions, NormalLangPath mainLangModule)
    {
        MainLangModule = mainLangModule;
        TopLevelDefinitions = definitions.ToList();
    }

    public CodeGenContext(IEnumerable<ParseResult> results, NormalLangPath mainLangModule) : this(
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

    public void CodeGen(IConcreteDefinition definition)
    {
        if (!definition.HasBeenGened) definition.CodeGen(this);
        definition.HasBeenGened = true;
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

    public IRefItem? GetRefItemFor(LangPath ident, bool monomorphizePath = true)
    {
        if (monomorphizePath) ident = ident.Monomorphize(this) ?? ident;

        foreach (var scope in ScopeItems)
            if (scope.TryGetValue(ident, out var symbol))
                return symbol;

        {
            var first = DefinitionsCollection.OfType<IMonomorphizable>().FirstOrDefault(i =>
            {
                var gotten = i.GetGenericArguments(ident);
                return i.GetGenericArguments(ident) is not null;
            });
            // generate and store the type if not already, and it is defined
            if (first != null)
            {
                var monod = first.Monomorphize(this, ident);
                if (monod is Function function)
                    AddToScope(ident, new FunctionRefItem
                    {
                        Function = function
                    }, GetScope(function.Definition));
                else if (monod is Type type)
                    AddToScope(ident, new TypeRefItem
                    {
                        Type = type
                    }, GetScope(type.TypeDefinition));

                // codegen may change the insert block. this is to preserve and set it back
                var prevBuilder = Builder.InsertBlock;
                monod.CodeGen(this);
                unsafe
                {
                    LLVM.PositionBuilderAtEnd(Builder, prevBuilder);
                }

                return GetRefItemFor(ident);
            }
        }

        {
            if (ident is TupleLangPath tuplePath)
            {
                var types = new List<Type>();
                foreach (var i in tuplePath.TypePaths)
                {
                    var type = GetRefItemFor(i) as TypeRefItem;
                    if (type != null) types.Add(type.Type);

                    return null;
                }

                var tuple = new TupleType(types);
                AddToScope(ident, new TypeRefItem
                {
                    Type = tuple
                }, 0);
                // codegen may change the insert block. this is to preserve and set it back
                var prevBuilder = Builder.InsertBlock;
                CodeGen(tuple);
                unsafe
                {
                    LLVM.PositionBuilderAtEnd(Builder, prevBuilder);
                }

                return GetRefItemFor(ident);
            }

            return null;
        }
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
        foreach (var i in ScopeItems.Reverse())
        {
            if (i.TryGetValue(path, out var symbol)) return scope;
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
        var emptyTuple = new TupleType([]);
        CodeGen(emptyTuple);


        Void = new ValueRefItem
        {
            ValueRef = null,
            Type = emptyTuple
        };
    }


    public unsafe Func<int>? CodeGen(bool showLLVMIR = false, bool optimized = false)
    {
        const string MODULE_NAME = "LEGENDARY_LANGUAGE";
        Module = LLVM.ModuleCreateWithName(MODULE_NAME.ToCString());

        var Context = LLVM.ContextCreate();
        Builder = LLVM.CreateBuilderInContext(Context);


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

        var mainConc = mainDef.Monomorphize(this, new NormalLangPath(null, [..MainLangModule, "main"]));
        AddToDeepestScope(new NormalLangPath(null, [..MainLangModule, "main"]), new FunctionRefItem
        {
            Function = mainConc
        });

        mainConc.CodeGen(this);
        if (optimized)
        {
            var passManager = LLVM.CreatePassManager();

            LLVM.AddFunctionInliningPass(passManager);


            LLVM.AddDeadStoreEliminationPass(passManager);

            LLVM.AddInstructionCombiningPass(passManager);
            LLVM.AddReassociatePass(passManager);
            LLVM.AddGVNPass(passManager);
            LLVM.AddCFGSimplificationPass(passManager);


            LLVM.AddDeadStoreEliminationPass(passManager);
            LLVM.RunPassManager(passManager, Module);
        }

        if (showLLVMIR) Console.WriteLine(FromByte(LLVM.PrintModuleToString(Module)));

        sbyte* idk;
        if (LLVM.VerifyModule(Module, LLVMVerifierFailureAction.LLVMPrintMessageAction, &idk) != 0)
        {
            var errMsg = Marshal.PtrToStringAnsi((IntPtr)idk);
            Console.WriteLine("LLVM Module Verification Failed:\n" + errMsg);
            return null;
        }


        var engine = Module.CreateExecutionEngine();


        var mainFnPath = (mainConc as IDefinition).FullPath;
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