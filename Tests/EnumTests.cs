using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class EnumTests
{
    [Test] public void EnumBasicTest() => AssertSuccess("enum_tests/enum_basic_test", 2);

    [Test] public void EnumTupleVariantTest() => AssertSuccess("enum_tests/enum_tuple_variant_test", 42);

    [Test] public void EnumGenericTest() => AssertSuccess("enum_tests/enum_generic_test", 7);

    [Test] public void EnumMultiGenericTest() => AssertSuccess("enum_tests/enum_multi_generic_test", 5);

    [Test] public void EnumWildcardTest() => AssertSuccess("enum_tests/enum_wildcard_test", 99);

    [Test] public void EnumNestedTest() => AssertSuccess("enum_tests/enum_nested_test", 10);

    [Test] public void EnumNonExhaustiveFailTest() => AssertFail<NonExhaustiveMatchError>("enum_tests/enum_non_exhaustive_fail_test");

    [Test] public void EnumMultiFieldVariantTest() => AssertSuccess("enum_tests/enum_multi_field_variant_test", 10);

    [Test] public void EnumFnArgTest() => AssertSuccess("enum_tests/enum_fn_arg_test", 15);

    [Test] public void EnumFnReturnTest() => AssertSuccess("enum_tests/enum_fn_return_test", 20);

    [Test] public void EnumDuplicateVariantFailTest() => AssertFail<GenericSemanticError>("enum_tests/enum_duplicate_variant_fail_test");

    [Test] public void EnumWrongFieldCountFailTest() => AssertFail<GenericSemanticError>("enum_tests/enum_wrong_field_count_fail_test");

    [Test] public void EnumMatchReturnInArmTest() => AssertSuccess("enum_tests/enum_match_return_in_arm_test");

    [Test] public void EnumMatchAsExprTest() => AssertSuccess("enum_tests/enum_match_as_expr_test", 2);

    // ── Pattern matching computation ──

    [Test]
    public void EnumMatchAllArmsComputeTest()
    {
        // Add(10,5)=15 + Mul(3,4)=12 + Neg(3)=-3 = 24
        AssertSuccess("enum_tests/enum_match_all_arms_compute_test", 24);
    }

    [Test]
    public void EnumMatchReturnStructTest()
    {
        // Match returns struct from arm — Rect(3,7) → Point{3,7} → 10
        AssertSuccess("enum_tests/enum_match_return_struct_test", 10);
    }

    [Test]
    public void EnumMatchEarlyReturnArmTest()
    {
        // return keyword in match arm
        AssertSuccess("enum_tests/enum_match_early_return_arm_test", 42);
    }

    [Test]
    public void EnumGenericPassthroughMatchTest()
    {
        // Enum passed through generic function then matched
        AssertSuccess("enum_tests/enum_generic_passthrough_match_test", 42);
    }

    // ── Enum move semantics ──

    [Test]
    public void EnumUseAfterMoveMatchFailTest()
    {
        // consume(w) then consume(w) — use after move
        AssertFail<UseAfterMoveError>("enum_tests/enum_use_after_move_match_fail_test");
    }

    // ── Enum borrow rules ──

    [Test]
    public void EnumBorrowBlocksSourceFailTest()
    {
        // Enum holds &uniq x, then x used — blocked
        AssertFail<UseWhileBorrowedError>("enum_tests/enum_borrow_blocks_source_fail_test");
    }

    [Test]
    public void EnumBorrowReleasedAfterMoveTest()
    {
        // Enum holds &uniq x, moved into DropNow, then x used — OK
        AssertSuccess("enum_tests/enum_borrow_released_after_move_test", 5);
    }

    [Test]
    public void EnumBorrowReleasedScopeExitTest()
    {
        // Enum holds &uniq x in inner scope, x used after scope — OK
        AssertSuccess("enum_tests/enum_borrow_released_scope_exit_test", 5);
    }

    [Test]
    public void EnumBorrowSourceUntouchedPassTest()
    {
        // Enum holds &uniq x, x never used, different var returned — OK
        AssertSuccess("enum_tests/enum_borrow_source_untouched_pass_test", 99);
    }
}
