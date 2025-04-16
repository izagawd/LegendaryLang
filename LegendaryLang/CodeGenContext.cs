using System.Reflection;
using System.Runtime.InteropServices;
using LegendaryLang.Parse;

using LegendaryLang.Parse.Types;
using LLVMSharp.Interop;
using Type = LegendaryLang.Parse.Types.Type;

namespace LegendaryLang;


public static class Extensions
{
    public unsafe static sbyte* ToCString(this string str)
    {
        return (sbyte*)Marshal.StringToHGlobalAnsi(str).ToPointer();
    }
}
/// <summary>
/// Base type that represents either the type, or variable reference of a path
/// like how in c#, if u have a type Foo in namespace bar it will be in
/// Bar.Foo. that wouldbe the type. if Foo has a static variable Dog, it will be in
/// Bar.Foo.Dog. that would be a variable ref
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
    public required  Function Function { get; init; }
}
public class TypeRefItem : IRefItem, IHasType
{
    public  LLVMTypeRef TypeRef
    {
        get => Type.TypeRef;
  
    }

    public required Type Type {get; init;}
}
/// <summary>
/// NOTE: DataRefItems that are not primitive 9eg structs, tuples) having a classification RValue doesnt mean its valueref is a value.
/// for structs, it would be a pointer since
/// LLVM doesnt have much freedom with structs as values. Its not set as an LValue because
/// from the programming language perspective, the developer would see it as an LValue
///
/// If its a struct that is an R
/// </summary>
public enum ValueClassification
{
    /// intended to represent stored in memory. doesnt matter if its in the stack or heap
    LValue,
    ///intented just a value, not stored in memory. probably is stored in a register
    RValue
}

public class VariableRefItem : IRefItem, IHasType
{
    public required Type Type {get; init;}
    public  required LLVMValueRef ValueRef {get; init; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns>Pointer to stack allocation</returns>
    public LLVMValueRef StackAllocate(CodeGenContext context)
    {
       return Type.AssignToStack(context,this);
    }
    public LLVMValueRef LoadValForRetOrArg(CodeGenContext context)
    {
        return Type.LoadValueForRetOrArg(context,this);
    }
}

public struct Symbol
{
    public LLVMValueRef Value;
    public LLVMTypeRef Type;
}
public class CodeGenContext
{
    public void CodeGen(IDefinition definition)
    {
        if (!definition.HasBeenGened)
        {
            definition.CodeGen(this);
        }
        definition.HasBeenGened = true;
    }
    private Stack<Dictionary<LangPath, IRefItem>> ScopeItems = new();


    public IRefItem? GetRefItemFor(LangPath ident)
    {
        foreach (var scope in ScopeItems)
        {
            if (scope.TryGetValue(ident, out var symbol))
            {

                return symbol;
                
             
            }
        }
        var first = definitionsList.OfType<Type>().FirstOrDefault(i => i.Ident == ident);
        // generate and store the type if not already, and it is defined
        if (first != null)
        {
            CodeGen(first);
            return GetRefItemFor(ident);
        }

        
        // if its tuple, first ensure all its types are stored
        if (ident is TupleLangPath tuplePath)
        {
            var types = new List<Type>();
            foreach (var i in tuplePath.TypePaths)
            {
                var type = GetRefItemFor(i) as TypeRefItem;
                if (type != null)
                {
                    types.Add(type.Type);
                }

                return null;


            }
            var tuple = new TupleType(types);
            CodeGen(tuple);
            return GetRefItemFor(ident);
        }

        return null;
    }


    public void AddToDeepestScope(LangPath symb, IRefItem refItem)
    {
        ScopeItems.Peek().Add(symb, refItem);
    }


    public void  AddScope()
    {

        ScopeItems.Push(new());
 
    }

    public Dictionary<LangPath, IRefItem> PopScope()
    {
        return ScopeItems.Pop();

    }


    public VariableRefItem GetVoid()
    {
        return Void;
    }

    private VariableRefItem Void;

    public LLVMBuilderRef Builder { get; set; }  
    public LLVMModuleRef Module { get; private set; }
    // Dictionary to map custom BaseLangPath's to LLVMTypeRef's (if needed)

    
    public Dictionary<LangPath, Struct> TypeMap { get; } = new();

    public Struct GetLangType(LangPath lang)
    {
        return TypeMap[lang];
    }

    public unsafe static string? FromByte(sbyte* value)
    {
       
            return Marshal.PtrToStringAnsi((IntPtr)value);
        

      
    }

    private unsafe void SetupVoid()
    {
        var emptyTuple = new TupleType([]);
        CodeGen(emptyTuple);

       
        Void = new VariableRefItem
        {
            ValueRef = null,
            Type =emptyTuple
        };
    }
    public List<IDefinition> definitionsList { get; } = new List<IDefinition>();
    public CodeGenContext(IEnumerable<IDefinition> definitions)
    {
        definitionsList = definitions.ToList();
      
        

    }

    public void CodeGen()
    {
        const string MODULE_NAME = "LEGENDARY_LANGUAGE";
        unsafe
        {
            Module = LLVM.ModuleCreateWithName(MODULE_NAME.ToCString());

        }

        unsafe
        {

            var Context = LLVM.ContextCreate();
            Builder = LLVM.CreateBuilderInContext(Context);





            AddScope();


            SetupVoid();


            foreach (var def in definitionsList.OrderBy(i => i.Priority))
            {
                CodeGen(def); // Generate LLVM IR for functions, etc.
            }

            sbyte* idk;
            if (LLVM.VerifyModule(Module, LLVMVerifierFailureAction.LLVMPrintMessageAction, &idk) != 0)
            {
                var errMsg = Marshal.PtrToStringAnsi((IntPtr)idk);
                Console.WriteLine("LLVM Module Verification Failed:\n" + errMsg);
                return;
            }

            Console.WriteLine(FromByte(LLVM.PrintModuleToString(Module)));

            LLVMExecutionEngineRef engine;
            sbyte* error;

            // Create a pointer to the engine ref
            LLVMExecutionEngineRef* enginePtr = &engine;
            if (LLVM.CreateExecutionEngineForModule((LLVMOpaqueExecutionEngine**)enginePtr, Module, &idk) != 0)
            {
                var errMsg = FromByte(idk);
                Console.WriteLine("Execution engine creation failed: " + errMsg);
                return;
            }

            Console.WriteLine(engine == null);
            LLVMValueRef mainFn = LLVM.GetNamedFunction(Module, "main".ToCString());

            if (mainFn.Handle == IntPtr.Zero)
            {
                Console.WriteLine("main function not found!");
                return;
            }

            var val = engine.RunFunction(LLVM.GetNamedFunction(Module, "main".ToCString()), []);

            Console.WriteLine(LLVM.GenericValueToInt(val, 0));
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int MainDelegate();
    public CodeGenContext( IEnumerable<ParseResult> results) : this(results.SelectMany(i => i.Definitions))
    {
    
    }


}
