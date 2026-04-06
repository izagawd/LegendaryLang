using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class RefTests
{
    [Test] public void RefBasicTest() => AssertSuccess("ref_tests/ref_basic_test", 5);

    [Test] public void RefAutoDerefFieldTest() => AssertSuccess("ref_tests/ref_auto_deref_field_test", 7);

    [Test] public void RefFnParamTest() => AssertSuccess("ref_tests/ref_fn_param_test", 42);

    [Test] public void RefIsCopyTest() => AssertSuccess("ref_tests/ref_is_copy_test", 20);

    [Test] public void RefNoMoveTest() => AssertSuccess("ref_tests/ref_no_move_test", 7);

    [Test] public void RefDerefNonRefFailTest() => AssertFail<DerefNonReferenceError>("ref_tests/ref_deref_non_ref_fail_test");

    [Test] public void RefInStructFieldTest() => AssertSuccess("ref_tests/ref_in_struct_field_test", 99);

    [Test] public void RefNestedDerefTest() => AssertSuccess("ref_tests/ref_nested_deref_test", 5);

    [Test] public void RefDerefArithmeticTest() => AssertSuccess("ref_tests/ref_deref_arithmetic_test", 30);

    [Test] public void RefReturnTypeTest() => AssertSuccess("ref_tests/ref_return_type_test", 55);
}

public class RefLifetimeTests
{
    [Test] public void RefShadowInvalidatesBorrowFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_shadow_invalidates_borrow_fail_test");

    [Test] public void RefUsedBeforeShadowTest() => AssertSuccess("ref_tests/ref_used_before_shadow_test", 5);

    [Test] public void RefReborrowAfterShadowTest() => AssertSuccess("ref_tests/ref_reborrow_after_shadow_test", 10);

    [Test] public void RefMultiBorrowShadowFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_multi_borrow_shadow_fail_test");

    [Test] public void RefFieldBorrowShadowFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_field_borrow_shadow_fail_test");

    [Test] public void RefValidSameScopeTest() => AssertSuccess("ref_tests/ref_valid_same_scope_test", 84);
}

public class RefDerefMoveTests
{
    [Test] public void RefDerefMoveOutFailTest() => AssertFail<MoveOutOfReferenceError>("ref_tests/ref_deref_move_out_fail_test");

    [Test] public void RefDerefCopyTypeTest() => AssertSuccess("ref_tests/ref_deref_copy_type_test", 10);

    [Test] public void RefDerefCopyStructTest() => AssertSuccess("ref_tests/ref_deref_copy_struct_test", 7);

    [Test] public void RefDerefMoveOutTwiceFailTest() => AssertFail<MoveOutOfReferenceError>("ref_tests/ref_deref_move_out_twice_fail_test");

    [Test] public void RefDerefGenericCopyTest() => AssertSuccess("ref_tests/ref_deref_generic_copy_test", 42);

    [Test] public void RefAutoDerefNonCopyTest() => AssertSuccess("ref_tests/ref_auto_deref_non_copy_test", 99);
}

public class RefLifetimeReturnTests
{
    [Test] public void RefReturnLocalExplicitFailTest() => AssertFail<DanglingReferenceError>("ref_tests/ref_return_local_explicit_fail_test");

    [Test] public void RefReturnParamTest() => AssertSuccess("ref_tests/ref_return_param_test", 55);

    [Test] public void RefReturnLocalImplicitFailTest() => AssertFail<DanglingReferenceError>("ref_tests/ref_return_local_implicit_fail_test");

    [Test] public void RefReturnLocalVarFailTest() => AssertFail<DanglingReferenceError>("ref_tests/ref_return_local_var_fail_test");

    [Test] public void RefReturnParamVarTest() => AssertSuccess("ref_tests/ref_return_param_var_test", 77);

    [Test] public void RefReturnMixedTest() => AssertFail<DanglingReferenceError>("ref_tests/ref_return_mixed_test");
}

