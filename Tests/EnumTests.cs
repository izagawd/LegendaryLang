using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class EnumTests
{
    [Test]
    public void EnumBasicTest()
    {
        // Unit variants with match — Color::Green => 2
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_basic_test", true, true);
        Assert.That(result.Success);
        Assert.That(2 == result.Function?.Invoke());
    }

    [Test]
    public void EnumTupleVariantTest()
    {
        // Tuple variant Foo::C(42) with pattern extraction
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_tuple_variant_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void EnumGenericTest()
    {
        // Option<i32>::Some(7) with generic inference
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_generic_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void EnumMultiGenericTest()
    {
        // Either<i32, i32>::Left(5) with explicit turbofish
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_multi_generic_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void EnumWildcardTest()
    {
        // _ wildcard pattern catches non-matched variants
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_wildcard_test", true, true);
        Assert.That(result.Success);
        Assert.That(99 == result.Function?.Invoke());
    }

    [Test]
    public void EnumNestedTest()
    {
        // Enum defined inside function body
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_nested_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void EnumNonExhaustiveFailTest()
    {
        // Missing variant in match without wildcard — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_non_exhaustive_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.Errors.Any(e => e.Message.Contains("not covered")));
    }

    [Test]
    public void EnumMultiFieldVariantTest()
    {
        // Variant with two fields: Pair::Two(3, 7) => 3 + 7 = 10
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_multi_field_variant_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void EnumFnArgTest()
    {
        // Passing enum to a function
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_fn_arg_test", true, true);
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke());
    }

    [Test]
    public void EnumFnReturnTest()
    {
        // Returning enum from a function
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_fn_return_test", true, true);
        Assert.That(result.Success);
        Assert.That(20 == result.Function?.Invoke());
    }

    [Test]
    public void EnumDuplicateVariantFailTest()
    {
        // Duplicate variant name — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_duplicate_variant_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.Errors.Any(e => e.Message.Contains("Duplicate variant")));
    }

    [Test]
    public void EnumWrongFieldCountFailTest()
    {
        // Wrong number of fields in variant construction — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_wrong_field_count_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.Errors.Any(e => e.Message.Contains("field")));
    }

    [Test]
    public void EnumMatchReturnInArmTest()
    {
        // Match arm with explicit return — arm type is void but shouldn't cause type mismatch
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_match_return_in_arm_test", true, true);
        Assert.That(result.Success);
    }

    [Test]
    public void EnumMatchAsExprTest()
    {
        // let b = match c { ... } — match used as expression assigned to variable
        var result = Compiler.CompileWithResult(
            "compiler_tests/enum_tests/enum_match_as_expr_test", true, true);
        Assert.That(result.Success);
        Assert.That(2 == result.Function?.Invoke());
    }
}
