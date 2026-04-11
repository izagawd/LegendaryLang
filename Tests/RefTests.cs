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

public class RefMutTests
{
    [Test] public void RefMutBasicTest() => AssertSuccess("ref_tests/ref_mut_basic_test", 5);

    [Test] public void RefMutMultipleTest() => AssertSuccess("ref_tests/ref_mut_multiple_test", 10);

    [Test] public void RefMutWithSharedTest() => AssertSuccess("ref_tests/ref_mut_with_shared_test", 10);

}

public class RefUniqTests
{
    [Test] public void RefUniqBasicTest() => AssertSuccess("ref_tests/ref_uniq_basic_test", 5);
    

    [Test] public void RefSharedMultipleTest() => AssertSuccess("ref_tests/ref_shared_multiple_test", 15);

    [Test] public void RefUniqAfterShadowTest() => AssertSuccess("ref_tests/ref_uniq_after_shadow_test", 10);
}

public class RefCopyTests
{

    [Test] public void RefSharedIsCopyTest() => AssertSuccess("ref_tests/ref_shared_is_copy_test", 10);



    [Test] public void RefMutIsCopyTest() => AssertSuccess("ref_tests/ref_mut_is_copy_test", 10);
}

public class RefNllTests
{
    [Test] public void RefUniqThenMutNllPassTest() => AssertSuccess("ref_tests/ref_uniq_then_mut_nll_pass_test", 5);



    [Test] public void RefUniqThenUniqNllPassTest() => AssertSuccess("ref_tests/ref_uniq_then_uniq_nll_pass_test", 5);
}


public class RefStandaloneBorrowTests
{

    [Test] public void RefStandaloneUniqNotUsedPassTest() => AssertSuccess("ref_tests/ref_standalone_uniq_not_used_pass_test", 5);

    [Test] public void RefStandaloneSharedCompatiblePassTest() => AssertSuccess("ref_tests/ref_standalone_shared_compatible_pass_test", 5);

    [Test] public void RefStandaloneMutCompatiblePassTest() => AssertSuccess("ref_tests/ref_standalone_mut_compatible_pass_test", 5);
}

public class RefLifetimeElisionTests
{
    [Test] public void RefElisionFnReturnPassTest() => AssertSuccess("ref_tests/ref_elision_fn_return_pass_test", 5);

    [Test] public void RefElisionSharedPassTest() => AssertSuccess("ref_tests/ref_elision_shared_pass_test", 5);

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
    [Test] public void RefExplicitLifetimeOtherInvalidatePassTest() => AssertSuccess("ref_tests/ref_explicit_lifetime_other_invalidate_pass_test", 10);

    [Test] public void RefExplicitLifetimeDifferentTest() => AssertSuccess("ref_tests/ref_explicit_lifetime_different_test", 10);

    [Test] public void RefUndeclaredLifetimeFailTest() => AssertFail<GenericSemanticError>("ref_tests/ref_undeclared_lifetime_fail_test");
}

public class RefTwoLifetimeTests
{
    [Test]
    public void RefTwoLifetimesUniqSecondPassTest()
    {
        // fn pick<'a, 'b>(x: &'a i32, y: &'b i32) -> &'a i32
        // r borrows from a ('a), &mut b ('b) doesn't conflict — *r + *u works
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
    public void RefTraitLifetimeE2ePassTest()
    {
        // Trait method pick<'a>(&'a i32, &i32) -> &'a i32
        // Call pick(&x, &y), then &mut y — y has no lifetime link to return, so *r is fine
        var result = Compile("ref_tests/ref_trait_lifetime_e2e_pass_test");
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test] public void RefMethodCallLifetimeE2eTest() => AssertSuccess("ref_tests/ref_method_call_lifetime_e2e_test", 7);
}

public class RefUseWhileBorrowedTests
{

    [Test]
    public void RefUniqBorrowScopeExitPassTest()
    {
        // &mut x in inner scope, borrower exits, x usable again
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
        // Passing &mut to a function auto-reborrows instead of moving
        CompilerTestHelper.AssertSuccess("ref_tests/ref_uniq_fn_reborrow_pass_test", 10);
    }

