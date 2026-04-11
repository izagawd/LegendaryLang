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
    private static readonly NormalLangPath PrimitiveModule = new(null, ["Std", "Primitive"]);

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
        ["TryCastPrimitive"] = PrimitiveModule,
    };

    public static bool IsIntrinsic(NormalLangPath module, string name)
    {
        if (!IntrinsicModules.TryGetValue(name, out var expectedModule)) return false;
        return module.Contains(expectedModule);
    }

    /// <summary>
    /// Wraps a raw LLVM pointer into a {ptr, metadata} struct for returning from an intrinsic.
    /// All pointer-like types are uniformly represented as {ptr, metadata} structs, so malloc/calloc
    /// results (raw i8* values) must be inserted into field 0 of the return struct.
    /// </summary>
    private static LLVMValueRef WrapRawPtrAsStruct(LLVMTypeRef structType, LLVMValueRef rawPtr, LLVMBuilderRef builder)
    {
        var zero = LLVMValueRef.CreateConstNull(structType);
        return builder.BuildInsertValue(zero, rawPtr, 0, "ptr_struct");
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
            "TryCastPrimitive" => EmitTryCastPrimitive(function, context),
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

    private static unsafe bool EmitAlloc(Function function, CodeGenContext context)
    {
        if (function.Arguments.Length != 2) return false;

        var sizeAlloca = function.Arguments[0].Alloca;
        var sizeVal = context.Builder.BuildLoad2(UsizeTypeDefinition.UsizeLLVMType, sizeAlloca, "size");

        var mallocFunc = GetOrDeclareMalloc(context);
        var ptr = context.Builder.BuildCall2(MallocFuncType, mallocFunc,
            new LLVMValueRef[] { sizeVal }, "heap_ptr");
        var returnTypeRef = LLVM.GetReturnType(function.FunctionType);
        context.Builder.BuildRet(WrapRawPtrAsStruct(returnTypeRef, ptr, context.Builder));
        return true;
    }

    private static bool EmitDealloc(Function function, CodeGenContext context)
    {
        if (function.Arguments.Length != 3) return false;

        var ptrArg = function.Arguments[0];
        var ptrType = ptrArg.Type as RawPtrType;
        if (ptrType == null) return false;

        var rawPtr = ptrType.ExtractDataPointer(context, new ValueRefItem
        {
            Type = ptrType, ValueRef = ptrArg.Alloca
        });

        var freeFunc = GetOrDeclareFree(context);
        context.Builder.BuildCall2(FreeFuncType, freeFunc, new LLVMValueRef[] { rawPtr });

        var voidVal = context.GetVoid();
        context.Builder.BuildRet(voidVal.LoadValue(context));
        return true;
    }

    private static unsafe bool EmitAllocZeroed(Function function, CodeGenContext context)
    {
        if (function.Arguments.Length != 2) return false;

        var sizeAlloca = function.Arguments[0].Alloca;
        var sizeVal = context.Builder.BuildLoad2(UsizeTypeDefinition.UsizeLLVMType, sizeAlloca, "size");

        var callocFunc = GetOrDeclareCalloc(context);
        var one = LLVMValueRef.CreateConstInt(UsizeTypeDefinition.UsizeLLVMType, 1, false);
        var ptr = context.Builder.BuildCall2(CallocFuncType, callocFunc,
            new LLVMValueRef[] { one, sizeVal }, "heap_ptr");
        var returnTypeRef = LLVM.GetReturnType(function.FunctionType);
        context.Builder.BuildRet(WrapRawPtrAsStruct(returnTypeRef, ptr, context.Builder));
        return true;
    }

    // ── Pointer intrinsics ──

    private static unsafe bool EmitPtrWrite(Function function, CodeGenContext context)
    {
        if (function.Arguments.Length != 2) return false;

        var dstArg = function.Arguments[0];
        var valArg = function.Arguments[1];
        var dstType = dstArg.Type as RawPtrType;
        if (dstType == null) return false;

        var rawPtr = dstType.ExtractDataPointer(context, new ValueRefItem
        {
            Type = dstType, ValueRef = dstArg.Alloca
        });

        var valType = valArg.Type;
        valType.AssignTo(context,
            new ValueRefItem { Type = valType, ValueRef = valArg.Alloca },
            new ValueRefItem { Type = valType, ValueRef = rawPtr });

        // Ownership transferred to heap destination — don't drop the parameter
        context.MarkDropFlagMoved(valArg.Name);

        var returnTypeRef = LLVM.GetReturnType(function.FunctionType);
        context.Builder.BuildRet(WrapRawPtrAsStruct(returnTypeRef, rawPtr, context.Builder));
        return true;
    }

    private static unsafe bool EmitPtrAsU8(Function function, CodeGenContext context)
    {
        if (function.Arguments.Length != 1) return false;

        var ptrArg = function.Arguments[0];
        var ptrType = ptrArg.Type as RawPtrType;
        if (ptrType == null) return false;

        var rawPtr = ptrType.ExtractDataPointer(context, new ValueRefItem
        {
            Type = ptrType, ValueRef = ptrArg.Alloca
        });

        var returnTypeRef = LLVM.GetReturnType(function.FunctionType);
        context.Builder.BuildRet(WrapRawPtrAsStruct(returnTypeRef, rawPtr, context.Builder));
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

        var rawPtr = ptrType.ExtractDataPointer(context, new ValueRefItem
        {
            Type = ptrType, ValueRef = ptrArg.Alloca
        });

        context.EmitDestruct(typePath, rawPtr);

        var voidVal = context.GetVoid();
        context.Builder.BuildRet(voidVal.LoadValue(context));
        return true;
    }

    // ── C runtime function declarations ──

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

    // ── Primitive conversion intrinsic ──

    /// <summary>
    /// TryCastPrimitive[From:! Sized +Primitive](To:! Sized +Primitive, input: From) -> Option(To)
    /// Converts between primitive numeric types with range checking.
    /// Returns Some(converted) if the value fits, None otherwise.
    /// GenericArguments[0] = From, GenericArguments[1] = To.
    /// </summary>
    private static unsafe bool EmitTryCastPrimitive(Function function, CodeGenContext context)
    {
        if (function.GenericArguments.Length != 2 || function.Arguments.Length != 1) return false;

        var fromType = (context.GetRefItemFor(function.GenericArguments[0]) as TypeRefItem)?.Type as PrimitiveType;
        var toType = (context.GetRefItemFor(function.GenericArguments[1]) as TypeRefItem)?.Type as PrimitiveType;
        if (fromType == null || toType == null) return false;

        var returnType = function.ReturnType as EnumType;
        if (returnType == null) return false;

        var inputArg = function.Arguments[0];
        var inputVal = fromType.LoadValue(context, new ValueRefItem { Type = fromType, ValueRef = inputArg.Alloca });

        var fromRef = fromType.TypeRef;
        var toRef = toType.TypeRef;
        var fromBits = fromRef.IntWidth;
        var toBits = toRef.IntWidth;
        bool fromSigned = fromType.Name == "i32";

        LLVMValueRef converted;
        LLVMValueRef fits;

        if (fromBits == toBits && fromType.Name == toType.Name)
        {
            converted = inputVal;
            fits = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1, false);
        }
        else
        {
            if (toBits > fromBits)
                converted = fromSigned
                    ? context.Builder.BuildSExt(inputVal, toRef)
                    : context.Builder.BuildZExt(inputVal, toRef);
            else if (toBits < fromBits)
                converted = context.Builder.BuildTrunc(inputVal, toRef);
            else
                converted = inputVal;

            bool toSigned = toType.Name == "i32";
            LLVMValueRef roundTrip;
            if (fromBits > toBits)
                roundTrip = toSigned
                    ? context.Builder.BuildSExt(converted, fromRef)
                    : context.Builder.BuildZExt(converted, fromRef);
            else if (fromBits < toBits)
                roundTrip = context.Builder.BuildTrunc(converted, fromRef);
            else
                roundTrip = converted;

            fits = context.Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, inputVal, roundTrip);

            if (fromSigned && !toSigned)
            {
                var nonNeg = context.Builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE,
                    inputVal, LLVMValueRef.CreateConstInt(fromRef, 0, false));
                fits = context.Builder.BuildAnd(fits, nonNeg);
            }
        }

        var resultAlloca = context.Builder.BuildAlloca(returnType.TypeRef);

        var parentFunc = context.Builder.InsertBlock.Parent;
        var someBB = parentFunc.AppendBasicBlock("trycast.some");
        var noneBB = parentFunc.AppendBasicBlock("trycast.none");
        var mergeBB = parentFunc.AppendBasicBlock("trycast.merge");

        context.Builder.BuildCondBr(fits, someBB, noneBB);

        context.Builder.PositionAtEnd(someBB);
        var someTagPtr = context.Builder.BuildStructGEP2(returnType.TypeRef, resultAlloca, 0);
        context.Builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0, false), someTagPtr);
        if (returnType.HasPayloads)
        {
            var payloadPtr = context.Builder.BuildStructGEP2(returnType.TypeRef, resultAlloca, 1);
            toType.AssignTo(context,
                new ValueRefItem { Type = toType, ValueRef = converted },
                new ValueRefItem { Type = toType, ValueRef = payloadPtr });
        }
        context.Builder.BuildBr(mergeBB);

        context.Builder.PositionAtEnd(noneBB);
        var noneTagPtr = context.Builder.BuildStructGEP2(returnType.TypeRef, resultAlloca, 0);
        context.Builder.BuildStore(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 1, false), noneTagPtr);
        context.Builder.BuildBr(mergeBB);

        context.Builder.PositionAtEnd(mergeBB);
        var loaded = returnType.LoadValue(context, new ValueRefItem { Type = returnType, ValueRef = resultAlloca });
        context.Builder.BuildRet(loaded);
        return true;
    }
}
