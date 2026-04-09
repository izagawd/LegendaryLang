using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class StructGenericTests
{
    [Test] public void GenericStructBasicTest() => AssertSuccess("struct_tests/generic_struct_basic_test", 42);

    [Test]
    public void GenericStructPairTest()
    {
        var result = Compile("struct_tests/generic_struct_pair_test");
        Assert.That(result.Success);
        Assert.That(30 == result.Function?.Invoke()); // 10 + 20
    }

    [Test] public void GenericStructUnusedParamTest() => AssertFail<GenericSemanticError>("struct_tests/generic_struct_unused_param_test");

    [Test] public void GenericStructNestedTest() => AssertSuccess("struct_tests/generic_struct_nested_test", 7);

    [Test] public void GenericStructImplTraitTest() => AssertSuccess("struct_tests/generic_struct_impl_trait_test", 99);

    [Test]
    public void GenericImplBlanketTest()
    {
        // impl<T: Copy> Summable for Wrapper(T) — blanket impl
        // Wrapper(i32) satisfies because i32: Copy
        var result = Compile("struct_tests/generic_impl_blanket_test");
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void GenericImplBoundFailTest()
    {
        // impl<T: Copy> Summable for Wrapper(T) — NonCopy doesn't impl Copy
        // So Wrapper(NonCopy) should NOT implement Summable
        var result = Compile("struct_tests/generic_impl_bound_fail_test");
        Assert.That(!result.Success);
    }

    [Test] public void GenericImplUnusedParamTest() => AssertFail<GenericSemanticError>("struct_tests/generic_impl_unused_param_test");

    [Test] public void GenericStructFnTest() => AssertSuccess("struct_tests/generic_struct_fn_test", 77);
}
