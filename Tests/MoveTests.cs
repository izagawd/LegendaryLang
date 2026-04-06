using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class MoveTests
{
    [Test]
    public void UseAfterMoveTest()
    {
        // Struct doesn't impl Copy, so let b = a moves a
        var result = Compile("move_tests/use_after_move_test");
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
        var result = Compile("move_tests/copy_type_no_move_test");
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke()); // 5 + 5
    }

    [Test]
    public void ReassignAfterMoveTest()
    {
        // a is moved, then reassigned — should be usable again
        var result = Compile("move_tests/reassign_after_move_test");
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke()); // 10 + 5
    }

    [Test] public void MoveOnFnCallTest() => AssertFail<UseAfterMoveError>("move_tests/move_on_fn_call_test");

    [Test]
    public void CopyTypeFnCallTest()
    {
        // i32 is Copy, so passing to function does NOT move
        var result = Compile("move_tests/copy_type_fn_call_test");
        Assert.That(result.Success);
        Assert.That(11 == result.Function?.Invoke()); // 5 + 6
    }

    [Test] public void GenericCopyBoundTest() => AssertSuccess("move_tests/generic_copy_bound_test", 5);

    [Test] public void GenericNoCopyBoundBodyTest() => AssertFail<UseAfterMoveError>("move_tests/generic_no_copy_bound_body_test");

    [Test] public void GenericNoCopyBoundCallSiteTest() => AssertFail<UseAfterMoveError>("move_tests/generic_no_copy_bound_test");

    [Test]
    public void CopyStructValidTest()
    {
        // All fields are i32 (Copy), so impl Copy for Point is valid
        // let b = a should copy, a is still usable
        var result = Compile("move_tests/copy_struct_valid_test");
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke()); // 3 + 4
    }

    [Test] public void CopyStructInvalidFieldTest() => AssertFail<GenericSemanticError>("move_tests/copy_struct_invalid_field_test");

    [Test]
    public void CopyGenericStructValidTest()
    {
        // impl<T: Copy> Copy for Wrapper(T) — T has Copy bound, so valid
        // Wrapper(i32) is Copy, so let b = a doesn't move a
        var result = Compile("move_tests/copy_generic_struct_valid_test");
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke()); // 5 + 5
    }

    [Test] public void CopyGenericStructNoBoundTest() => AssertFail<GenericSemanticError>("move_tests/copy_generic_struct_no_bound_test");

    [Test]
    public void CopyGenericWrapperInFnTest()
    {
        // fn idk<T: Copy> — Wrapper(T) is Copy because impl<T: Copy> Copy for Wrapper(T)
        // Using made twice should succeed (it's Copy)
        var result = Compile("move_tests/copy_generic_wrapper_in_fn_test");
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void CopyImplBoundsInMethodTest()
    {
        // impl<T: Copy> Something for Wrapper(T) — inside bruh(), input is Wrapper(T)
        // which is Copy because T: Copy from the impl bounds. Using input twice should work.
        var result = Compile("move_tests/copy_impl_bounds_in_method_test");
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test] public void OperatorMovesNonCopyGenericTest() => AssertFail<UseAfterMoveError>("move_tests/operator_moves_non_copy_test");

    [Test]
    public void OperatorCopyAllowsReuseTest()
    {
        // T: Add(T) + Copy — using two twice in one + two + two should succeed
        var result = Compile("move_tests/operator_copy_allows_reuse_test");
        Assert.That(result.Success);
        Assert.That(8 == result.Function?.Invoke()); // 2 + 3 + 3
    }

    [Test] public void OperatorConcreteMovesTest() => AssertFail<UseAfterMoveError>("move_tests/operator_concrete_moves_test");

    [Test] public void MoveInnerScopeNoLeakTest() => AssertSuccess("move_tests/move_inner_scope_no_leak_test", 4);

    [Test] public void MoveOuterVisibleInInnerTest() => AssertFail<UseAfterMoveError>("move_tests/move_outer_visible_in_inner_test");

    [Test] public void MoveOuterStillMovedAfterInnerTest() => AssertFail<UseAfterMoveError>("move_tests/move_outer_still_moved_after_inner_test");

    [Test] public void EnumCopyNonCopyFieldFailTest() => AssertFail<GenericSemanticError>("move_tests/enum_copy_non_copy_field_fail_test");

    [Test] public void EnumCopyAllCopyTest() => AssertSuccess("move_tests/enum_copy_all_copy_test", 4);

    [Test]
    public void StructFieldMovesNonCopyFailTest()
    {
        // Box (non-Copy) used as struct field initializer → moved into struct → can't reuse
        AssertFail<UseAfterMoveError>("move_tests/struct_field_moves_noncopy_fail_test");
    }
}
