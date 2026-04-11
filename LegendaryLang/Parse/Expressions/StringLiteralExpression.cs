using System.Text;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

/// <summary>
/// A string literal expression like "hello".
/// Produces a &amp;'static const str — a fat pointer to a global constant byte array.
/// The fat pointer contains {data_ptr, length}.
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
        // Type is &str — a fat reference to the unsized str type
        var strPath = LangPath.PrimitivePath.Append("str");
        var refPath = RefTypeDefinition.GetRefModule()
            .Append(RefTypeDefinition.GetRefName(RefKind.Shared))
            .AppendGenerics([strPath]);
        TypePath = refPath;
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

        // Get the &str fat pointer type
        var refTypeItem = codeGenContext.GetRefItemFor(TypePath!) as TypeRefItem;
        if (refTypeItem?.Type is not PointerLikeType fatPtrType || !fatPtrType.HasNonTrivialMetadata)
            throw new InvalidOperationException("&str should be a fat pointer type");

        // Build fat pointer: {data_ptr, length}
        var dataPtr = globalVar;
        var length = LLVMValueRef.CreateConstInt(
            StrTypeDefinition.MetadataLLVMType, (ulong)bytes.Length, false);

        return fatPtrType.BuildFatPointer(codeGenContext, dataPtr, length);
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