public class RefBlockScopeTests
{
    [Test] public void RefBlockScopeEscapeFailTest() => AssertFail<DanglingReferenceError>("ref_tests/ref_block_scope_escape_fail_test");

    [Test] public void RefBlockScopeValueTest() => AssertSuccess("ref_tests/ref_block_scope_value_test", 5);

    [Test] public void RefBlockOuterScopeTest() => AssertSuccess("ref_tests/ref_block_outer_scope_test", 42);

    [Test] public void RefMatchBlockEscapeFailTest() => AssertFail<DanglingReferenceError>("ref_tests/ref_match_block_escape_fail_test");

    [Test] public void RefNestedBlockEscapeFailTest() => AssertFail<DanglingReferenceError>("ref_tests/ref_nested_block_escape_fail_test");
}

public class RefConstTests
{
    [Test] public void RefConstBasicTest() => AssertSuccess("ref_tests/ref_const_basic_test", 5);

    [Test] public void RefConstMultipleTest() => AssertSuccess("ref_tests/ref_const_multiple_test", 10);

    [Test] public void RefConstWithSharedTest() => AssertSuccess("ref_tests/ref_const_with_shared_test", 10);

    [Test] public void RefConstWithMutFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_const_with_mut_fail_test");
}

public class RefMutTests
{
    [Test] public void RefMutBasicTest() => AssertSuccess("ref_tests/ref_mut_basic_test", 5);

    [Test] public void RefMutMultipleTest() => AssertSuccess("ref_tests/ref_mut_multiple_test", 10);

    [Test] public void RefMutWithSharedTest() => AssertSuccess("ref_tests/ref_mut_with_shared_test", 10);

    [Test] public void RefMutWithConstFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_mut_with_const_fail_test");
}

public class RefUniqTests
{
    [Test] public void RefUniqBasicTest() => AssertSuccess("ref_tests/ref_uniq_basic_test", 5);

    [Test] public void RefUniqWithSharedFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_uniq_with_shared_fail_test");

    [Test] public void RefUniqWithConstFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_uniq_with_const_fail_test");

    [Test] public void RefUniqWithMutFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_uniq_with_mut_fail_test");

    [Test] public void RefUniqDoubleFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_uniq_double_fail_test");

    [Test] public void RefSharedAfterUniqFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_shared_after_uniq_fail_test");

    [Test] public void RefSharedMultipleTest() => AssertSuccess("ref_tests/ref_shared_multiple_test", 15);

    [Test] public void RefUniqAfterShadowTest() => AssertSuccess("ref_tests/ref_uniq_after_shadow_test", 10);
}

public class RefCopyTests
{
    [Test] public void RefUniqNotCopyFailTest()
    {
        // &uniq is not Copy (let-assignment moves), but passing to functions auto-reborrows.
        // This test passes &uniq to a function twice — succeeds via reborrow.
        CompilerTestHelper.AssertSuccess("ref_tests/ref_uniq_not_copy_fail_test", 5);
    }

    [Test] public void RefSharedIsCopyTest() => AssertSuccess("ref_tests/ref_shared_is_copy_test", 10);

    [Test] public void RefConstIsCopyTest() => AssertSuccess("ref_tests/ref_const_is_copy_test", 10);

    [Test] public void RefMutIsCopyTest() => AssertSuccess("ref_tests/ref_mut_is_copy_test", 10);
}

public class RefNllTests
{
    [Test] public void RefUniqThenMutNllPassTest() => AssertSuccess("ref_tests/ref_uniq_then_mut_nll_pass_test", 5);

    [Test] public void RefConstThenMutNllPassTest() => AssertSuccess("ref_tests/ref_const_then_mut_nll_pass_test", 5);

    [Test] public void RefUniqThenUniqNllPassTest() => AssertSuccess("ref_tests/ref_uniq_then_uniq_nll_pass_test", 5);
}

public class RefNllFailTests
{
    [Test] public void RefUniqThenMutUseOldFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_uniq_then_mut_use_old_fail_test");

    [Test] public void RefConstThenMutUseOldFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_const_then_mut_use_old_fail_test");

    [Test] public void RefUniqThenUniqUseOldFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_uniq_then_uniq_use_old_fail_test");

    [Test] public void RefUniqThenSharedUseOldFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_uniq_then_shared_use_old_fail_test");

    [Test] public void RefMutThenConstUseOldFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_mut_then_const_use_old_fail_test");

    [Test] public void RefUniqThenConstUseOldFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_uniq_then_const_use_old_fail_test");
}

public class RefStandaloneBorrowTests
{
    [Test] public void RefStandaloneUniqInvalidatesUniqFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_standalone_uniq_invalidates_uniq_fail_test");

    [Test] public void RefStandaloneUniqInvalidatesMutFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_standalone_uniq_invalidates_mut_fail_test");

    [Test] public void RefStandaloneUniqInvalidatesSharedFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_standalone_uniq_invalidates_shared_fail_test");

    [Test] public void RefStandaloneConstInvalidatesMutFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_standalone_const_invalidates_mut_fail_test");

    [Test] public void RefStandaloneMutInvalidatesConstFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_standalone_mut_invalidates_const_fail_test");

    [Test] public void RefStandaloneUniqNotUsedPassTest() => AssertSuccess("ref_tests/ref_standalone_uniq_not_used_pass_test", 5);

    [Test] public void RefStandaloneSharedCompatiblePassTest() => AssertSuccess("ref_tests/ref_standalone_shared_compatible_pass_test", 5);

    [Test] public void RefStandaloneMutCompatiblePassTest() => AssertSuccess("ref_tests/ref_standalone_mut_compatible_pass_test", 5);
}

public class RefLifetimeElisionTests
{
    [Test] public void RefElisionFnReturnInvalidatedFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_elision_fn_return_invalidated_fail_test");

    [Test] public void RefElisionFnReturnPassTest() => AssertSuccess("ref_tests/ref_elision_fn_return_pass_test", 5);

    [Test] public void RefElisionSharedInvalidatedFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_elision_shared_invalidated_fail_test");

    [Test] public void RefElisionSharedPassTest() => AssertSuccess("ref_tests/ref_elision_shared_pass_test", 5);

    [Test] public void RefElisionMethodInvalidatedFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_elision_method_invalidated_fail_test");

    [Test] public void RefElisionChainedFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_elision_chained_fail_test");
}

public class RefElisionAmbiguityTests
{
    [Test] public void RefElisionAmbiguousFailTest() => AssertFail<GenericSemanticError>("ref_tests/ref_elision_ambiguous_fail_test");

    [Test] public void RefElisionThreeRefsFailTest() => AssertFail<GenericSemanticError>("ref_tests/ref_elision_three_refs_fail_test");

    [Test] public void RefElisionNoRefReturnTest() => AssertSuccess("ref_tests/ref_elision_no_ref_return_test", 10);

    [Test] public void RefElisionOneRefParamTest() => AssertSuccess("ref_tests/ref_elision_one_ref_param_test", 10);

    [Test] public void RefElisionSelfWinsTest() => AssertSuccess("ref_tests/ref_elision_self_wins_test", 42);

    [Test] public void RefElisionGenericAmbiguousFailTest() => AssertFail<GenericSemanticError>("ref_tests/ref_elision_generic_ambiguous_fail_test");
}

public class RefExplicitLifetimeTests
{
    [Test] public void RefExplicitLifetimeTest() => AssertSuccess("ref_tests/ref_explicit_lifetime_test", 10);

    [Test] public void RefExplicitLifetimeSameTest() => AssertSuccess("ref_tests/ref_explicit_lifetime_same_test", 10);

    [Test] public void RefExplicitLifetimeInvalidateFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_explicit_lifetime_invalidate_fail_test");

