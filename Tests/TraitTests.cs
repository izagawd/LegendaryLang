namespace Tests;

public class TraitTests
{
    [Fact]
    public void TraitBasicTest()
    {
        var result = LegendaryLang.Compiler.Compile(
            "compiler_tests\\trait_tests\\trait_basic_test", true, true);
        // add_things::<i32>(10, 20) = 30
        // get_val::<Point>(Point{x=3,y=7}) = 10
        // total = 40
        Assert.Equal(40, result?.Invoke());
    }
}
