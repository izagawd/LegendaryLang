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
    /// Target pointer size in bits. Set by the compiler based on the target architecture.
    /// Defaults to 64. Used to determine usize width and pointer sizes.
    /// </summary>
    public static int TargetPointerBits { get; set; } = 64;

    /// <summary>
    /// The LLVM integer type matching target pointer size. Referenced directly by intrinsics.
    /// </summary>
    public static LLVMTypeRef UsizeLLVMType => TargetPointerBits == 64 ? LLVMTypeRef.Int64 : LLVMTypeRef.Int32;

    public override string Name => "usize";
    protected override LLVMTypeRef LLVMType => UsizeLLVMType;
}
