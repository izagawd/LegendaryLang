using System.Collections.Immutable;

namespace LegendaryLang.Parse;

public interface IMonomorphizable : IDefinition
{
    /// <summary>
    /// Returns generic parameters from a lang path if the functin can be monomorphized to it
    /// </summary>

    public ImmutableArray<LangPath>? GetGenericArguments(LangPath ident);

    /// <summary>
    /// Monomorhize and codegen for specific generic arguments.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="langPath"></param>
    /// <returns></returns>
    public IConcreteDefinition? Monomorphize(CodeGenContext codeGenContext, LangPath ident);
}