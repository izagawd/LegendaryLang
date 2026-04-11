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
    public void BorrowStructLifetimeFnReturnNllPassTest()
    {
        // wrap(&mut x) returns Holder['a] → h consumed before x accessed = 10
        AssertSuccess("borrow_lifetime_tests/borrow_struct_lifetime_fn_return_nll_pass_test", 10);
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




    // ═══════════════════════════════════════════════════════════════
    //  REFERENCES & COPY TYPES: NLL APPLIES (no Drop concern)
    // ═══════════════════════════════════════════════════════════════

    [Test]
    public void BorrowRefNllExpiresAtLastUsePassTest()
    {
        // &mut x — reference (Copy, no Drop). Last use of r before x access.
        // NLL expires the borrow → x accessible = 20
        AssertSuccess("borrow_lifetime_tests/borrow_ref_nll_expires_at_last_use_pass_test", 20);
    }



    [Test]
    public void BorrowCopyNoRestrictionPassTest()
    {
        // &x (shared ref, Copy) — no exclusive borrow, no restriction = 20
        AssertSuccess("borrow_lifetime_tests/borrow_copy_no_restriction_pass_test", 20);
    }
}