    [Test] public void RefExplicitLifetimeOtherInvalidatePassTest() => AssertSuccess("ref_tests/ref_explicit_lifetime_other_invalidate_pass_test", 10);

    [Test] public void RefExplicitLifetimeBothBoundFailTest() => AssertFail<BorrowInvalidatedError>("ref_tests/ref_explicit_lifetime_both_bound_fail_test");

    [Test] public void RefExplicitLifetimeDifferentTest() => AssertSuccess("ref_tests/ref_explicit_lifetime_different_test", 10);

    [Test] public void RefUndeclaredLifetimeFailTest() => AssertFail<GenericSemanticError>("ref_tests/ref_undeclared_lifetime_fail_test");
}

public class RefTwoLifetimeTests
{
    [Test]
    public void RefTwoLifetimesUniqSecondPassTest()
    {
        // fn pick<'a, 'b>(x: &'a i32, y: &'b i32) -> &'a i32
        // r borrows from a ('a), &uniq b ('b) doesn't conflict — *r + *u works
        var result = Compile("ref_tests/ref_two_lifetimes_uniq_second_pass_test");
        Assert.That(result.Success);
        Assert.That(30 == result.Function?.Invoke());
    }
}

public class RefLifetimeValidationTests
{
    [Test] public void RefReturnWrongLifetimeFailTest() => AssertFail<GenericSemanticError>("ref_tests/ref_return_wrong_lifetime_fail_test");

    [Test] public void RefReturnCorrectLifetimeTest() => AssertSuccess("ref_tests/ref_return_correct_lifetime_test", 20);

    [Test] public void RefTraitMethodLifetimeTest() => AssertSuccess("ref_tests/ref_trait_method_lifetime_test");

    [Test] public void RefTraitMethodTwoLifetimesTest() => AssertSuccess("ref_tests/ref_trait_method_two_lifetimes_test");
}

public class RefLifetimeReturnValidationTests
{
    [Test] public void RefLifetimeReturnMismatchFailTest() => AssertFail<GenericSemanticError>("ref_tests/ref_lifetime_return_mismatch_fail_test");

    [Test] public void RefLifetimeReturnCorrectTest() => AssertSuccess("ref_tests/ref_lifetime_return_correct_test", 20);

    [Test] public void RefTraitLifetimeParseTest() => AssertSuccess("ref_tests/ref_trait_lifetime_parse_test");

    [Test] public void RefTraitLifetimeImplTest() => AssertSuccess("ref_tests/ref_trait_lifetime_impl_test", 10);
}

public class RefReturnLifetimeTests
{
    [Test] public void RefReturnLifetimeMismatchFailTest() => AssertFail<GenericSemanticError>("ref_tests/ref_return_lifetime_mismatch_fail_test");

    [Test] public void RefReturnLifetimeMatchTest() => AssertSuccess("ref_tests/ref_return_lifetime_match_test", 20);

    [Test] public void RefTraitMethodLifetimeTest() => AssertSuccess("ref_tests/ref_trait_method_lifetime_test");

    [Test] public void RefTraitMethodMultiLifetimeTest() => AssertSuccess("ref_tests/ref_trait_method_multi_lifetime_test");
}

public class RefTraitLifetimeTests
{
    [Test] public void RefTraitUndeclaredLifetimeFailTest() => AssertFail<GenericSemanticError>("ref_tests/ref_trait_undeclared_lifetime_fail_test");

    [Test] public void RefTraitAmbiguousFailTest() => AssertFail<GenericSemanticError>("ref_tests/ref_trait_ambiguous_fail_test");

    [Test] public void RefTraitSelfElisionTest() => AssertSuccess("ref_tests/ref_trait_self_elision_test");

    [Test] public void RefTraitExplicitLifetimeTest() => AssertSuccess("ref_tests/ref_trait_explicit_lifetime_test");

    [Test] public void RefTraitReturnLifetimeOrphanFailTest() => AssertFail<GenericSemanticError>("ref_tests/ref_trait_return_lifetime_orphan_fail_test");
}

