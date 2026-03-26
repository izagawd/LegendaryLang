using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class DuplicateTests
{
    [Test]
    public void DuplicateFnTest()
    {
        // fn bro() {} fn bro() {} at top level — should fail with duplicate error
        var result = Compiler.CompileWithResult(
            "compiler_tests/duplicate_tests/duplicate_fn_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DuplicateDefinitionError>());
    }

    [Test]
    public void DuplicateStructTest()
    {
        // struct Idk<T> {} struct Idk<U> {} at top level — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/duplicate_tests/duplicate_struct_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DuplicateDefinitionError>());
    }

    [Test]
    public void DuplicateNestedFnTest()
    {
        // fn bro() {} fn bro() {} inside main — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/duplicate_tests/duplicate_nested_fn_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DuplicateDefinitionError>());
    }

    [Test]
    public void DuplicateNestedStructTest()
    {
        // struct Idk<T> {} struct Idk<U> {} inside main — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/duplicate_tests/duplicate_nested_struct_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DuplicateDefinitionError>());
    }

    [Test]
    public void DuplicateNestedTraitTest()
    {
        // trait Foo {} trait Foo {} inside main — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/duplicate_tests/duplicate_nested_trait_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DuplicateDefinitionError>());
    }
}
