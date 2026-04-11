using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Definitions.Types;

/// <summary>
/// Type definition for fixed-size arrays [T; N].
/// </summary>
public class ArrayTypeDefinition : ComposableTypeDefinition
{
    public LangPath ElementType { get; }
    public ulong Size { get; }

    public ArrayTypeDefinition(LangPath elementType, ulong size)
    {
        ElementType = elementType;
        Size = size;
    }

    public override LangPath TypePath => new ArrayLangPath(ElementType,
        new NormalLangPath(null, [Size.ToString()]));
    public override string Name => $"[{ElementType}; {Size}]";
    public override NormalLangPath Module { get; } = new(null, []);
    public override Token Token => null!;

    public override ImmutableArray<LangPath> ComposedTypes
    {
        get
        {
            var types = new LangPath[Size];
            for (ulong i = 0; i < Size; i++)
                types[i] = ElementType;
            return types.ToImmutableArray();
        }
    }

    public override ImmutableArray<string> LifetimeParameters => [];

    public override void ResolvePaths(PathResolver resolver) { }

    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        var elemPath = genericArguments[0].Monomorphize(context);
        var sizePath = genericArguments[1];

        var elemRefItem = (TypeRefItem)context.GetRefItemFor(elemPath)!;
        var elemType = elemRefItem.Type;

        var concretePath = new ArrayLangPath(elemPath, sizePath);
        var llvmArrayType = LLVMTypeRef.CreateArray(elemType.TypeRef, (uint)Size);

        return new TypeRefItem
        {
            Type = new ArrayType(this, elemType, (uint)Size, llvmArrayType, concretePath)
        };
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path is ArrayLangPath arrPath)
            return [arrPath.ElementType, arrPath.Size];
        return null;
    }

    public override void Analyze(SemanticAnalyzer analyzer) { }
}
