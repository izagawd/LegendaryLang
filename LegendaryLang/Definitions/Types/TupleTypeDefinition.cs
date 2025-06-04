using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions.Types;

public class TupleTypeDefinition : CustomTypeDefinition
{
    public TupleTypeDefinition(IEnumerable<Type> otherTypes)
    {
        OtherTypes = otherTypes.ToImmutableArray();
    }

    public override string Name => $"({string.Join(',', OtherTypes.Select(i => i.TypePath))})";
    public override NormalLangPath Module { get; } = new(null, []);

    public override LangPath TypePath => new TupleLangPath(OtherTypes.Select(i => i.TypePath));
    public override Token Token { get; }
    public ImmutableArray<Type> OtherTypes { get; }

    public ImmutableArray<GenericParameter> GenericParameters
    {
        get
        {
            var genericParameters = new List<GenericParameter>();
            for (var i = 0; i < OtherTypes.Length; i++) genericParameters.Add(new GenericParameter(i.ToString()));
            return genericParameters.ToImmutableArray();
        }
    }


    public override ImmutableArray<LangPath> ComposedTypes => OtherTypes.Select(i => i.TypePath).ToImmutableArray();

    public override void ResolvePaths(PathResolver resolver)
    {
        foreach (var i in OtherTypes) i.ResolvePaths(resolver);
    }

    public override Type GenerateIncompleteMono(CodeGenContext context, LangPath langPath)
    {
        return new TupleType(OtherTypes);
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        throw new NotImplementedException();
    }

    public override void Analyze(SemanticAnalyzer analyzer)
    {
    }
}