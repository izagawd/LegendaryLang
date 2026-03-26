using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class AssocTypeTests
{
    [Test]
    public void AssocQualifiedLetTest()
    {
        // let kk: <i32 as Add<i32>>::Output = 5 — Output resolves to i32
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_qualified_let_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void AssocQualifiedNestedTest()
    {
        // <<i32 as Add<i32>>::Output as Add<i32>>::Output — double nested resolution
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_qualified_nested_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void AssocTripleNestedTest()
    {
        // <<<i32 as Add<i32>>::Output as Add<i32>>::Output as Add<i32>>::Output — triple nested
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_triple_nested_test", true, true);
        Assert.That(result.Success);
        Assert.That(3 == result.Function?.Invoke());
    }

    [Test]
    public void AssocFnReturnTypeTest()
    {
        // fn dd<T: Add<T, Output = T>>(one: T, two: T) -> <T as Add<T>>::Output
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_fn_return_type_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void AssocSelfInImplTest()
    {
        // <Self as Add<Self>>::Output in trait method context where Self = i32
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_self_in_impl_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void AssocNoBoundFailTest()
    {
        // fn dd<T>(one: T) -> <T as Add<T>>::Output — T has no Add bound, should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_no_bound_fail_test", true, true);
        Assert.That(!result.Success);
    }

    [Test]
    public void AssocWrongTraitFailTest()
    {
        // fn dd<T: Copy>(one: T) -> <T as Add<T>>::Output — T has Copy, not Add
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_wrong_trait_fail_test", true, true);
        Assert.That(!result.Success);
    }

    [Test]
    public void AssocQualifiedInNestedFnTest()
    {
        // <i32 as Add<i32>>::Output as return type of a nested function
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_qualified_in_nested_fn_test", true, true);
        Assert.That(result.Success);
        Assert.That(12 == result.Function?.Invoke());
    }

    [Test]
    public void AssocCustomTraitQualifiedTest()
    {
        // <i32 as Maker>::Product with a custom trait Maker { type Product; }
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_custom_trait_qualified_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void AssocTraitNotFoundFailTest()
    {
        // <i32 as NonExistentTrait>::Output — trait doesn't exist
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_trait_not_found_fail_test", true, true);
        Assert.That(!result.Success);
    }

    [Test]
    public void AssocTypeMismatchFailTest()
    {
        // let kk: <i32 as Add<i32>>::Output = true — Output is i32 but assigning bool
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_type_mismatch_fail_test", true, true);
        Assert.That(!result.Success);
    }

    [Test]
    public void AssocQualifiedArgTypeTest()
    {
        // fn take_output(x: <i32 as Add<i32>>::Output) -> i32 — qualified path as param type
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_qualified_arg_type_test", true, true);
        Assert.That(result.Success);
        Assert.That(11 == result.Function?.Invoke());
    }

    [Test]
    public void AssocInStructFieldTest()
    {
        // struct Holder { val: <i32 as Add<i32>>::Output } — qualified path in field type
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_in_struct_field_test", true, true);
        Assert.That(result.Success);
        Assert.That(13 == result.Function?.Invoke());
    }

    [Test]
    public void AssocGenericOperatorOutputTest()
    {
        // fn something<T: Add<T>>(...) -> <T as Add<T>>::Output and T::Output shorthand
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_generic_operator_output_test", true, true);
        Assert.That(result.Success);
        Assert.That(16 == result.Function?.Invoke());
    }

    [Test]
    public void AssocGenericSubOutputTest()
    {
        // fn do_sub<T: Sub<T>>(...) -> <T as Sub<T>>::Output
        var result = Compiler.CompileWithResult(
            "compiler_tests/assoc_type_tests/assoc_generic_sub_output_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }
}
