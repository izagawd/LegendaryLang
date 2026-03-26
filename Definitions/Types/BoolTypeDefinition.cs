using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions.Types;

public class BoolTypeDefinition : PrimitiveTypeDefinition
{
    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        
        return new TypeRefItem()
        {
            Type = new BoolType(this)
        };
    }

    public override string Name => "bool";

    public override Token Token => null;


    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path != (this as IDefinition).TypePath) return null;

        return [];
    }


    public override void Analyze(SemanticAnalyzer analyzer)
    {
    }
}