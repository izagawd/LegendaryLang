using System.Collections.Immutable;
using LegendaryLang.Parse;

namespace LegendaryLang.Definitions.Types;

public abstract class CustomTypeDefinition : TypeDefinition
{

    public abstract ImmutableArray<LangPath> ComposedTypes { get; }
}