using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions.Types;

public class BoolTypeDefinition : PrimitiveTypeDefinition
{
    public override string Name => "bool";

    public override Token Token => null;

    public override Type GenerateIncompleteMono(CodeGenContext context, LangPath langPath)
    {
        return new BoolType(this);
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path != (this as IDefinition).FullPath) return null;

        return [];
    }


    public override void Analyze(SemanticAnalyzer analyzer)
    {
    }
}