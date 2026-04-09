using System.Collections.Immutable;
using LegendaryLang.Parse;

namespace LegendaryLang.Definitions.Types;

public abstract class ComposableTypeDefinition : TypeDefinition
{
    public abstract ImmutableArray<LangPath> ComposedTypes { get; }
    public abstract ImmutableArray<string> LifetimeParameters { get; }
}