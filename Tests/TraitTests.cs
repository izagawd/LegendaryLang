using NUnit.Framework;

namespace Tests;

public class TraitTests
{
    [Test]
    public void TraitBasicTest()
    {
        var result = LegendaryLang.Compiler.Compile(
            "compiler_tests/trait_tests/trait_basic_test", true, true);
        // add_things::<i32>(10, 20) = 30
        // get_val::<Point>(Point{x=3,y=7}) = 10
        // total = 40
        Assert.That(40 == result?.Invoke());
    }

    [Test]
    public void TraitGenericParamCallTest()
    {
        var result = LegendaryLang.Compiler.Compile(
            "compiler_tests/trait_tests/trait_generic_param_call_test", true, true);
        // the_fooer::<i32>() calls T::bro() which resolves to impl Foo for i32 -> 3
        Assert.That(3 == result?.Invoke());
    }

    [Test]
    public void TraitBoundViolationTest()
    {
        // bool does not implement Foo, so this should fail to compile
        var result = LegendaryLang.Compiler.Compile(
            "compiler_tests/trait_tests/trait_bound_violation_test", true, true);
        Assert.That(result == null);
    }
}
