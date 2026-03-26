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
