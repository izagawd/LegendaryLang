using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class DropTests
{
    // ═══════════════════════════════════════════════════════════════
    //  BASIC DROP FUNCTIONALITY
    // ═══════════════════════════════════════════════════════════════

    [Test] public void DropBasicTest() => AssertSuccess("drop_tests/drop_basic_test", 1);
    [Test] public void DropScopeExitTest() => AssertSuccess("drop_tests/drop_scope_exit_test", 10);
    [Test] public void DropNestedScopeTest() => AssertSuccess("drop_tests/drop_nested_scope_test", 2);
    [Test] public void DropStructWithRefTest() => AssertSuccess("drop_tests/drop_struct_with_ref_test", 100);
    [Test] public void DropMethodCallSelfTest() => AssertSuccess("drop_tests/drop_method_call_self_test", 7);
    [Test] public void DropMultipleVarsTest() => AssertSuccess("drop_tests/drop_multiple_vars_test", 3);
    [Test] public void DropInnerScopeOnlyTest() => AssertSuccess("drop_tests/drop_inner_scope_only_test", 2);
    [Test] public void DropOrderReverseTest() => AssertSuccess("drop_tests/drop_order_reverse_test", 2);
    [Test] public void DropNestedStructDropTest() => AssertSuccess("drop_tests/drop_nested_struct_drop_test", 15);

    // ═══════════════════════════════════════════════════════════════
    //  DROP + FUNCTION PARAMETERS
    // ═══════════════════════════════════════════════════════════════

    [Test] public void DropFnParamTest() => AssertSuccess("drop_tests/drop_fn_param_test", 5);
    [Test] public void DropGenericFnDropBoundTest() => AssertSuccess("drop_tests/drop_generic_fn_drop_bound_test", 1);

    // ═══════════════════════════════════════════════════════════════
    //  DROP + MOVE SEMANTICS
    // ═══════════════════════════════════════════════════════════════

    [Test] public void DropMoveNoDoubleDropTest() => AssertSuccess("drop_tests/drop_move_no_double_drop_test", 1);
    [Test] public void DropMoveFnCallTest() => AssertSuccess("drop_tests/drop_move_fn_call_test", 1);
    [Test] public void DropReassignTest() => AssertSuccess("drop_tests/drop_reassign_test", 2);

    // ═══════════════════════════════════════════════════════════════
    //  NO DROP — TYPES WITHOUT DROP IMPL
    // ═══════════════════════════════════════════════════════════════

    [Test] public void DropCopyTypeNoDropTest() => AssertSuccess("drop_tests/drop_copy_type_no_drop_test", 10);
    [Test] public void DropNoImplNoCallTest() => AssertSuccess("drop_tests/drop_no_impl_no_call_test", 42);

    // ═══════════════════════════════════════════════════════════════
    //  GENERIC STRUCT + DROP
    // ═══════════════════════════════════════════════════════════════

    [Test] public void DropGenericStructTest() => AssertSuccess("drop_tests/drop_generic_struct_test", 1);
    [Test] public void DropGenericStructBoundMatchTest() => AssertSuccess("drop_tests/drop_generic_struct_bound_match_test", 1);

    // ═══════════════════════════════════════════════════════════════
    //  COPY + DROP MUTUAL EXCLUSION (should all FAIL)
    // ═══════════════════════════════════════════════════════════════

    [Test] public void DropCopyConflictTest() => AssertFail<CopyDropConflictError>("drop_tests/drop_copy_conflict_test");
    [Test] public void DropCopyConflictReverseTest() => AssertFail<CopyDropConflictError>("drop_tests/drop_copy_conflict_reverse_test");
    [Test] public void DropCopyStructTryDropTest() => AssertFail<CopyDropConflictError>("drop_tests/drop_copy_struct_try_drop_test");
    [Test] public void DropEnumCopyConflictTest() => AssertFail<CopyDropConflictError>("drop_tests/drop_enum_copy_conflict_test");

    // ═══════════════════════════════════════════════════════════════
    //  DROP GENERIC CONSTRAINTS MUST MATCH TYPE (should all FAIL)
    // ═══════════════════════════════════════════════════════════════

    [Test] public void DropGenericsMismatchCountTest() => AssertFail<DropGenericsMismatchError>("drop_tests/drop_generics_mismatch_count_test");
    [Test] public void DropGenericsMismatchBoundsTest() => AssertFail<DropGenericsMismatchError>("drop_tests/drop_generics_mismatch_bounds_test");
    [Test] public void DropGenericsExtraBoundsTest() => AssertFail<DropGenericsMismatchError>("drop_tests/drop_generics_extra_bounds_test");
    [Test] public void DropGenericsMissingBoundsTest() => AssertFail<DropGenericsMismatchError>("drop_tests/drop_generics_missing_bounds_test");
    [Test] public void DropNonGenericNoExtraGenericsTest() => AssertFail<DropGenericsMismatchError>("drop_tests/drop_non_generic_no_extra_generics_test");

    // ═══════════════════════════════════════════════════════════════
    //  GENERIC CONSTRAINTS MATCH — SHOULD PASS
    // ═══════════════════════════════════════════════════════════════

    [Test] public void DropGenericsMatchTest() => AssertSuccess("drop_tests/drop_generics_match_test");

    // ═══════════════════════════════════════════════════════════════
    //  ENUM TESTS
    // ═══════════════════════════════════════════════════════════════

    [Test] public void DropEnumTest() => AssertSuccess("drop_tests/drop_enum_test", 1);
    [Test] public void DropEnumVariantPayloadTest() => AssertSuccess("drop_tests/drop_enum_variant_payload_test", 1);
    [Test] public void DropEnumInactiveVariantTest() => AssertSuccess("drop_tests/drop_enum_inactive_variant_test", 0);

    // ═══════════════════════════════════════════════════════════════
    //  BOX DESTRUCT TESTS — DestructPtr intrinsic
    // ═══════════════════════════════════════════════════════════════

    [Test] public void DropBoxDestructsInnerTest() => AssertSuccess("drop_tests/drop_box_destructs_inner_test", 1);
    [Test] public void DropBoxDestructsNestedFieldTest() => AssertSuccess("drop_tests/drop_box_destructs_nested_field_test", 1);

    // ═══════════════════════════════════════════════════════════════
    //  RETURN VALUE OWNERSHIP — returned values must NOT be dropped
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void DropReturnImplicitNoDropTest()
    {
        // fn passthrough(d) -> Dropper { d } — implicit return, param not dropped
        AssertSuccess("drop_tests/drop_return_implicit_no_drop_test", 10);
    }

    [Test]
    public void DropReturnExplicitNoDropTest()
    {
        // fn passthrough(d) -> Dropper { return d; } — explicit return keyword, param not dropped
        AssertSuccess("drop_tests/drop_return_explicit_no_drop_test", 10);
    }

    [Test]
    public void DropBlockLocalDropsValueSurvivesTest()
    {
        // let val = { let d = Dropper{...}; 5 } — d drops (+1), val=5, total=6
        AssertSuccess("drop_tests/drop_block_local_drops_value_survives_test", 6);
    }

    [Test]
    public void DropBlockReturnsDroppableTest()
    {
        // let d = { make Dropper{...} } — Dropper escapes block, not dropped in block, drops on consume
        AssertSuccess("drop_tests/drop_block_returns_droppable_test", 1);
    }

    [Test]
    public void DropNestedBlockReturnTest()
    {
        // let d = { { make Dropper{...} } } — passes through two nested blocks, single drop
        AssertSuccess("drop_tests/drop_nested_block_return_test", 1);
    }

    [Test]
    public void DropNonReturnedLocalDropsTest()
    {
        // fn returns 42 but has local Dropper — local drops (+1), val=42, total=43
        AssertSuccess("drop_tests/drop_non_returned_local_drops_test", 43);
    }

    [Test]
    public void DropParamNotReturnedDropsTest()
    {
        // fn takes Dropper, returns 77 — param drops at function exit
        AssertSuccess("drop_tests/drop_param_not_returned_drops_test", 1);
    }

    [Test]
    public void DropChainedPassthroughTest()
    {
        // pass3(pass2(pass1(d))) — three passthroughs, single drop at the end
        AssertSuccess("drop_tests/drop_chained_passthrough_test", 1);
    }

    [Test]
    public void DropStructWithDroppableFieldReturnedTest()
    {
        // fn returns Wrapper{inner: Dropper} — struct not dropped in fn, field drops when wrapper dies
        AssertSuccess("drop_tests/drop_struct_with_droppable_field_returned_test", 1);
    }

    [Test]
    public void DropMixedReturnAndLocalTest()
    {
        // keep_first(a, b) returns a, b drops in fn — each counter incremented once
        AssertSuccess("drop_tests/drop_mixed_return_and_local_test", 2);
    }

    [Test]
    public void DropEarlyReturnLocalsDropTest()
    {
        // fn has local Dropper, then return new Dropper — local drops, returned doesn't
        AssertSuccess("drop_tests/drop_early_return_locals_drop_test", 2);
    }
}