public class RefLifetimeE2eTests
{
    [Test]
    public void RefTraitLifetimeE2eFailTest()
    {
        // Trait method pick<'a>(&'a i32, &i32) -> &'a i32
        // Call pick(&x, &y), then &uniq x invalidates r — *r fails
        var result = Compile("ref_tests/ref_trait_lifetime_e2e_fail_test");
        Assert.That(!result.Success);
        Assert.That(result.HasError<BorrowInvalidatedError>());
    }

    [Test]
    public void RefTraitLifetimeE2ePassTest()
    {
        // Trait method pick<'a>(&'a i32, &i32) -> &'a i32
        // Call pick(&x, &y), then &uniq y — y has no lifetime link to return, so *r is fine
        var result = Compile("ref_tests/ref_trait_lifetime_e2e_pass_test");
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test] public void RefMethodCallLifetimeE2eTest() => AssertSuccess("ref_tests/ref_method_call_lifetime_e2e_test", 7);
}

public class RefUseWhileBorrowedTests
{
    [Test]
    public void RefUseWhileUniqBorrowedFailTest()
    {
        // let r = &uniq x; then x + *r — x is exclusively borrowed, can't be used
        var result = CompilerTestHelper.AssertFail<UseWhileBorrowedError>(
            "ref_tests/ref_use_while_uniq_borrowed_fail_test");
        var errors = result.GetErrors<UseWhileBorrowedError>().ToList();
        Assert.That(errors.Count >= 1);
        Assert.That(errors[0].Source == "x");
    }

    [Test]
    public void RefUseFieldWhileUniqBorrowedFailTest()
    {
        // let doo = &uniq foo; ... foo.dd.number — foo is exclusively borrowed, can't access fields
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>(
            "ref_tests/ref_use_field_while_uniq_borrowed_fail_test");
    }

    [Test]
    public void RefUniqBorrowScopeExitPassTest()
    {
        // &uniq x in inner scope, borrower exits, x usable again
        CompilerTestHelper.AssertSuccess("ref_tests/ref_uniq_borrow_scope_exit_pass_test", 15);
    }

    [Test]
    public void RefUseThroughRefWhileBorrowedPassTest()
    {
        // Using the reference (r.x + r.y) is fine — that's the whole point of borrowing
        CompilerTestHelper.AssertSuccess("ref_tests/ref_use_through_ref_while_borrowed_pass_test", 10);
    }

    [Test]
    public void RefSharedBorrowAllowsReadPassTest()
    {
        // &shared borrow does NOT prevent reading the source
        CompilerTestHelper.AssertSuccess("ref_tests/ref_shared_borrow_allows_read_pass_test", 84);
    }
}

public class RefAutoReborrowTests
{
    [Test]
    public void RefUniqFnReborrowPassTest()
    {
        // Passing &uniq to a function auto-reborrows instead of moving
        CompilerTestHelper.AssertSuccess("ref_tests/ref_uniq_fn_reborrow_pass_test", 10);
    }

    [Test]
    public void RefUniqFnReborrowMultiplePassTest()
    {
        // Can pass &uniq to functions multiple times (each is a reborrow)
        CompilerTestHelper.AssertSuccess("ref_tests/ref_uniq_fn_reborrow_multiple_pass_test", 15);
    }

    [Test]
    public void RefUniqLetStillMovesTest()
    {
        // let p = r still moves (reborrow only applies to fn/method args)
        CompilerTestHelper.AssertFail<UseAfterMoveError>("ref_tests/ref_uniq_let_still_moves_test");
    }
}

public class RefCreationBorrowConflictTests
{
    [Test]
    public void RefStructDoubleUniqFieldFailTest()
    {
        // Two &uniq borrows of the same variable in a struct literal — should fail
        CompilerTestHelper.AssertFail<BorrowConflictError>("ref_tests/ref_struct_double_uniq_field_fail_test");
    }

