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
        Assert.That(result.HasError<DerefNonReferenceError>());
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

public class RefLifetimeTests
{
    [Test]
    public void RefShadowInvalidatesBorrowFailTest()
    {
        // let a = 5; let b = &a; let a = 10; *b — b's borrow invalidated by shadow
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_shadow_invalidates_borrow_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefUsedBeforeShadowTest()
    {
        // Use borrow before shadow — should work (c captures value before shadow)
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_used_before_shadow_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefReborrowAfterShadowTest()
    {
        // Re-borrow from new binding after shadow — should work
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_reborrow_after_shadow_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void RefMultiBorrowShadowFailTest()
    {
        // Multiple borrows from same source, source shadowed — both invalid
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_multi_borrow_shadow_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefFieldBorrowShadowFailTest()
    {
        // Borrow from struct field, struct shadowed — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_field_borrow_shadow_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefValidSameScopeTest()
    {
        // Multiple borrows from same source, no shadow — all valid
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_valid_same_scope_test", true, true);
        Assert.That(result.Success);
        Assert.That(84 == result.Function?.Invoke());
    }
}

public class RefDerefMoveTests
{
    [Test]
    public void RefDerefMoveOutFailTest()
    {
        // *r where r: &Foo and Foo is not Copy — cannot move out of reference
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_deref_move_out_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<MoveOutOfReferenceError>());
    }

    [Test]
    public void RefDerefCopyTypeTest()
    {
        // *r where r: &i32 — i32 is Copy, multiple derefs fine
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_deref_copy_type_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void RefDerefCopyStructTest()
    {
        // *r where r: &Point and Point impls Copy — multiple derefs fine
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_deref_copy_struct_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void RefDerefMoveOutTwiceFailTest()
    {
        // Two derefs of non-Copy through reference — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_deref_move_out_twice_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<MoveOutOfReferenceError>());
    }

    [Test]
    public void RefDerefGenericCopyTest()
    {
        // fn deref_twice<T: Copy>(r: &T) -> T — generic Copy deref works
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_deref_generic_copy_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void RefAutoDerefNonCopyTest()
    {
        // r.val where r: &Foo and Foo is not Copy — auto-deref on field access still works
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_auto_deref_non_copy_test", true, true);
        Assert.That(result.Success);
        Assert.That(99 == result.Function?.Invoke());
    }
}

public class RefLifetimeReturnTests
{
    [Test]
    public void RefReturnLocalExplicitFailTest()
    {
        // return &a where a is a local — dangling reference
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_return_local_explicit_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DanglingReferenceError>());
    }

    [Test]
    public void RefReturnParamTest()
    {
        // fn foo(kk: &i32) -> &i32 { kk } — returning param ref is safe
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_return_param_test", true, true);
        Assert.That(result.Success);
        Assert.That(55 == result.Function?.Invoke());
    }

    [Test]
    public void RefReturnLocalImplicitFailTest()
    {
        // fn idk() -> &i32 { let a = 5; &a } — implicit return of local ref
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_return_local_implicit_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DanglingReferenceError>());
    }

    [Test]
    public void RefReturnLocalVarFailTest()
    {
        // let r = &a; return r — returning variable holding local borrow
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_return_local_var_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DanglingReferenceError>());
    }

    [Test]
    public void RefReturnParamVarTest()
    {
        // let r = kk; return r — kk is param, r is just a copy of the pointer
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_return_param_var_test", true, true);
        Assert.That(result.Success);
        Assert.That(77 == result.Function?.Invoke());
    }

    [Test]
    public void RefReturnMixedTest()
    {
        // idk returns dangling ref (should fail), foo returns param ref (fine)
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_return_mixed_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DanglingReferenceError>());
    }
}

public class RefBlockScopeTests
{
    [Test]
    public void RefBlockScopeEscapeFailTest()
    {
        // let gotten = { let a = 5; &a }; — borrow escapes block scope
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_block_scope_escape_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DanglingReferenceError>());
    }

    [Test]
    public void RefBlockScopeValueTest()
    {
        // let gotten = { let a = 5; a }; — no reference, just value, fine
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_block_scope_value_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefBlockOuterScopeTest()
    {
        // let gotten = { &a }; where a is in outer scope — borrow is valid
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_block_outer_scope_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void RefMatchBlockEscapeFailTest()
    {
        // match arm block returns &x where x is local to the arm — dangling
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_match_block_escape_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DanglingReferenceError>());
    }

    [Test]
    public void RefNestedBlockEscapeFailTest()
    {
        // inner block returns &a where a is in inner block — dangling
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_nested_block_escape_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<DanglingReferenceError>());
    }
}

public class RefConstTests
{
    [Test]
    public void RefConstBasicTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_const_basic_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefConstMultipleTest()
    {
        // Multiple &const from same source — ok
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_const_multiple_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void RefConstWithSharedTest()
    {
        // &T + &const T from same source — ok
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_const_with_shared_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void RefConstWithMutFailTest()
    {
        // &const T + &mut T — conflict
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_const_with_mut_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }
}

public class RefMutTests
{
    [Test]
    public void RefMutBasicTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_mut_basic_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefMutMultipleTest()
    {
        // Multiple &mut from same source — ok (shared mutable)
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_mut_multiple_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void RefMutWithSharedTest()
    {
        // &mut T + &T from same source — ok
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_mut_with_shared_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void RefMutWithConstFailTest()
    {
        // &mut T + &const T — conflict
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_mut_with_const_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }
}

public class RefUniqTests
{
    [Test]
    public void RefUniqBasicTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_basic_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefUniqWithSharedFailTest()
    {
        // &T then &uniq T — conflict
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_with_shared_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefUniqWithConstFailTest()
    {
        // &const T then &uniq T — conflict
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_with_const_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefUniqWithMutFailTest()
    {
        // &mut T then &uniq T — conflict
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_with_mut_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefUniqDoubleFailTest()
    {
        // &uniq T then &uniq T — conflict (unique means exclusive)
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_double_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefSharedAfterUniqFailTest()
    {
        // &uniq T then &T — conflict (uniq blocks everything)
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_shared_after_uniq_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefSharedMultipleTest()
    {
        // Three &T from same source — all ok
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_shared_multiple_test", true, true);
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke());
    }

    [Test]
    public void RefUniqAfterShadowTest()
    {
        // Shadow clears old borrows, so &uniq on new binding is fine
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_after_shadow_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }
}

public class RefCopyTests
{
    [Test]
    public void RefUniqNotCopyFailTest()
    {
        // &uniq is NOT Copy — second use after passing to fn should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_not_copy_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<UseAfterMoveError>());
    }

    [Test]
    public void RefSharedIsCopyTest()
    {
        // &T is Copy — can use reference twice
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_shared_is_copy_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void RefConstIsCopyTest()
    {
        // &const T is Copy — can use reference twice
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_const_is_copy_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void RefMutIsCopyTest()
    {
        // &mut T is Copy — can use reference twice
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_mut_is_copy_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }
}

public class RefNllTests
{
    [Test]
    public void RefUniqThenMutNllPassTest()
    {
        // &uniq then &mut — old borrow not used after, NLL allows this
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_then_mut_nll_pass_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefConstThenMutNllPassTest()
    {
        // &const then &mut — old borrow not used after, NLL allows this
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_const_then_mut_nll_pass_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefUniqThenUniqNllPassTest()
    {
        // &uniq then &uniq — old borrow not used after, NLL allows this
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_then_uniq_nll_pass_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }
}

public class RefNllFailTests
{
    [Test]
    public void RefUniqThenMutUseOldFailTest()
    {
        // &uniq then &mut, then USE the invalidated &uniq — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_then_mut_use_old_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefConstThenMutUseOldFailTest()
    {
        // &const then &mut, then USE the invalidated &const — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_const_then_mut_use_old_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefUniqThenUniqUseOldFailTest()
    {
        // &uniq then &uniq, then USE the first &uniq — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_then_uniq_use_old_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefUniqThenSharedUseOldFailTest()
    {
        // &uniq then &shared, then USE the invalidated &uniq — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_then_shared_use_old_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefMutThenConstUseOldFailTest()
    {
        // &mut then &const, then USE the invalidated &mut — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_mut_then_const_use_old_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefUniqThenConstUseOldFailTest()
    {
        // &uniq then &const, then USE the invalidated &uniq — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_uniq_then_const_use_old_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }
}

public class RefStandaloneBorrowTests
{
    [Test]
    public void RefStandaloneUniqInvalidatesUniqFailTest()
    {
        // &uniq a; invalidates existing &uniq borrow dd — *dd fails
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_standalone_uniq_invalidates_uniq_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefStandaloneUniqInvalidatesMutFailTest()
    {
        // &uniq a; invalidates existing &mut borrow dd — *dd fails
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_standalone_uniq_invalidates_mut_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefStandaloneUniqInvalidatesSharedFailTest()
    {
        // &uniq a; invalidates existing &shared borrow dd — *dd fails
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_standalone_uniq_invalidates_shared_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefStandaloneConstInvalidatesMutFailTest()
    {
        // &const a; invalidates existing &mut borrow dd — *dd fails
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_standalone_const_invalidates_mut_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefStandaloneMutInvalidatesConstFailTest()
    {
        // &mut a; invalidates existing &const borrow dd — *dd fails
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_standalone_mut_invalidates_const_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefStandaloneUniqNotUsedPassTest()
    {
        // &uniq a; invalidates dd but dd is never used again — pass
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_standalone_uniq_not_used_pass_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefStandaloneSharedCompatiblePassTest()
    {
        // &a; does NOT invalidate existing &a borrow — *dd still works
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_standalone_shared_compatible_pass_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefStandaloneMutCompatiblePassTest()
    {
        // &mut a; does NOT invalidate existing &mut borrow — *dd still works
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_standalone_mut_compatible_pass_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }
}

public class RefLifetimeElisionTests
{
    [Test]
    public void RefElisionFnReturnInvalidatedFailTest()
    {
        // fn returns &uniq from input, then &uniq num invalidates derived — *derived fails
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_fn_return_invalidated_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefElisionFnReturnPassTest()
    {
        // fn returns &uniq from input, no conflict — passes
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_fn_return_pass_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefElisionSharedInvalidatedFailTest()
    {
        // fn returns &shared from input, then &uniq invalidates — *derived fails
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_shared_invalidated_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefElisionSharedPassTest()
    {
        // fn returns &shared from input, compatible &shared after — passes
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_shared_pass_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void RefElisionMethodInvalidatedFailTest()
    {
        // method returns &i32 borrowing from self, then &uniq h invalidates — *r fails
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_method_invalidated_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefElisionChainedFailTest()
    {
        // pass_through(r1) propagates borrow from x, then &uniq x invalidates r2
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_chained_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }
}

public class RefElisionAmbiguityTests
{
    [Test]
    public void RefElisionAmbiguousFailTest()
    {
        // 2 ref params, returns ref, no self — ambiguous, should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_ambiguous_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<GenericSemanticError>());
    }

    [Test]
    public void RefElisionThreeRefsFailTest()
    {
        // 3 ref params, returns ref — ambiguous, should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_three_refs_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<GenericSemanticError>());
    }

    [Test]
    public void RefElisionNoRefReturnTest()
    {
        // 2 ref params but returns non-ref — no ambiguity
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_no_ref_return_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void RefElisionOneRefParamTest()
    {
        // 1 ref + 1 non-ref param, returns ref — unambiguous
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_one_ref_param_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void RefElisionSelfWinsTest()
    {
        // self ref + other ref, returns ref — self wins (Rust rule 2)
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_self_wins_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void RefElisionGenericAmbiguousFailTest()
    {
        // 2 generic ref params, returns ref — ambiguous, should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/ref_tests/ref_elision_generic_ambiguous_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<GenericSemanticError>());
    }
}
