using LegendaryLang.Parse;

namespace LegendaryLang.Definitions;



/// <summary>
/// EG MyStruct<T> where T is a generic wont count as a concrete definition,
/// but MyStruct<i32> would count. same goes for functions and traits. 
/// </summary>
public interface IConcreteDefinition : IDefinition
{
    public IDefinition? Definition { get; }
    /// <summary>
    /// NOTE: DO NOT ADD TO SCOPE! <see cref="CodeGenContext"/> will handle that!!
    /// </summary>
    /// <param name="context"></param>
    public void CodeGen(CodeGenContext context);
}