    [Test]
    public void RefEnumVariantDoubleUniqFailTest()
    {
        // Two &uniq borrows of the same variable in an enum variant — should fail
        CompilerTestHelper.AssertFail<BorrowConflictError>("ref_tests/ref_enum_variant_double_uniq_fail_test");
    }

    [Test]
    public void RefEnumNestedStructDoubleUniqFailTest()
    {
        // Two structs each holding &uniq of the same variable passed to an enum variant — should fail
        CompilerTestHelper.AssertFail<BorrowConflictError>("ref_tests/ref_enum_nested_struct_double_uniq_fail_test");
    }
}

public class MutReassignTests
{
    // ── Should PASS ──

    [Test]
    public void MutReassignPrimitivePassTest()
    {
        // *(&mut i32) = val — i32 implements MutReassign
        CompilerTestHelper.AssertSuccess("ref_tests/mut_reassign_primitive_pass_test", 42);
    }

    [Test]
    public void MutReassignUniqAlwaysPassTest()
    {
        // *(&uniq Foo) = val — &uniq can always reassign regardless of MutReassign
        CompilerTestHelper.AssertSuccess("ref_tests/mut_reassign_uniq_always_pass_test", 99);
    }

    [Test]
    public void MutReassignStructImplPassTest()
    {
        // Struct implementing MutReassign with all MutReassign fields
        CompilerTestHelper.AssertSuccess("ref_tests/mut_reassign_struct_impl_pass_test", 30);
    }

    [Test]
    public void MutReassignFlatEnumPassTest()
    {
        // Flat enum (no payload) implementing MutReassign
        CompilerTestHelper.AssertSuccess("ref_tests/mut_reassign_flat_enum_pass_test", 3);
    }

    [Test]
    public void MutReassignFnParamPassTest()
    {
        // MutReassign through &mut function parameter
        CompilerTestHelper.AssertSuccess("ref_tests/mut_reassign_fn_param_pass_test", 55);
    }

    // ── Should FAIL ──

    [Test]
    public void MutReassignStructNoImplFailTest()
    {
        // *(&mut Foo) = val where Foo doesn't implement MutReassign
        CompilerTestHelper.AssertFail<GenericSemanticError>("ref_tests/mut_reassign_struct_no_impl_fail_test");
    }

    [Test]
    public void MutReassignStructBadFieldFailTest()
    {
        // impl MutReassign for Outer where Inner field doesn't implement MutReassign
        CompilerTestHelper.AssertFail<GenericSemanticError>("ref_tests/mut_reassign_struct_bad_field_fail_test");
    }

    [Test]
    public void MutReassignPayloadEnumFailTest()
    {
        // impl MutReassign for enum with payload variants — enums must be flat
        CompilerTestHelper.AssertFail<GenericSemanticError>("ref_tests/mut_reassign_payload_enum_fail_test");
    }
}

public class RefEnumPatternMatchTests
{
    [Test]
    public void RefMatchSharedEnumTest()
    {
        // match &enum — binding is &shared, deref to read value
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_shared_enum_test", 42);
    }

    [Test]
    public void RefMatchMutEnumTest()
    {
        // match &mut enum — binding is &mut, can write through it
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_mut_enum_test", 77);
    }

    [Test]
    public void RefMatchUniqEnumTest()
    {
        // match &uniq enum — binding is &uniq, can write through it
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_uniq_enum_test", 99);
    }

    [Test]
    public void RefMatchGenericEnumTest()
    {
        // match &Option(i32) — generic enum through reference
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_generic_enum_test", 50);
    }

    [Test]
    public void RefMatchValueStillCopiesTest()
    {
        // match by value (no reference) — existing behavior preserved
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_value_still_copies_test", 42);
    }

    [Test]
    public void RefMatchUnitVariantTest()
    {
        // match &enum with unit variants — just dispatch, no bindings
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_unit_variant_test", 3);
    }

    // ── Multi-field reference matching ──

