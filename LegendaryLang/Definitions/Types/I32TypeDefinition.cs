using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Definitions.Types;

public abstract class PrimitiveTypeDefinition : TypeDefinition
{
    public override NormalLangPath Module => LangPath.PrimitivePath;
    public override Token Token => null;

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path != (this as IDefinition).TypePath) return null;
        return [];
    }

    public override void Analyze(SemanticAnalyzer analyzer) { }

    /// <summary>
    /// The LLVM type ref for this primitive. Subclasses just provide this.
    /// </summary>
    protected abstract LLVMTypeRef LLVMType { get; }

    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        return new TypeRefItem { Type = new PrimitiveType(this, LLVMType, Name) };
    }
}

public class I32TypeDefinition : PrimitiveTypeDefinition
{
    public override string Name => "i32";
    protected override LLVMTypeRef LLVMType => LLVMTypeRef.Int32;
}

public class BoolTypeDefinition : PrimitiveTypeDefinition
{
    public override string Name => "bool";
    protected override LLVMTypeRef LLVMType => LLVMTypeRef.Int1;
}

public class U8TypeDefinition : PrimitiveTypeDefinition
{
    public override string Name => "u8";
    protected override LLVMTypeRef LLVMType => LLVMTypeRef.Int8;
}

public class UsizeTypeDefinition : PrimitiveTypeDefinition
{
    /// <summary>
    /// The LLVM integer type matching pointer size. Referenced directly by intrinsics.
    /// </summary>
    public static readonly LLVMTypeRef UsizeLLVMType =
        Environment.Is64BitProcess ? LLVMTypeRef.Int64 : LLVMTypeRef.Int32;

    public override string Name => "usize";
    protected override LLVMTypeRef LLVMType => UsizeLLVMType;
}
