using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;

namespace LegendaryLang.Definitions.Types;

public class BoolTypeDefinition : PrimitiveTypeDefinition
{
    public override ConcreteDefinition.Type GenerateIncompleteMono(CodeGenContext context, LangPath langPath)
    {
        return new BoolType(this);
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path != (this as IDefinition).FullPath)
        {
            return null;
        }

        return [];
    }

    public override string Name => "bool";

    public override Token LookUpToken => null;


    public override void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }
}