    [Test]
    public void RefMatchSharedMultiFieldTest()
    {
        // match &Pair.Two(a, b) — both a,b are &i32, deref and sum
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_shared_multi_field_test", 42);
    }

    [Test]
    public void RefMatchMutMultiFieldTest()
    {
        // match &mut Pair — modify both fields, verify changes
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_mut_multi_field_test", 24);
    }

    // ── Wildcard and dispatch ──

    [Test]
    public void RefMatchSharedWildcardTest()
    {
        // match &Color with wildcard — Red=1, Blue via wildcard=0
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_shared_wildcard_test", 1);
    }

    [Test]
    public void RefMatchRefDispatchAllVariantsTest()
    {
        // match &Dir for dx and dy — Dir.E → dx=1, dy=0
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_ref_dispatch_all_variants_test", 1);
    }

    // ── Generic and nested ──

    [Test]
    public void RefMatchSharedNestedGenericTest()
    {
        // Two &Option(i32) matched — Some(30)+Some(12)=42, Some(30)+None=30, total=72
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_shared_nested_generic_test", 72);
    }

    // ── Value then ref, ref then value ──

    [Test]
    public void RefMatchValueThenRefTest()
    {
        // Read by ref (50), then match by value (50) — total 100
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_value_then_ref_test", 100);
    }

    // ── Write through ref then read by value ──

    [Test]
    public void RefMatchUniqWriteThenReadTest()
    {
        // Set via &uniq twice (42, then 77), read by value — 77
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_uniq_write_then_read_test", 77);
    }

    [Test]
    public void RefMatchMutCallerSeesChangeTest()
    {
        // Increment Counter 3 times via &mut match — caller reads 3
        CompilerTestHelper.AssertSuccess("ref_tests/ref_match_mut_caller_sees_change_test", 3);
    }
}

public class RefBorrowThroughGenericTests
{
    [Test]
    public void RefBorrowThroughGenericFailTest()
    {
        // Struct with &uniq borrow passed through generic fn, then source reassigned — should fail
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>("ref_tests/ref_borrow_through_generic_fail_test");
    }

    [Test]
    public void RefBorrowThroughGenericPassTest()
    {
        // Struct with &uniq borrow passed through generic fn, consumed before source used — should pass
        CompilerTestHelper.AssertSuccess("ref_tests/ref_borrow_through_generic_pass_test", 5);
    }

    [Test]
    public void RefBorrowThroughChainedGenericFailTest()
    {
        // Borrow survives through Pass2(Pass1(h)) — two generic hops
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>("ref_tests/ref_borrow_through_chained_generic_fail_test");
    }

    [Test]
    public void RefNoBorrowThroughGenericPassTest()
    {
        // Non-borrowing struct through generic — no borrows to propagate
        CompilerTestHelper.AssertSuccess("ref_tests/ref_no_borrow_through_generic_pass_test", 42);
    }

    [Test]
    public void RefUniqBorrowThroughGenericFailTest()
    {
        // Struct with &uniq borrow passed through generic fn, then source reassigned — should fail
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>("ref_tests/ref_uniq_borrow_through_generic_fail_test");
    }

    [Test]
    public void RefCopyThroughGenericPassTest()
    {
        // Copy type through generic — no borrow tracking needed
        CompilerTestHelper.AssertSuccess("ref_tests/ref_copy_through_generic_pass_test", 84);
    }

    // ── Source returned / not returned ──

    [Test]
    public void RefBorrowSourceReturnedFailTest()
    {
        // Borrow source 'a' is returned while borrower is alive — should fail
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>("ref_tests/ref_borrow_source_returned_fail_test");
    }

    [Test]
    public void RefBorrowSourceNotReturnedPassTest()
    {
        // Borrow source 'a' is NOT returned (b is) — borrower is harmless
        CompilerTestHelper.AssertSuccess("ref_tests/ref_borrow_source_not_returned_pass_test", 10);
    }

