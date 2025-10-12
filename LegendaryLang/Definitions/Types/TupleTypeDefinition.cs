using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions.Types;

public class TupleTypeDefinition : CustomTypeDefinition
{
    public TupleTypeDefinition(IEnumerable<LangPath> composedTypes)
    {
        ComposedTypes = composedTypes.ToImmutableArray();
    }

    public override LangPath FullPath => new TupleLangPath([] /**TO CHANGE LATER**/);
    public override string Name => $"({string.Join(',', ComposedTypes)})";
    public override NormalLangPath Module { get; } = new(null, []);

    public override LangPath TypePath => new TupleLangPath(ComposedTypes);
    public override Token Token { get; }


    public ImmutableArray<GenericParameter> GenericParameters
    {
        get
        {
            var genericParameters = new List<GenericParameter>();
            for (var i = 0; i < GenericParameters.Length; i++) genericParameters.Add(new GenericParameter(i.ToString()));
            return genericParameters.ToImmutableArray();
        }
    }


    public override ImmutableArray<LangPath> ComposedTypes { get; }

    public override void ResolvePaths(PathResolver resolver)
    {
        foreach (var i in ComposedTypes)
        {
            i.Resolve(resolver);
        }
    }


    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        var monomorphizedArguments = genericArguments.Select(i => i.Monomorphize(context)).ToArray();
        var structt =
            context.Module.Context.CreateNamedStruct(
                new TupleLangPath(monomorphizedArguments).ToString());

        var types = monomorphizedArguments
            .Select(i => (TypeRefItem)context.GetRefItemFor(i)!)
            .Select(i => i.Type)
            .ToArray();
        
        structt.StructSetBody(types.Select(i => i.TypeRef).ToArray(), false);
        return new TypeRefItem()
        {
            Type = new TupleType(this,types, structt)
            {
                TypeDefinition = { }
            }
        };
    }
    
    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        throw new NotImplementedException();
    }

    public override void Analyze(SemanticAnalyzer analyzer)
    {
    }
}