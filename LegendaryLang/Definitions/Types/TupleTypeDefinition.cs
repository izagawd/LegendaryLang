using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Parse.Types;

public class TupleTypeDefinition : CustomTypeDefinition
{
    public override Type? Monomorphize(CodeGenContext context, LangPath langPath)
    {
        if ((this as IDefinition).FullPath == langPath)
        {
            var tup = new TupleType(OtherTypes);
            tup.CodeGen(context);
            return tup;
        }
        return null;
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath langPath)
    {
        throw new NotImplementedException();
    }

    public override string Name => $"({string.Join(',', OtherTypes.Select(i => i.TypePath))})";
    public override NormalLangPath Module { get; } = new NormalLangPath(null, []);

    public override LangPath TypePath => new TupleLangPath(OtherTypes.Select(i => i.TypePath));
    public override Token LookUpToken { get; }
    public override void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }
    public ImmutableArray<Type> OtherTypes { get; }
    public TupleTypeDefinition( IEnumerable<Type> otherTypes)
    {
        OtherTypes = otherTypes.ToImmutableArray();
    }

    public ImmutableArray<GenericParameter> GenericParameters
    {
        get
        {
            List<GenericParameter> genericParameters = new List<GenericParameter>();
            for (int i = 0; i < OtherTypes.Length; i++)
            {
                genericParameters.Add(new GenericParameter(i.ToString()));
            }
            return genericParameters.ToImmutableArray();
        }
    }



    public override ImmutableArray<LangPath> ComposedTypes => OtherTypes.Select(i => i.TypePath).ToImmutableArray();
}