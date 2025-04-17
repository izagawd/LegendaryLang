using System.Collections.Immutable;
using LegendaryLang.Parse;

namespace LegendaryLang.Definitions;

public interface IMonomorphizable : IDefinition
{
/// <summary>
/// 
/// </summary>
/// <param name="path">eg to monomorphize foo::func::<T> to foo::func::<i32> , u pass foo::func::<i32></param>
/// <returns>    Returns generic parameters from a lang path if the functin can be monomorphized to itif not, should return null</returns>

    public ImmutableArray<LangPath>? GetGenericArguments(LangPath path);

    /// <summary>
    /// NOTE: DO NOT ADD TO SCOPE! OR CODEGEN THE RETURNED CONCRETE DEFINITION!! <see cref="CodeGenContext"/> will handle that!!
    /// </summary>
    /// <param name="context"></param>
    public IConcreteDefinition? Monomorphize(CodeGenContext codeGenContext, LangPath ident);
}