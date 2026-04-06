using static Tests.CompilerTestHelper;
using LegendaryLang;
using NUnit.Framework;

namespace Tests;

[TestFixture]
public class BorrowLifetimeTests
{
    // ═══════════════════════════════════════════════════════════════
    //  STRUCT WITH LIFETIME PARAM — DIRECT CONSTRUCTION
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BorrowStructLifetimeBlocksUseFailTest()
    {
        // Holder['a] { val: &'a uniq i32 } borrows x → can't use x while h alive
        AssertFail<UseWhileBorrowedError>("borrow_lifetime_tests/borrow_struct_lifetime_blocks_use_fail_test");
    }

    [Test]
    public void BorrowStructLifetimeNllPassTest()
    {
        // Holder consumed before x accessed → NLL allows it = 10
        AssertSuccess("borrow_lifetime_tests/borrow_struct_lifetime_nll_pass_test", 10);
    }

    [Test]
    public void BorrowStructLifetimeReleasedAfterMoveTest()
    {
        // Holder moved to consume() → borrow released → x accessible = 10
        AssertSuccess("borrow_lifetime_tests/borrow_struct_lifetime_released_after_move_test", 10);
    }

    [Test]
    public void BorrowStructLifetimeReassignBlocksFailTest()
    {
        // Holder borrows x → can't reassign x while h alive
        AssertFail<UseWhileBorrowedError>("borrow_lifetime_tests/borrow_struct_lifetime_reassign_blocks_fail_test");
    }

    // ═══════════════════════════════════════════════════════════════
    //  STRUCT WITH LIFETIME PARAM — FUNCTION RETURN
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BorrowStructLifetimeFnReturnBlocksFailTest()
    {
        // wrap(&uniq x) returns Holder['a] → x blocked while h alive
        AssertFail<UseWhileBorrowedError>("borrow_lifetime_tests/borrow_struct_lifetime_fn_return_blocks_fail_test");
    }

    [Test]
    public void BorrowStructLifetimeFnReturnNllPassTest()
    {
        // wrap(&uniq x) returns Holder['a] → h consumed before x accessed = 10
        AssertSuccess("borrow_lifetime_tests/borrow_struct_lifetime_fn_return_nll_pass_test", 10);
    }

    // ═══════════════════════════════════════════════════════════════
    //  ENUM WITH LIFETIME PARAM
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BorrowEnumLifetimeBlocksUseFailTest()
    {
        // Maybe.Some(Holder{&uniq x}) → can't use x while m alive
        AssertFail<UseWhileBorrowedError>("borrow_lifetime_tests/borrow_enum_lifetime_blocks_use_fail_test");
    }

    // ═══════════════════════════════════════════════════════════════
    //  NON-COPY TYPES: BORROW PERSISTS UNTIL SCOPE EXIT OR MOVE
    //  (Drop could access borrowed data at scope exit)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BorrowNoncopyPersistsUntilScopeFailTest()
    {
        // Non-Copy Holder borrows x via &uniq. h is never used again,
        // but still in scope — Drop could fire at scope exit → x locked
        AssertFail<UseWhileBorrowedError>("borrow_lifetime_tests/borrow_noncopy_persists_until_scope_fail_test");
    }

    [Test]
    public void BorrowNoncopyConsumedReleasesPassTest()
    {
        // Non-Copy Holder consumed by DropNow() → borrow released → x = 10
        AssertSuccess("borrow_lifetime_tests/borrow_noncopy_consumed_releases_pass_test", 10);
    }

    [Test]
    public void BorrowNoncopyInnerScopeReleasesPassTest()
    {
        // Non-Copy Holder goes out of scope in inner block → borrow released → x = 10
        AssertSuccess("borrow_lifetime_tests/borrow_noncopy_inner_scope_releases_pass_test", 10);
    }

    [Test]
    public void BorrowNoncopyReassignWhileAliveFailTest()
    {
        // Non-Copy Holder in scope → can't reassign x
        AssertFail<UseWhileBorrowedError>("borrow_lifetime_tests/borrow_noncopy_reassign_while_alive_fail_test");
    }

    [Test]
    public void BorrowNoncopyNllNotEnoughFailTest()
    {
        // Non-Copy Holder's last use is before x access, BUT h is still in scope.
        // NLL alone is NOT enough — Drop runs at scope exit, could access x.
        AssertFail<UseWhileBorrowedError>("borrow_lifetime_tests/borrow_noncopy_nll_not_enough_fail_test");
    }

    // ═══════════════════════════════════════════════════════════════
    //  REFERENCES & COPY TYPES: NLL APPLIES (no Drop concern)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BorrowRefNllExpiresAtLastUsePassTest()
    {
        // &uniq x — reference (Copy, no Drop). Last use of r before x access.
        // NLL expires the borrow → x accessible = 20
        AssertSuccess("borrow_lifetime_tests/borrow_ref_nll_expires_at_last_use_pass_test", 20);
    }

    [Test]
    public void BorrowRefNllStillAliveFailTest()
    {
        // &uniq x — r is used AFTER x is accessed → borrow still active → fail
        AssertFail<UseWhileBorrowedError>("borrow_lifetime_tests/borrow_ref_nll_still_alive_fail_test");
    }

    [Test]
    public void BorrowCopyNoRestrictionPassTest()
    {
        // &x (shared ref, Copy) — no exclusive borrow, no restriction = 20
        AssertSuccess("borrow_lifetime_tests/borrow_copy_no_restriction_pass_test", 20);
    }
}
