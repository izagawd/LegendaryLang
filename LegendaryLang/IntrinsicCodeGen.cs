using LegendaryLang.Definitions.Types;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions;
using LegendaryLang.Parse;
using LLVMSharp.Interop;

namespace LegendaryLang;

/// <summary>
/// Handles codegen for compiler intrinsic functions.
/// Intrinsics are defined in std with placeholder bodies — the compiler
/// replaces the body with specialized LLVM IR during codegen.
/// </summary>
public static class IntrinsicCodeGen
{
    private static readonly NormalLangPath AllocModule = new(null, ["Std", "Alloc"]);
    private static readonly NormalLangPath MemModule = new(null, ["Std", "Mem"]);
    private static readonly NormalLangPath PtrModule = new(null, ["Std", "Ptr"]);

    private static readonly Dictionary<string, NormalLangPath> IntrinsicModules = new()
    {
        ["SizeOf"] = MemModule,
        ["AlignOf"] = MemModule,
        ["Alloc"] = AllocModule,
        ["Dealloc"] = AllocModule,
        ["AllocZeroed"] = AllocModule,
        ["PtrWrite"] = PtrModule,
        ["PtrAsU8"] = PtrModule,
        ["DestructPtr"] = PtrModule,
    };

    public static bool IsIntrinsic(NormalLangPath module, string name)
    {
        if (!IntrinsicModules.TryGetValue(name, out var expectedModule)) return false;
        return module.Contains(expectedModule);
    }

    public static bool TryEmit(Function function, CodeGenContext context)
    {
        var defPath = function.Definition.TypePath;
        if (defPath is not NormalLangPath nlp) return false;
        if (!IntrinsicModules.TryGetValue(function.Definition.Name, out var expectedModule)) return false;
        if (!nlp.Contains(expectedModule)) return false;

        return function.Definition.Name switch
        {
            "SizeOf" => EmitSizeOf(function, context),
            "AlignOf" => EmitAlignOf(function, context),
            "Alloc" => EmitAlloc(function, context),
            "Dealloc" => EmitDealloc(function, context),
            "AllocZeroed" => EmitAllocZeroed(function, context),
            "PtrWrite" => EmitPtrWrite(function, context),
            "PtrAsU8" => EmitPtrAsU8(function, context),
            "DestructPtr" => EmitDestructPtr(function, context),
            _ => false
        };
    }
    // ── Type query intrinsics ──

    private static bool EmitSizeOf(Function function, CodeGenContext context)
    {
        if (function.GenericArguments.Length != 1) return false;
        var typeRef = (context.GetRefItemFor(function.GenericArguments[0]) as TypeRefItem)?.Type;
        if (typeRef == null) return false;
        context.Builder.BuildRet(typeRef.TypeRef.SizeOf);
        return true;
    }

    private static bool EmitAlignOf(Function function, CodeGenContext context)
    {
        if (function.GenericArguments.Length != 1) return false;
        var typeRef = (context.GetRefItemFor(function.GenericArguments[0]) as TypeRefItem)?.Type;
        if (typeRef == null) return false;
        context.Builder.BuildRet(typeRef.TypeRef.AlignOf);
        return true;
    }

    // ── Allocation intrinsics ──

    private static bool EmitAlloc(Function function, CodeGenContext context)
    {
        if (function.Arguments.Length != 2) return false;

        var sizeAlloca = function.Arguments[0].Alloca;
        var sizeVal = context.Builder.BuildLoad2(UsizeTypeDefinition.UsizeLLVMType, sizeAlloca, "size");

        var mallocFunc = GetOrDeclareMalloc(context);
        var ptr = context.Builder.BuildCall2(MallocFuncType, mallocFunc,
            new LLVMValueRef[] { sizeVal }, "heap_ptr");
        context.Builder.BuildRet(ptr);
        return true;
    }

    private static bool EmitDealloc(Function function, CodeGenContext context)
    {
        if (function.Arguments.Length != 3) return false;

        var ptrArg = function.Arguments[0];
        var ptrType = ptrArg.Type as RawPtrType;
        if (ptrType == null) return false;

        var rawPtr = ptrType.LoadValue(context, new ValueRefItem
        {
            Type = ptrType, ValueRef = ptrArg.Alloca
        });

        var freeFunc = GetOrDeclareFree(context);
        context.Builder.BuildCall2(FreeFuncType, freeFunc, new LLVMValueRef[] { rawPtr });

        var voidVal = context.GetVoid();
        context.Builder.BuildRet(voidVal.LoadValue(context));
        return true;
    }

