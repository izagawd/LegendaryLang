using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class StructGenericTests
{
    [Test]
    public void GenericStructBasicTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/struct_tests/generic_struct_basic_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void GenericStructPairTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/struct_tests/generic_struct_pair_test", true, true);
        Assert.That(result.Success);
        Assert.That(30 == result.Function?.Invoke()); // 10 + 20
    }

    [Test]
    public void GenericStructUnusedParamTest()
    {
        // struct Foo<T> { val: i32 } — T is never used, should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/struct_tests/generic_struct_unused_param_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<GenericSemanticError>());
        
    }

    [Test]
    public void GenericStructNestedTest()
    {
        // Wrapper::<Wrapper::<i32>> — nested generic structs
        var result = Compiler.CompileWithResult(
            "compiler_tests/struct_tests/generic_struct_nested_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void GenericStructImplTraitTest()
    {
        // impl HasValue for Wrapper::<i32>, call via generic fn
        var result = Compiler.CompileWithResult(
            "compiler_tests/struct_tests/generic_struct_impl_trait_test", true, true);
        Assert.That(result.Success);
        Assert.That(99 == result.Function?.Invoke());
    }

    [Test]
    public void GenericImplBlanketTest()
    {
        // impl<T: Copy> Summable for Wrapper::<T> — blanket impl
        // Wrapper::<i32> satisfies because i32: Copy
        var result = Compiler.CompileWithResult(
            "compiler_tests/struct_tests/generic_impl_blanket_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void GenericImplBoundFailTest()
    {
        // impl<T: Copy> Summable for Wrapper::<T> — NonCopy doesn't impl Copy
        // So Wrapper::<NonCopy> should NOT implement Summable
        var result = Compiler.CompileWithResult(
            "compiler_tests/struct_tests/generic_impl_bound_fail_test", true, true);
        Assert.That(!result.Success);
    }

    [Test]
    public void GenericImplUnusedParamTest()
    {
        // impl<T> Foo for Bar {} — T not used in Bar, should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/struct_tests/generic_impl_unused_param_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<GenericSemanticError>());
        
    }

    [Test]
    public void GenericStructFnTest()
    {
        // fn extract<T: Copy>(w: Wrapper::<T>) -> T
        var result = Compiler.CompileWithResult(
            "compiler_tests/struct_tests/generic_struct_fn_test", true, true);
        Assert.That(result.Success);
        Assert.That(77 == result.Function?.Invoke());
    }
}
