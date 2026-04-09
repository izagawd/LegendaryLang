using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class DerefAssignTests
{
    [Test] public void DerefAssignBasicTest() => AssertSuccess("ref_tests/deref_assign_basic_test", 42);

    [Test] public void DerefAssignAddTest() => AssertSuccess("ref_tests/deref_assign_add_test", 15);

    [Test] public void DerefAssignFieldTest() => AssertSuccess("ref_tests/deref_assign_field_test", 99);

    [Test] public void DerefAssignMutParamTest() => AssertSuccess("ref_tests/deref_assign_mut_param_test", 15);

    [Test]
    public void DerefAssignUniqParamTest()
    {
        // fn add_to(r: &uniq i32, amount: i32) { *r = *r + amount; }
        // Called twice: 0 + 7 = 7, 7 + 3 = 10
        var result = Compile("ref_tests/deref_assign_uniq_param_test");
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test] public void DerefAssignBothSidesTest() => AssertSuccess("ref_tests/deref_assign_both_sides_test", 42);

    [Test] public void DerefAssignNestedTest() => AssertSuccess("ref_tests/deref_assign_nested_test", 9);

    [Test] public void DerefAssignFieldChainTest() => AssertSuccess("ref_tests/deref_assign_field_chain_test", 55);
}
