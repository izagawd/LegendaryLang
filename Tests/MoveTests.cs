using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class MoveTests
{
    [Test]
    public void UseAfterMoveTest()
    {
        // Struct doesn't impl Copy, so let b = a moves a
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/use_after_move_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<UseAfterMoveError>());

        var errors = result.GetErrors<UseAfterMoveError>().ToList();
        Assert.That(errors.Count == 1);
        Assert.That(errors[0].VariablePath.ToString().Contains("a"));
    }

    [Test]
    public void CopyTypeNoMoveTest()
    {
        // i32 implements Copy, so let b = a does NOT move a
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/copy_type_no_move_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke()); // 5 + 5
    }

    [Test]
    public void ReassignAfterMoveTest()
    {
        // a is moved, then reassigned — should be usable again
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/reassign_after_move_test", true, true);
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke()); // 10 + 5
    }

    [Test]
    public void MoveOnFnCallTest()
    {
        // Passing non-Copy struct to function moves it
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/move_on_fn_call_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<UseAfterMoveError>());
    }

    [Test]
    public void CopyTypeFnCallTest()
    {
        // i32 is Copy, so passing to function does NOT move
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/copy_type_fn_call_test", true, true);
        Assert.That(result.Success);
        Assert.That(11 == result.Function?.Invoke()); // 5 + 6
    }

    [Test]
    public void GenericCopyBoundTest()
    {
        // T: Copy — using a twice inside the body is fine
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/generic_copy_bound_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void GenericNoCopyBoundBodyTest()
    {
        // T without Copy — let b = a; let c = a; should fail (use after move inside body)
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/generic_no_copy_bound_body_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<UseAfterMoveError>());
    }

    [Test]
    public void GenericNoCopyBoundCallSiteTest()
    {
        // Wrapper doesn't impl Copy — use_twice::<Wrapper>(w, w) should fail (w used after move)
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/generic_no_copy_bound_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<UseAfterMoveError>());
    }

    [Test]
    public void CopyStructValidTest()
    {
        // All fields are i32 (Copy), so impl Copy for Point is valid
        // let b = a should copy, a is still usable
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/copy_struct_valid_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke()); // 3 + 4
    }

    [Test]
    public void CopyStructInvalidFieldTest()
    {
        // Holder contains NonCopy (which doesn't impl Copy), so impl Copy for Holder should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/copy_struct_invalid_field_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.Errors.Any(e => e.Message.Contains("does not implement Copy")));
    }

    [Test]
    public void CopyGenericStructValidTest()
    {
        // impl<T: Copy> Copy for Wrapper<T> — T has Copy bound, so valid
        // Wrapper::<i32> is Copy, so let b = a doesn't move a
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/copy_generic_struct_valid_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke()); // 5 + 5
    }

    [Test]
    public void CopyGenericStructNoBoundTest()
    {
        // impl<T> Copy for Wrapper<T> — T has NO Copy bound, should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/copy_generic_struct_no_bound_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.Errors.Any(e => e.Message.Contains("does not have a Copy bound")));
    }

    [Test]
    public void CopyGenericWrapperInFnTest()
    {
        // fn idk<T: Copy> — Wrapper<T> is Copy because impl<T: Copy> Copy for Wrapper<T>
        // Using made twice should succeed (it's Copy)
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/copy_generic_wrapper_in_fn_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void CopyImplBoundsInMethodTest()
    {
        // impl<T: Copy> Something for Wrapper<T> — inside bruh(), input is Wrapper<T>
        // which is Copy because T: Copy from the impl bounds. Using input twice should work.
        var result = Compiler.CompileWithResult(
            "compiler_tests/move_tests/copy_impl_bounds_in_method_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }
}
