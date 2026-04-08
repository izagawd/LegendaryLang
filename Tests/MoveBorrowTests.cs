using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

[TestFixture]
public class MoveBorrowTests
{
    // === FAIL: non-Copy struct borrows, source moved ===

    [Test] public void MoveWhileStructBorrowsFailTest() =>
        AssertFail<MoveWhileBorrowedError>("move_borrow_tests/move_while_struct_borrows_fail_test");

    [Test] public void ReturnWhileStructBorrowsFailTest() =>
        AssertFail<MoveWhileBorrowedError>("move_borrow_tests/return_while_struct_borrows_fail_test");

    [Test] public void MoveNestedBlockBorrowsFailTest() =>
        AssertFail<MoveWhileBorrowedError>("move_borrow_tests/move_nested_block_borrows_fail_test");

    [Test] public void TwoBorrowersMoveFailTest() =>
        AssertFail<MoveWhileBorrowedError>("move_borrow_tests/two_borrowers_move_fail_test");

    [Test] public void MoveWhileEnumBorrowsFailTest() =>
        AssertFail<MoveWhileBorrowedError>("move_borrow_tests/move_while_enum_borrows_fail_test");

    [Test] public void MoveWhileNestedStructBorrowsFailTest() =>
        AssertFail<MoveWhileBorrowedError>("move_borrow_tests/move_while_nested_struct_borrows_fail_test");

    // === FAIL: use borrow after source moved (NLL borrower) ===

    [Test] public void MoveThenUseBorrowerFailTest() =>
        AssertFail<BorrowInvalidatedError>("move_borrow_tests/move_then_use_borrower_fail_test");

    // === PASS: borrower gone before source moves ===

    [Test] public void StructBorrowsInnerScopeThenMoveOkTest() =>
        AssertSuccess("move_borrow_tests/struct_borrows_inner_scope_then_move_ok_test", 5);

    [Test] public void BorrowerConsumedThenMoveOkTest() =>
        AssertSuccess("move_borrow_tests/borrower_consumed_then_move_ok_test", 5);

    [Test] public void CopyStructBorrowsMoveOkTest() =>
        AssertSuccess("move_borrow_tests/copy_struct_borrows_move_ok_test", 5);

    [Test] public void DeepNestedScopeBorrowThenMoveOkTest() =>
        AssertSuccess("move_borrow_tests/deep_nested_scope_borrow_then_move_ok_test", 5);

    [Test] public void BorrowTemporaryConsumedThenMoveOkTest() =>
        AssertSuccess("move_borrow_tests/borrow_temporary_consumed_then_move_ok_test", 5);

    // === PASS: NLL-eligible (Copy/ref) borrowers ===

    [Test] public void RefBorrowsMoveAfterLastUseOkTest() =>
        AssertSuccess("move_borrow_tests/ref_borrows_move_after_last_use_ok_test", 5);

    [Test] public void GenericCopyBorrowsMoveOkTest() =>
        AssertSuccess("move_borrow_tests/generic_copy_borrows_move_ok_test", 42);

    [Test] public void GenericRefBorrowsMoveOkTest() =>
        AssertSuccess("move_borrow_tests/generic_ref_borrows_move_ok_test", 5);

    // === Explicit return while borrowed ===

    [Test] public void ExplicitReturnWhileBorrowedFailTest() =>
        AssertFail<MoveWhileBorrowedError>("move_borrow_tests/explicit_return_while_borrowed_fail_test");

    [Test] public void ExplicitReturnInNestedBlockWhileBorrowedFailTest() =>
        AssertFail<MoveWhileBorrowedError>("move_borrow_tests/explicit_return_in_nested_block_while_borrowed_fail_test");

    [Test] public void ExplicitReturnBorrowerDroppedFirstOkTest() =>
        AssertSuccess("move_borrow_tests/explicit_return_borrower_dropped_first_ok_test", 5);

    [Test] public void CopySourceBorrowedReturnOkTest() =>
        AssertSuccess("move_borrow_tests/copy_source_borrowed_return_ok_test", 5);

    [Test] public void UniqBorrowBlocksSourceUseFailTest() =>
        AssertFail<UseWhileBorrowedError>("move_borrow_tests/uniq_borrow_blocks_source_use_fail_test");
}
