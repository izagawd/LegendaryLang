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
}
