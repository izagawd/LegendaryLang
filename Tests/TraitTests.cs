using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class TraitTests
{
    [Test]
    public void TraitBasicTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_basic_test", true, true);
        Assert.That(result.Success);
        Assert.That(40 == result.Function?.Invoke());
    }

    [Test]
    public void TraitGenericParamCallTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_generic_param_call_test", true, true);
        Assert.That(result.Success);
        Assert.That(3 == result.Function?.Invoke());
    }

    [Test]
    public void TraitBoundViolationTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_bound_violation_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<TraitBoundViolationError>());

        var violations = result.GetErrors<TraitBoundViolationError>().ToList();
        Assert.That(violations.Count == 1);
        Assert.That(violations[0].TypePath.ToString().Contains("bool"));
        Assert.That(violations[0].TraitPath.ToString().Contains("Foo"));
    }

    [Test]
    public void TraitEmptyBoundTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_empty_bound_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void TraitMultiBoundTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_multi_bound_test", true, true);
        Assert.That(result.Success);
        Assert.That(16 == result.Function?.Invoke());
    }

    [Test]
    public void TraitMultiBoundViolationTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_multi_bound_violation_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<TraitBoundViolationError>());

        var violations = result.GetErrors<TraitBoundViolationError>().ToList();
        Assert.That(violations.Count == 2);

        var traitNames = violations.Select(v => v.TraitPath.ToString()).ToList();
        Assert.That(traitNames.Any(t => t.Contains("Adder")));
        Assert.That(traitNames.Any(t => t.Contains("Multiplier")));
        Assert.That(violations.All(v => v.TypePath.ToString().Contains("bool")));
    }

    [Test]
    public void TraitDuplicateGenericParamTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_duplicate_generic_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<GenericSemanticError>());
        Assert.That(result.Errors.Any(e => e.Message.Contains("Duplicate generic parameter")));
    }

    [Test]
    public void TraitQualifiedCallTest()
    {
        // <i32 as Default>::default() should resolve Self to i32
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_qualified_call_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void TraitConcreteTypeCallTest()
    {
        // i32::default() should find the impl and resolve to 99
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_concrete_type_call_test", true, true);
        Assert.That(result.Success);
        Assert.That(99 == result.Function?.Invoke());
    }
}
