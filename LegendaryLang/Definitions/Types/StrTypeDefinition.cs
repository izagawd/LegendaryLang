using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Definitions.Types;

/// <summary>
/// The str type — a primitive unsized type representing a UTF-8 byte sequence.
/// str can never exist on the stack; it only exists behind pointers.
/// References to str (&amp;str) are fat pointers: {data_ptr, length}.
/// </summary>
public class StrTypeDefinition : TypeDefinition
{
    public override string Name => "str";
    public override NormalLangPath Module => LangPath.PrimitivePath;
    public override Token Token => null;
    public override bool IsUnsized => true;

    /// <summary>
    /// The LLVM element type for str's underlying data (i8 — bytes).
    /// Used when accessing individual bytes through the data pointer.
    /// </summary>
    public static LLVMTypeRef ElementLLVMType => LLVMTypeRef.Int8;

    /// <summary>
    /// The metadata type for str fat pointers (usize — byte length).
    /// </summary>
    public static LLVMTypeRef MetadataLLVMType => UsizeTypeDefinition.UsizeLLVMType;

    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        // str has no concrete LLVM representation — it's unsized.
        // Use an opaque marker so the type system can reference it,
        // but it can never be instantiated directly.
        return new TypeRefItem
        {
            Type = new UnsizedType(this, ElementLLVMType, MetadataLLVMType)
        };
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path) => [];
    public override void Analyze(SemanticAnalyzer analyzer) { }
}
