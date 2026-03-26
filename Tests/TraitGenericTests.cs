using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class TraitGenericTests
{
    [Test]
    public void TraitGenericTraitTest()
    {
        // trait Converter<Target> — generic trait with single param
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_generic_trait_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void TraitMultiGenericTraitTest()
    {
        // trait Combiner<A, B> — generic trait with two params
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_multi_generic_trait_test", true, true);
        Assert.That(result.Success);
        Assert.That(30 == result.Function?.Invoke());
    }

    [Test]
    public void TraitAssocTypeBasicTest()
    {
        // trait Producer { type Output; } — basic associated type
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_assoc_type_basic_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void TraitAssocTypeBoundTest()
    {
        // type Product: Copy — associated type with trait bound (i32 satisfies Copy)
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_assoc_type_bound_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void TraitAssocTypeBoundFailTest()
    {
        // type Product: Copy — but Product = NonCopy, should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_assoc_type_bound_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.Errors.Any(e => e.Message.Contains("does not satisfy bound")));
    }

    [Test]
    public void TraitAssocTypeMissingTest()
    {
        // impl Maker for Foo without type Product = ...; — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_assoc_type_missing_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.Errors.Any(e => e.Message.Contains("Missing associated type")));
    }

    [Test]
    public void TraitMultiAssocTypeTest()
    {
        // trait Transform { type Input; type Output; }
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_multi_assoc_type_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void TraitGenericBoundWithGenericArgTest()
    {
        // fn add_them<T: Add<T> + Copy>(one: T, two: T) -> T — bound Add<T> uses the generic param
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_generic_bound_with_generic_arg_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void TraitImplGenericBoundWithGenericArgTest()
    {
        // impl<T: Add<T> + Copy> Add<Wrapper<T>> for Wrapper<T>
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_impl_generic_bound_with_generic_arg_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }
}

public class OperatorTraitTests
{
    [Test]
    public void OpsAddI32Test()
    {
        // 3 + 7 via Add<i32> for i32
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/ops_add_i32_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void OpsAllFourTest()
    {
        // (10 + 5 - 3) * 2 / 6 = 4
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/ops_all_four_test", true, true);
        Assert.That(result.Success);
        Assert.That(4 == result.Function?.Invoke());
    }

    [Test]
    public void OpsNoImplFailTest()
    {
        // Foo + Foo where Foo doesn't impl Add — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/ops_no_impl_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.Errors.Any(e => e.Message.Contains("does not implement")));
    }

    [Test]
    public void OpsCustomAddTest()
    {
        // Vec2 + Vec2 via custom impl Add<Vec2> for Vec2
        // (1+3) + (2+4) = 10
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/ops_custom_add_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void OpsCustomTypeAddOperatorTest()
    {
        // Foo{} + 5 via impl Add<i32> for Foo — uses operator syntax, not qualified call
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/ops_custom_type_add_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void OpsPrimitiveLhsCustomRhsTest()
    {
        // 4 + Foo{} via impl Add<Foo> for i32 — primitive left, custom right
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/ops_primitive_lhs_custom_rhs_test", true, true);
        Assert.That(result.Success);
        Assert.That(4 == result.Function?.Invoke());
    }

    [Test]
    public void OpsNestedCustomAddTest()
    {
        // Foo{} + 5 via nested impl Add<i32> for Foo inside main
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/ops_nested_custom_add_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void OpsNestedPrimitiveLhsTest()
    {
        // 4 + Foo{} via nested impl Add<Foo> for i32 inside main
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/ops_nested_primitive_lhs_test", true, true);
        Assert.That(result.Success);
        Assert.That(4 == result.Function?.Invoke());
    }
}
