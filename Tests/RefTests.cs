using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class RefTests
{
    [Test]
    public void RefBasicTest()
    {
        // let a = 5; let r = &a; *r — basic borrow and deref
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_basic_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefAutoDerefFieldTest()
    {
        // r.x + r.y where r: &Point — auto-deref on field access
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_auto_deref_field_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void RefDerefNonRefFailTest()
    {
        // *a where a is i32, not a reference — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_deref_non_ref_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.Errors.Any(e => e.Message.Contains("dereference")));
    }

    [Test]
    public void RefDerefArithmeticTest()
    {
        // *&a + *&b — deref inline with arithmetic
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_deref_arithmetic_test", true, true);
        Assert.That(result.Success);
        Assert.That(30 == result.Function?.Invoke());
    }
}
