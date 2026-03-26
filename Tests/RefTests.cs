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
    public void RefFnParamTest()
    {
        // fn read_val(r: &i32) -> i32 { *r } — pass reference to function
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_fn_param_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void RefIsCopyTest()
    {
        // References are Copy — read_val(r) + read_val(r) works
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_is_copy_test", true, true);
        Assert.That(result.Success);
        Assert.That(20 == result.Function?.Invoke());
    }

    [Test]
    public void RefNoMoveTest()
    {
        // Borrowing non-Copy struct doesn't move it
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_no_move_test", true, true);
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
    public void RefInStructFieldTest()
    {
        // struct Holder { r: &i32 } — reference as struct field
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_in_struct_field_test", true, true);
        Assert.That(result.Success);
        Assert.That(99 == result.Function?.Invoke());
    }

    [Test]
    public void RefNestedDerefTest()
    {
        // let r2 = &r1; **r2 — reference to reference
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_nested_deref_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
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

    [Test]
    public void RefReturnTypeTest()
    {
        // fn get_ref(r: &i32) -> &i32 { r } — return a reference
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_return_type_test", true, true);
        Assert.That(result.Success);
        Assert.That(55 == result.Function?.Invoke());
    }
}
