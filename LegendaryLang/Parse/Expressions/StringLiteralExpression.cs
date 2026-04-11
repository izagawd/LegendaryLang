using System.Text;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

/// <summary>
/// A string literal expression like "hello".
/// Produces a Gc(str) — a GC-managed pointer to a heap-allocated UTF-8 byte array.
/// The Gc struct wraps a *mut str fat pointer: {data_ptr, length}.
/// </summary>
public class StringLiteralExpression : IExpression
{
    public string Value { get; }
    public Token Token { get; }
    public IEnumerable<ISyntaxNode> Children => [];
    public bool HasGuaranteedExplicitReturn => false;

    public StringLiteralExpression(StringLiteralToken token)
    {
        Value = token.Value;
        Token = token;
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Type is Gc(str) — a GC-managed pointer to the unsized str type
        var strPath = LangPath.PrimitivePath.Append("str");
        var gcPath = new NormalLangPath(null,
                new NormalLangPath.PathSegment[] { "Std", "Alloc", "Gc" })
            .AppendGenerics([strPath]);
        TypePath = gcPath;
    }

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var bytes = Encoding.UTF8.GetBytes(Value);

        // Create a global constant byte array for the string data
        var byteValues = bytes.Select(b =>
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int8, b, false)).ToArray();
        var constArray = LLVMValueRef.CreateConstArray(LLVMTypeRef.Int8, byteValues);

        var globalVar = codeGenContext.Module.AddGlobal(
            LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)bytes.Length),
            $"str.{_globalCounter++}");
        globalVar.Initializer = constArray;
        globalVar.IsGlobalConstant = true;
        globalVar.Linkage = LLVMLinkage.LLVMPrivateLinkage;

        // Allocate heap memory and copy string data there
        var sizeVal = LLVMValueRef.CreateConstInt(
            UsizeTypeDefinition.UsizeLLVMType, (ulong)bytes.Length, false);

        var mallocFunc = IntrinsicCodeGen.GetOrDeclareMalloc(codeGenContext);
        var heapPtr = codeGenContext.Builder.BuildCall2(
            IntrinsicCodeGen.MallocFuncType, mallocFunc,
            new LLVMValueRef[] { sizeVal }, "str_heap");

        var memcpyFunc = IntrinsicCodeGen.GetOrDeclareMemcpy(codeGenContext);
        codeGenContext.Builder.BuildCall2(
            IntrinsicCodeGen.MemcpyFuncType, memcpyFunc,
            new LLVMValueRef[] { heapPtr, globalVar, sizeVal }, "");

        // Get the Gc(str) struct type — it has one field: ptr (*mut str, a fat pointer)
        var gcTypeItem = codeGenContext.GetRefItemFor(TypePath!) as TypeRefItem;
        var gcStructType = gcTypeItem?.Type as StructType
            ?? throw new InvalidOperationException("Gc(str) should resolve to a StructType");

        var ptrFieldType = gcStructType.ResolvedFieldTypes!.Value[0] as PointerLikeType
            ?? throw new InvalidOperationException("Gc(str).ptr should be a PointerLikeType");

        // Build the *mut str fat pointer: {heap_ptr, length}
        var length = LLVMValueRef.CreateConstInt(
            StrTypeDefinition.MetadataLLVMType, (ulong)bytes.Length, false);
        var fatPtr = ptrFieldType.BuildFatPointer(codeGenContext, heapPtr, length);

        // Build the Gc(str) struct: store the fat pointer as field 0
        var gcAlloca = codeGenContext.Builder.BuildAlloca(gcStructType.TypeRef);
        var fieldPtr = codeGenContext.Builder.BuildStructGEP2(gcStructType.TypeRef, gcAlloca, 0);
        ptrFieldType.AssignTo(codeGenContext, fatPtr, new ValueRefItem
        {
            Type = ptrFieldType,
            ValueRef = fieldPtr
        });

        return new ValueRefItem
        {
            Type = gcStructType,
            ValueRef = gcAlloca
        };
    }

    public LangPath? TypePath { get; private set; }
    public bool IsTemporary => true; // fresh string

    public static StringLiteralExpression Parse(Parser parser)
    {
        var token = (StringLiteralToken)parser.Pop();
        return new StringLiteralExpression(token);
    }

    private static int _globalCounter;
}