    [Test]
    public void RefBorrowSourceExplicitReturnFailTest()
    {
        // return a; while borrower holds &uniq a
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>("ref_tests/ref_borrow_source_explicit_return_fail_test");
    }

    [Test]
    public void RefBorrowSourceUsedInExprFailTest()
    {
        // a + b where a is borrowed — using source in expression
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>("ref_tests/ref_borrow_source_used_in_expr_fail_test");
    }

    [Test]
    public void RefBorrowSourceReassignFailTest()
    {
        // a = 10 while borrower holds &uniq a
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>("ref_tests/ref_borrow_source_reassign_fail_test");
    }

    [Test]
    public void RefBorrowTwoSourcesOneReturnedPassTest()
    {
        // Two sources borrowed, neither used — third var returned
        CompilerTestHelper.AssertSuccess("ref_tests/ref_borrow_two_sources_one_returned_pass_test", 20);
    }

    [Test]
    public void RefBorrowConsumedThenSourceUsedPassTest()
    {
        // Borrower moved into DropIt(), source usable after
        CompilerTestHelper.AssertSuccess("ref_tests/ref_borrow_consumed_then_source_used_pass_test", 5);
    }

    [Test]
    public void RefBorrowInnerScopeSourceAfterPassTest()
    {
        // Borrower in inner scope, source used after scope exits
        CompilerTestHelper.AssertSuccess("ref_tests/ref_borrow_inner_scope_source_after_pass_test", 5);
    }

    [Test]
    public void RefBorrowSourcePassedToFnFailTest()
    {
        // read(a) while a is borrowed — passing source to function
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>("ref_tests/ref_borrow_source_passed_to_fn_fail_test");
    }

    [Test]
    public void RefBorrowDifferentVarReturnedPassTest()
    {
        // Source borrowed, unrelated var returned
        CompilerTestHelper.AssertSuccess("ref_tests/ref_borrow_different_var_returned_pass_test", 42);
    }

    // ── Generic propagation edge cases ──

    [Test]
    public void RefGenericReturnsDifferentTypePassTest()
    {
        // Generic fn consumes borrower, returns i32 — borrow released by move
        CompilerTestHelper.AssertSuccess("ref_tests/ref_generic_returns_different_type_pass_test", 5);
    }

    [Test]
    public void RefChainedGenericConsumedPassTest()
    {
        // Pass(Pass(h)) then DropIt(h2) — borrow released before source used
        CompilerTestHelper.AssertSuccess("ref_tests/ref_chained_generic_consumed_pass_test", 5);
    }

    [Test]
    public void RefGenericPassthroughReassignFailTest()
    {
        // Pass(h) then a = 99 — borrow survives generic, blocks reassign
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>("ref_tests/ref_generic_passthrough_reassign_fail_test");
    }

    // ── Drop vs non-Drop borrower behavior ──

    [Test]
    public void RefDropBorrowerPersistsFailTest()
    {
        // Drop type borrower — borrow persists until scope exit even without explicit use
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>("ref_tests/ref_drop_borrower_persists_fail_test");
    }

    [Test]
    public void RefNonDropBlockScopeReleasedPassTest()
    {
        // Non-drop borrower in block scope — borrow released at scope exit, source usable
        CompilerTestHelper.AssertSuccess("ref_tests/ref_nondrop_block_scope_released_pass_test", 47);
    }

    // ── Multi-field borrows ──

    [Test]
    public void RefMultiBorrowFieldsUntouchedPassTest()
    {
        // Struct borrows a and b, neither used — unrelated var returned
        CompilerTestHelper.AssertSuccess("ref_tests/ref_multi_borrow_fields_untouched_pass_test", 99);
    }

    [Test]
    public void RefMultiBorrowOneSourceUsedFailTest()
    {
        // Struct borrows a and b, b returned — b used while borrowed
        CompilerTestHelper.AssertFail<UseWhileBorrowedError>("ref_tests/ref_multi_borrow_one_source_used_fail_test");
    }
}
