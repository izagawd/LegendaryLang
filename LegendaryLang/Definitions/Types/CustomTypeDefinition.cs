using System.Collections.Immutable;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public abstract class CustomTypeDefinition : TypeDefinition
{

    public abstract ImmutableArray<LangPath> ComposedTypes { get; }
}