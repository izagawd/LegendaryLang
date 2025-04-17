namespace LegendaryLang.Parse;



/// <summary>
/// Things/types that are not monomorphized,, or itself after monomorphizable. Eg MyStruct<T> where T is a generic wont count,
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