using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;

namespace LegendaryLang.Definitions.Types;

public class U8TypeDefinition : PrimitiveTypeDefinition
{
    public override Token Token { get; }
    public override string Name => "u8";

    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        return new TypeRefItem()
        {
            Type = new U8Type(this),
        };
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path != (this as IDefinition).TypePath) return null;
        return [];
    }

    public override void Analyze(SemanticAnalyzer analyzer) { }
}