    private static bool EmitAllocZeroed(Function function, CodeGenContext context)
    {
        if (function.Arguments.Length != 2) return false;

        var sizeAlloca = function.Arguments[0].Alloca;
        var sizeVal = context.Builder.BuildLoad2(UsizeTypeDefinition.UsizeLLVMType, sizeAlloca, "size");

        var callocFunc = GetOrDeclareCalloc(context);
        var one = LLVMValueRef.CreateConstInt(UsizeTypeDefinition.UsizeLLVMType, 1, false);
        var ptr = context.Builder.BuildCall2(CallocFuncType, callocFunc,
            new LLVMValueRef[] { one, sizeVal }, "heap_ptr");

        context.Builder.BuildRet(ptr);
        return true;
    }

    // ── Pointer intrinsics ──

    private static bool EmitPtrWrite(Function function, CodeGenContext context)
    {
        if (function.Arguments.Length != 2) return false;

        var dstArg = function.Arguments[0];
        var valArg = function.Arguments[1];
        var dstType = dstArg.Type as RawPtrType;
        if (dstType == null) return false;

        var rawPtr = dstType.LoadValue(context, new ValueRefItem
        {
            Type = dstType, ValueRef = dstArg.Alloca
        });

        var valType = valArg.Type;
        valType.AssignTo(context, new ValueRefItem
            {
                Type = valType,
                ValueRef = valArg.Alloca
            },
            new ValueRefItem
            {
                Type = valType,
                ValueRef = rawPtr
            });

        // Ownership transferred to heap destination — don't drop the parameter
        context.MarkDropFlagMoved(valArg.Name);

        context.Builder.BuildRet(rawPtr);
        return true;
    }

    private static bool EmitPtrAsU8(Function function, CodeGenContext context)
    {
        if (function.Arguments.Length != 1) return false;

        var ptrArg = function.Arguments[0];
        var ptrType = ptrArg.Type as RawPtrType;
        if (ptrType == null) return false;

        var rawPtr = ptrType.LoadValue(context, new ValueRefItem
        {
            Type = ptrType, ValueRef = ptrArg.Alloca
        });

        context.Builder.BuildRet(rawPtr);
        return true;
    }

    /// <summary>
    /// DestructPtr(*uniq T): destructs the value at the pointer — calls Drop (if T implements it)
    /// and recursively drops T's fields. Does NOT free memory.
    /// Used by Box.Drop to destruct the heap-allocated T before calling Dealloc.
    /// </summary>
    private static bool EmitDestructPtr(Function function, CodeGenContext context)
    {
        if (function.Arguments.Length != 1) return false;

        var ptrArg = function.Arguments[0];
        var ptrType = ptrArg.Type as RawPtrType;
        if (ptrType == null) return false;

        var typePath = ptrType.PointingToType.TypePath;

        // Load the raw pointer from the argument alloca
        var rawPtr = ptrType.LoadValue(context, new ValueRefItem
        {
            Type = ptrType, ValueRef = ptrArg.Alloca
        });

        // Destruct the value at the pointer: Drop call + recursive field drops
        context.EmitDestruct(typePath, rawPtr);

        var voidVal = context.GetVoid();
        context.Builder.BuildRet(voidVal.LoadValue(context));
        return true;
    }

    // ── C runtime function declarations ──
    // Explicitly declared in the LLVM module so we can map them via AddGlobalMapping.

    private static readonly LLVMTypeRef PtrType = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);

    private static readonly LLVMTypeRef MallocFuncType =
        LLVMTypeRef.CreateFunction(PtrType, [UsizeTypeDefinition.UsizeLLVMType]);

    private static readonly LLVMTypeRef FreeFuncType =
        LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [PtrType]);

    private static readonly LLVMTypeRef CallocFuncType =
        LLVMTypeRef.CreateFunction(PtrType, [UsizeTypeDefinition.UsizeLLVMType, UsizeTypeDefinition.UsizeLLVMType]);

    public static unsafe LLVMValueRef GetOrDeclareMalloc(CodeGenContext context)
    {
        LLVMValueRef existing = LLVM.GetNamedFunction(context.Module, "malloc".ToCString());
        if (existing.Handle != IntPtr.Zero) return existing;
        return context.Module.AddFunction("malloc", MallocFuncType);
    }

    public static unsafe LLVMValueRef GetOrDeclareFree(CodeGenContext context)
    {
        LLVMValueRef existing = LLVM.GetNamedFunction(context.Module, "free".ToCString());
        if (existing.Handle != IntPtr.Zero) return existing;
        return context.Module.AddFunction("free", FreeFuncType);
    }

    public static unsafe LLVMValueRef GetOrDeclareCalloc(CodeGenContext context)
    {
        LLVMValueRef existing = LLVM.GetNamedFunction(context.Module, "calloc".ToCString());
        if (existing.Handle != IntPtr.Zero) return existing;
        return context.Module.AddFunction("calloc", CallocFuncType);
    }
}
