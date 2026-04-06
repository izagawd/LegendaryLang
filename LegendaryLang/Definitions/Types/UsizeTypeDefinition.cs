using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;

namespace LegendaryLang.Definitions.Types;

public class UsizeTypeDefinition : PrimitiveTypeDefinition
{
    public override Token Token { get; }
    public override string Name => "usize";

    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        return new TypeRefItem()
        {
            Type = new UsizeType(this),
        };
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path != (this as IDefinition).TypePath) return null;
        return [];
    }

    public override void Analyze(SemanticAnalyzer analyzer) { }
}
