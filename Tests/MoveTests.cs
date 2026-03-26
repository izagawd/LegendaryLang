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
}