    [Test]
    public void RefUniqFnReborrowMultiplePassTest()
    {
        // Can pass &mut to functions multiple times (each is a reborrow)
        CompilerTestHelper.AssertSuccess("ref_tests/ref_uniq_fn_reborrow_multiple_pass_test", 15);
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
        // match &mut enum — binding is &mut, can write through it
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
        // Set via &mut twice (42, then 77), read by value — 77
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
    public void RefBorrowThroughGenericPassTest()
    {
        // Struct with &mut borrow passed through generic fn, consumed before source used — should pass
        CompilerTestHelper.AssertSuccess("ref_tests/ref_borrow_through_generic_pass_test", 5);
    }


    [Test]
    public void RefNoBorrowThroughGenericPassTest()
    {
        // Non-borrowing struct through generic — no borrows to propagate
        CompilerTestHelper.AssertSuccess("ref_tests/ref_no_borrow_through_generic_pass_test", 42);
    }


    [Test]
    public void RefCopyThroughGenericPassTest()
    {
        // Copy type through generic — no borrow tracking needed
        CompilerTestHelper.AssertSuccess("ref_tests/ref_copy_through_generic_pass_test", 84);
    }


    [Test]
    public void RefBorrowSourceNotReturnedPassTest()
    {
        // Borrow source 'a' is NOT returned (b is) — borrower is harmless
        CompilerTestHelper.AssertSuccess("ref_tests/ref_borrow_source_not_returned_pass_test", 10);
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



    [Test] public void RefReturnNoRefParamFailTest() =>
        AssertFail<GenericSemanticError>("ref_tests/ref_return_no_ref_param_fail_test");

    [Test] public void RefTraitReturnNoRefParamFailTest() =>
        AssertFail<GenericSemanticError>("ref_tests/ref_trait_return_no_ref_param_fail_test");

    [Test] public void RefReturnValueSelfNoRefFailTest() =>
        AssertFail<GenericSemanticError>("ref_tests/ref_return_value_self_no_ref_fail_test");

    [Test] public void RefReturnSingleRefParamOkTest() =>
        AssertSuccess("ref_tests/ref_return_single_ref_param_ok_test");
}

public class SharedRefImmutabilityTests
{
    [Test] public void SharedRefDerefAssignFailTest() =>
        CompilerTestHelper.AssertFail<GenericSemanticError>("ref_tests/shared_ref_deref_assign_fail_test");

    [Test] public void SharedRefFieldAssignFailTest() =>
        CompilerTestHelper.AssertFail<GenericSemanticError>("ref_tests/shared_ref_field_assign_fail_test");

    [Test] public void MutRefDerefAssignPassTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/mut_ref_deref_assign_pass_test", 42);

    [Test] public void MutRefFieldAssignPassTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/mut_ref_field_assign_pass_test", 42);

    [Test] public void SharedRefNestedFieldAssignFailTest() =>
        CompilerTestHelper.AssertFail<GenericSemanticError>("ref_tests/shared_ref_nested_field_assign_fail_test");
}

public class DoubleRefMutabilityTests
{
    [Test] public void DoubleRefSharedMutCanMutateTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/double_ref_shared_mut_can_mutate_test", 42);

    [Test] public void DoubleRefMutSharedCannotMutateFailTest() =>
        CompilerTestHelper.AssertFail<GenericSemanticError>("ref_tests/double_ref_mut_shared_cannot_mutate_fail_test");

    [Test] public void DoubleRefSharedMutFieldAssignTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/double_ref_shared_mut_field_assign_test", 42);

    [Test] public void DoubleRefMutSharedFieldAssignFailTest() =>
        CompilerTestHelper.AssertFail<GenericSemanticError>("ref_tests/double_ref_mut_shared_field_assign_fail_test");

    [Test] public void DoubleRefSharedMutMethodTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/double_ref_shared_mut_method_test", 42);
}

public class RefMethodDispatchTests
{
    [Test] public void SharedRefSharedMethodPassTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/shared_ref_shared_method_pass_test", 42);

    [Test] public void SharedRefMutMethodFailTest() =>
        CompilerTestHelper.AssertFail<GenericSemanticError>("ref_tests/shared_ref_mut_method_fail_test");

    [Test] public void MutRefSharedMethodPassTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/mut_ref_shared_method_pass_test", 42);

    [Test] public void MutRefMutMethodPassTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/mut_ref_mut_method_pass_test", 42);

    [Test] public void GcSharedMethodPassTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/gc_shared_method_pass_test", 42);

    [Test] public void GcMutMethodPassTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/gc_mut_method_pass_test", 42);

    [Test] public void RefGcSharedMethodPassTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/ref_gc_shared_method_pass_test", 42);

    [Test] public void RefGcMutMethodPassTest() =>
        CompilerTestHelper.AssertSuccess("ref_tests/ref_gc_mut_method_pass_test", 42);
}
