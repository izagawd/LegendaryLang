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
    private static readonly NormalLangPath AllocModule = new(null, ["Std", "Core", "Alloc"]);

    private static readonly HashSet<string> IntrinsicNames =
        ["SizeOf", "AlignOf", "Alloc", "Dealloc", "AllocZeroed", "PtrWrite", "PtrAsU8"];

    public static bool IsIntrinsic(NormalLangPath module, string name)
    {
        if (!IntrinsicNames.Contains(name)) return false;
        return module.Contains(AllocModule);
    }

    public static bool TryEmit(Function function, CodeGenContext context)
    {
        var defPath = function.Definition.TypePath;
        if (defPath is not NormalLangPath nlp) return false;
        if (!nlp.Contains(AllocModule)) return false;

        return function.Definition.Name switch
        {
            "SizeOf" => EmitSizeOf(function, context),
            "AlignOf" => EmitAlignOf(function, context),
            "Alloc" => EmitAlloc(function, context),
            "Dealloc" => EmitDealloc(function, context),
            "AllocZeroed" => EmitAllocZeroed(function, context),
            "PtrWrite" => EmitPtrWrite(function, context),
            "PtrAsU8" => EmitPtrAsU8(function, context),
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
        var sizeVal = context.Builder.BuildLoad2(UsizeType.LLVMType, sizeAlloca, "size");

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
        var sizeVal = context.Builder.BuildLoad2(UsizeType.LLVMType, sizeAlloca, "size");

        var callocFunc = GetOrDeclareCalloc(context);
        var one = LLVMValueRef.CreateConstInt(UsizeType.LLVMType, 1, false);
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

    // ── C runtime function declarations ──
    // Explicitly declared in the LLVM module so we can map them via AddGlobalMapping.

    private static readonly LLVMTypeRef PtrType = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);

    private static readonly LLVMTypeRef MallocFuncType =
        LLVMTypeRef.CreateFunction(PtrType, [UsizeType.LLVMType]);

    private static readonly LLVMTypeRef FreeFuncType =
        LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [PtrType]);

    private static readonly LLVMTypeRef CallocFuncType =
        LLVMTypeRef.CreateFunction(PtrType, [UsizeType.LLVMType, UsizeType.LLVMType]);

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
