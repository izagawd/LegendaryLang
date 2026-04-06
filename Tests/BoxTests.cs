using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class BoxTests
{
    // ── Basic Box operations ──

    [Test] public void BoxBasicTest() => AssertSuccess("box_tests/box_basic_test", 42);

    [Test] public void BoxDropTest() => AssertSuccess("box_tests/box_drop_test", 5);

    [Test] public void BoxStructTest() => AssertSuccess("box_tests/box_struct_test", 42);

    [Test] public void BoxMoveTest() => AssertSuccess("box_tests/box_move_test", 77);

    [Test] public void RawPtrBasicTest() => AssertSuccess("box_tests/rawptr_basic_test", 42);

    // ── Deref assign ──

    [Test]
    public void BoxDerefAssignTest()
    {
        // *b = 99 should write to heap, *b should read 99
        AssertSuccess("box_tests/box_deref_assign_test", 99);
    }

    [Test]
    public void BoxDerefReadAfterAssignTest()
    {
        // *b = Pair{10,32}, (*b).x + (*b).y = 42
        AssertSuccess("box_tests/box_deref_read_after_assign_test", 42);
    }

    // ── Use-after-free prevention ──

    [Test]
    public void BoxReturnDerefTest()
    {
        // fn get_val(b: Box<i32>) -> i32 { *b }
        // Return value must survive Box.Drop
        AssertSuccess("box_tests/box_return_deref_test", 42);
    }

    [Test]
    public void BoxDropScopeTest()
    {
        // Value from *b inside inner scope must survive scope exit
        AssertSuccess("box_tests/box_drop_scope_test", 42);
    }

    // ── Multiple operations ──

    [Test]
    public void BoxMultipleDerefTest()
    {
        // Multiple *b reads on same box (i32 is Copy): 7+7+7 = 21
        AssertSuccess("box_tests/box_multiple_deref_test", 21);
    }

    [Test]
    public void BoxTwoBoxesTest()
    {
        // Two boxes in same scope, both dropped correctly: 10+32 = 42
        AssertSuccess("box_tests/box_two_boxes_test", 42);
    }

    [Test]
    public void BoxNestedStructTest()
    {
        // Box<Outer> with nested structs, field access through deref: 10+32 = 42
        AssertSuccess("box_tests/box_nested_struct_test", 42);
    }
}

public class DropFieldTests
{
    [Test] public void BoxFieldDropTest() => AssertSuccess("box_tests/box_field_drop_test", 42);

    [Test] public void BoxNestedDropTest() => AssertSuccess("box_tests/box_nested_drop_test", 42);

    [Test]
    public void DropTransitiveFieldTest()
    {
        // A(Drop +10) -> B(no Drop) -> C(Drop +1) = 11
        AssertSuccess("box_tests/drop_transitive_field_test", 11);
    }

    [Test]
    public void DropTransitiveNoParentTest()
    {
        // A(no Drop) -> B(no Drop) -> C(Drop +1) = 1
        AssertSuccess("box_tests/drop_transitive_no_parent_test", 1);
    }

    [Test]
    public void DropMultiFieldTest()
    {
        // Multi(no Drop) has two Dropper fields (each +1) = 2
        AssertSuccess("box_tests/drop_multi_field_test", 2);
    }

    // ── New features: auto-deref, &*box, non-Copy field access ──

    [Test]
    public void BoxAutoDerefMethodTest()
    {
        // boxed_foo.method() auto-derefs Box(Foo) to call Foo.Method
        AssertSuccess("box_tests/box_auto_deref_method_test", 42);
    }

    [Test]
    public void BoxRefDerefTest()
    {
        // &*boxed_i32 produces &i32 pointing at the heap value
        AssertSuccess("box_tests/box_ref_deref_test", 42);
    }

    [Test]
    public void BoxNonCopyFieldAccessTest()
    {
        // (*boxed_foo).field on non-Copy Foo — place expression, not a move
        // *b = Foo{...} writes to heap, (*b).x + (*b).y reads fields
        AssertSuccess("box_tests/box_non_copy_field_access_test", 42);
    }

    [Test]
    public void BoxNonCopyDerefValueFailTest()
    {
        // let f: Foo = *b where Foo is non-Copy → semantic error
        AssertFail<GenericSemanticError>("box_tests/box_non_copy_deref_value_fail_test");
    }

    // ── Borrow rules through Box deref ──

    [Test]
    public void BoxMultiSharedBorrowTest()
    {
        // Multiple &*b shared borrows — allowed: 42+42-42 = 42
        AssertSuccess("box_tests/box_multi_shared_borrow_test", 42);
    }

    [Test]
    public void BoxUniqThenSharedFailTest()
    {
        // &uniq *b then &*b while uniq is live → borrow conflict
        AssertFail<BorrowInvalidatedError>("box_tests/box_uniq_then_shared_fail_test");
    }

    [Test]
    public void BoxSharedThenUniqFailTest()
    {
        // &*b then &uniq *b while shared is live → borrow conflict
        AssertFail<BorrowInvalidatedError>("box_tests/box_shared_then_uniq_fail_test");
    }

    [Test]
    public void BoxDoubleUniqFailTest()
    {
        // Two &uniq *b while both live → borrow conflict
        AssertFail<BorrowInvalidatedError>("box_tests/box_double_uniq_fail_test");
    }

    [Test]
    public void BoxNllUniqExpiresTest()
    {
        // &uniq *b expires (NLL), then &*b is safe: write 42 via uniq, read via shared
        AssertSuccess("box_tests/box_nll_uniq_expires_test", 42);
    }

    [Test]
    public void BoxAutoDerefBorrowTest()
    {
        // b.get_x() + b.get_y() — auto-deref method calls with &Self (shared, multiple OK)
        AssertSuccess("box_tests/box_auto_deref_borrow_test", 42);
    }

    // ── Double deref (&uniq **ref_to_box) and comprehensive borrow-through-deref ──

    [Test]
    public void BoxDoubleDerefBorrowTest()
    {
        // fn borrower(dd: &uniq Box<i32>) -> &uniq i32 { &uniq **dd }
        // Double deref: &uniq → Box → i32. Returns &uniq i32 pointing at heap.
        AssertSuccess("box_tests/box_double_deref_borrow_test", 4);
    }

    [Test]
    public void BoxDerefBorrowComprehensiveTest()
    {
        // read_through(&b) reads **r, write_through(&uniq b, 42) writes **r = val,
        // get_inner_ref(&b) returns &**r. Tests shared/uniq through double deref,
        // write-read round-trip, and inner ref extraction. val1 + val2 - 42 = 42.
        AssertSuccess("box_tests/box_deref_borrow_comprehensive_test", 42);
    }

    [Test]
    public void BoxDoubleDerefConflictFailTest()
    {
        // &uniq *b then &*b while uniq is live → borrow conflict (same as direct vars)
        AssertFail<BorrowInvalidatedError>("box_tests/box_double_deref_conflict_fail_test");
    }

    [Test]
    public void BoxDerefNllWriteReadTest()
    {
        // &uniq *b → write 42 → NLL expires → &*b → read → 42
        AssertSuccess("box_tests/box_deref_nll_write_read_test", 42);
    }

    // ── Deref hierarchy: raw pointer kind determines what ref kinds are allowed ──

    [Test]
    public void RawPtrConstToUniqFailTest()
    {
        // &uniq *(*const i32) → const can't produce &uniq
        AssertFail<GenericSemanticError>("box_tests/rawptr_const_to_uniq_fail_test");
    }

    [Test]
    public void RawPtrConstToMutFailTest()
    {
        // &mut *(*const i32) → const can't produce &mut
        AssertFail<GenericSemanticError>("box_tests/rawptr_const_to_mut_fail_test");
    }

    [Test]
    public void RawPtrMutToConstFailTest()
    {
        // &const *(*mut i32) → mut doesn't extend DerefConst
        AssertFail<GenericSemanticError>("box_tests/rawptr_mut_to_const_fail_test");
    }

    [Test]
    public void RawPtrSharedToUniqFailTest()
    {
        // &uniq *(*shared i32) → shared can only produce &
        AssertFail<GenericSemanticError>("box_tests/rawptr_shared_to_uniq_fail_test");
    }

    [Test]
    public void RawPtrUniqToSharedTest()
    {
        // &*(*uniq i32) → uniq can produce any ref kind including shared
        AssertSuccess("box_tests/rawptr_mut_to_shared_test", 42);
    }
}

public class DropReturnValueTests
{
    [Test]
    public void DropReturnValueSurvivesTest()
    {
        // Return value (42) must survive Dropper's drop in the function
        AssertSuccess("drop_tests/drop_return_value_survives_test", 42);
    }

    [Test]
    public void DropBlockReturnSurvivesTest()
    {
        // Block return value (42) survives Dropper drop, counter gets +1
        // val=42, counter=1, total=43
        AssertSuccess("drop_tests/drop_block_return_survives_test", 43);
    }
}

public class DerefHierarchyTests
{
    // ── Valid coercions: higher ref kinds can produce lower ones ──

    [Test]
    public void DerefHierarchyValidTest()
    {
        // & from &, &const→&, &mut→&, &uniq→&, &uniq→&const, &uniq→&mut, &uniq→&uniq
        // 7 functions each returning 6 = 42
        AssertSuccess("deref_hierarchy_tests/deref_hierarchy_valid_test", 42);
    }

    // ── Invalid: can't upgrade ref kind through deref ──

    [Test] public void DerefSharedToConstFailTest()
        => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_shared_to_const_fail_test");

    [Test] public void DerefSharedToMutFailTest()
        => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_shared_to_mut_fail_test");

    [Test] public void DerefSharedToUniqFailTest()
        => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_shared_to_uniq_fail_test");

    [Test] public void DerefConstToMutFailTest()
        => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_const_to_mut_fail_test");

    [Test] public void DerefConstToUniqFailTest()
        => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_const_to_uniq_fail_test");

    [Test] public void DerefMutToConstFailTest()
        => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_mut_to_const_fail_test");

    [Test] public void DerefMutToUniqFailTest()
        => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_mut_to_uniq_fail_test");

    // ── Method calls: auto-deref respects hierarchy ──

    [Test]
    public void DerefMethodUniqCallsAllTest()
    {
        // &uniq Foo can call methods taking &Self, &const Self, &mut Self
        // 14 + 14 + 14 = 42
        AssertSuccess("deref_hierarchy_tests/deref_method_uniq_calls_all_test", 42);
    }

    [Test] public void DerefMethodConstToUniqFailTest()
        => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_method_const_to_uniq_fail_test");

    [Test] public void DerefMethodConstToMutFailTest()
        => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_method_const_to_mut_fail_test");

    [Test] public void DerefMethodSharedToMutFailTest()
        => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_method_shared_to_mut_fail_test");

    // ── Multi-level auto-deref (recursive chain) ──

    [Test]
    public void DerefDoubleRefMethodTest()
    {
        // &&Foo → &Foo → Foo, call get() on Foo = 42
        AssertSuccess("deref_hierarchy_tests/deref_double_ref_method_test", 42);
    }

    [Test]
    public void DerefTripleRefMethodTest()
    {
        // &&Foo → &Foo → Foo (2 derefs), call get() = 42
        AssertSuccess("deref_hierarchy_tests/deref_triple_ref_method_test", 42);
    }

    [Test]
    public void DerefDoubleRefMutMethodTest()
    {
        // &uniq &mut Foo → &mut Foo → Foo, call get_mut() via &mut = 42
        AssertSuccess("deref_hierarchy_tests/deref_double_ref_mut_method_test", 42);
    }

    [Test]
    public void DerefChainRefkindFailTest()
    {
        // & &const Foo → last deref is &const, can't produce &uniq
        AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_chain_refkind_fail_test");
    }

    // ── Custom types implementing Deref ──

    [Test]
    public void DerefRefBoxChainTest()
    {
        // &Box(Foo) → Box(Foo) → Foo, 2-level chain = 42
        AssertSuccess("deref_hierarchy_tests/deref_ref_box_chain_test", 42);
    }

    [Test]
    public void DerefBoxChainMethodTest()
    {
        // Box<Box(Foo)> → Box(Foo) → Foo, 2-level smart pointer chain = 42
        AssertSuccess("deref_hierarchy_tests/deref_box_chain_method_test", 42);
    }

    // ── Generic deref bounds ──

    [Test]
    public void DerefGenericBoundMethodTest()
    {
        // T: Deref(Target = Foo), call Foo.Get through T = 42
        AssertSuccess("deref_hierarchy_tests/deref_generic_bound_method_test", 42);
    }

    [Test]
    public void DerefGenericBoundMutFailTest()
    {
        // T: Deref (shared only), method needs &mut Self → fails
        AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_generic_bound_mut_fail_test");
    }

    [Test]
    public void DerefGenericConstRefTest()
    {
        // T = &const Foo which implements Deref(Target = Foo), call Foo.Faah = 42
        AssertSuccess("deref_hierarchy_tests/deref_generic_const_ref_test", 42);
    }

    // ── Chain capability (weakest link) ──

    [Test]
    public void DerefSharedOuterBlocksUniqFailTest()
    {
        // &&uniq i32 → outer & is shared, caps chain to shared → can't call &uniq method
        AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_shared_outer_blocks_uniq_fail_test");
    }

    [Test]
    public void DerefUniqUniqChainTest()
    {
        // &uniq &uniq i32 → both levels are uniq → can call &uniq method = 42
        AssertSuccess("deref_hierarchy_tests/deref_uniq_uniq_chain_test", 42);
    }

    [Test]
    public void DerefConstSharedChainTest()
    {
        // &const &i32 → min(Const, Shared) = Shared → can call &Self method = 42
        AssertSuccess("deref_hierarchy_tests/deref_const_shared_chain_test", 42);
    }

    [Test]
    public void DerefSharedMutChainFailTest()
    {
        // &&mut i32 → min(Shared, Mut) = Shared → can't call &mut Self method
        AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_shared_mut_chain_fail_test");
    }

    [Test]
    public void DerefSharedConstChainFailTest()
    {
        // &&const i32 → min(Shared, Const) = Shared → can't call &const Self method
        AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_shared_const_chain_fail_test");
    }

    // ── Field access through reference (capability caps) ──

    [Test]
    public void DerefFieldThroughSharedRefTest()
    {
        // &Wrapper with &uniq field → can call &Self method through it = 42
        AssertSuccess("deref_hierarchy_tests/deref_field_through_shared_ref_test", 42);
    }

    [Test]
    public void DerefFieldThroughSharedRefFailTest()
    {
        // &Wrapper with &uniq field → can't call &uniq method (shared outer caps it)
        AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_field_through_shared_ref_fail_test");
    }

    [Test]
    public void DerefNestedFieldSharedTest()
    {
        // &Outer → &uniq Middle → &uniq Holder, call &Self method = 42
        AssertSuccess("deref_hierarchy_tests/deref_nested_field_shared_test", 42);
    }

    [Test]
    public void DerefNestedFieldSharedFailTest()
    {
        // &Outer → &uniq Middle → &uniq Holder, call &uniq method → shared outer caps chain
        AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_nested_field_shared_fail_test");
    }

    [Test]
    public void DerefNestedFieldUniqTest()
    {
        // &uniq Outer → &uniq Middle → &uniq Holder, call &uniq method → all uniq = 42
        AssertSuccess("deref_hierarchy_tests/deref_nested_field_uniq_test", 42);
    }

    // ── Deep field access with various ref-kind combinations ──

    [Test]
    public void DerefFieldDeepSharedFailTest()
    {
        // &Outer → &uniq Inner → &uniq Holder → call &uniq — shared outer caps everything
        AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_field_deep_shared_fail_test");
    }

    [Test]
    public void DerefFieldUniqThroughUniqTest()
    {
        // &uniq Wrapper → &uniq Holder → call &uniq method — all uniq = 42
        AssertSuccess("deref_hierarchy_tests/deref_field_uniq_through_uniq_test", 42);
    }

    [Test]
    public void DerefFieldMutWrapperUniqFieldMutTest()
    {
        // &mut Wrapper → &uniq field → call &mut method — min(Mut, Uniq) = Mut → ok = 42
        AssertSuccess("deref_hierarchy_tests/deref_field_mut_wrapper_uniq_field_mut_test", 42);
    }

    [Test]
    public void DerefFieldMutWrapperUniqFieldUniqFailTest()
    {
        // &mut Wrapper → &uniq field → call &uniq method — min(Mut, Uniq) = Mut → can't produce Uniq
        AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_field_mut_wrapper_uniq_field_uniq_fail_test");
    }

    [Test]
    public void DerefFieldConstWrapperSharedMethodTest()
    {
        // &const Wrapper → &uniq field → call &Self method — min(Const, Uniq) = Const → Shared ok = 42
        AssertSuccess("deref_hierarchy_tests/deref_field_const_wrapper_shared_method_test", 42);
    }

    [Test]
    public void DerefFieldDoubleNestedSharedFailTest()
    {
        // &Top → &uniq Mid → &uniq Holder → call &uniq — shared at top caps chain
        AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_field_double_nested_shared_fail_test");
    }

    [Test]
    public void DerefFieldDoubleNestedUniqTest()
    {
        // &uniq Top → &uniq Mid → &uniq Holder → call &uniq — all uniq = 42
        AssertSuccess("deref_hierarchy_tests/deref_field_double_nested_uniq_test", 42);
    }

    [Test]
    public void DerefFieldMixedChainFailTest()
    {
        // &uniq Top → &mut Mid → &uniq Holder → call &uniq — min(Uniq, Mut, Uniq) = Mut → fail
        AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_field_mixed_chain_fail_test");
    }
}

public class BorrowMethodTests
{
    // ── PASS: borrows that coexist with method calls ──

    [Test]
    public void BorrowMethodSharedOkTest()
    {
        // Multiple shared refs + shared method calls all coexist
        AssertSuccess("borrow_method_tests/borrow_method_shared_ok_test", 42);
    }

    [Test]
    public void BorrowMethodSharedCoexistTest()
    {
        // Shared borrow + direct shared method call on original coexist
        AssertSuccess("borrow_method_tests/borrow_method_shared_coexist_test", 42);
    }

    [Test]
    public void BorrowMethodNllUniqExpiresTest()
    {
        // NLL: uniq borrow used and expires, then original method call OK
        AssertSuccess("borrow_method_tests/borrow_method_nll_uniq_expires_test", 42);
    }

    [Test]
    public void BorrowMethodNllMutExpiresTest()
    {
        // NLL: mut borrow used and expires, then original method call OK
        AssertSuccess("borrow_method_tests/borrow_method_nll_mut_expires_test", 42);
    }

    // ── FAIL: borrows that conflict with method calls ──

    [Test]
    public void BorrowMethodUniqBlocksUseFailTest()
    {
        // &uniq active → can't call method on original
        AssertFail("borrow_method_tests/borrow_method_uniq_blocks_use_fail_test");
    }

    [Test]
    public void BorrowMethodMutBlocksUseFailTest()
    {
        // &mut active → can't call method on original
        AssertFail("borrow_method_tests/borrow_method_mut_blocks_use_fail_test");
    }

    [Test]
    public void BorrowMethodUniqBlocksSharedFailTest()
    {
        // &uniq active → can't take &shared of same variable
        AssertFail("borrow_method_tests/borrow_method_uniq_blocks_shared_fail_test");
    }

    [Test]
    public void BorrowMethodDoubleUniqFailTest()
    {
        // Two &uniq of same variable → conflict
        AssertFail("borrow_method_tests/borrow_method_double_uniq_fail_test");
    }

    [Test]
    public void BorrowMethodUniqBlocksMutFailTest()
    {
        // &uniq active → can't take &mut of same variable
        AssertFail("borrow_method_tests/borrow_method_uniq_blocks_mut_fail_test");
    }

    [Test]
    public void BorrowMethodDerefUniqBlocksFailTest()
    {
        // &uniq active → can't use original even through auto-deref chain
        AssertFail("borrow_method_tests/borrow_method_deref_uniq_blocks_fail_test");
    }

    [Test]
    public void BorrowMethodUniqBlocksDerefFailTest()
    {
        // &uniq active → can't call method on original (method also uses uniq)
        AssertFail("borrow_method_tests/borrow_method_uniq_blocks_deref_fail_test");
    }

    [Test]
    public void BorrowMethodSharedThenUniqFailTest()
    {
        // &shared active → can't take &uniq while shared is live
        AssertFail("borrow_method_tests/borrow_method_shared_then_uniq_fail_test");
    }

    // ── Borrow tracking through function returns ──

    [Test]
    public void BorrowFnReturnUniqInputBlocksFailTest()
    {
        // bro(&uniq made) -> &i32: locks made as &uniq, &made while gotten alive → fail
        AssertFail("borrow_method_tests/borrow_fn_return_uniq_input_blocks_fail_test");
    }

    [Test]
    public void BorrowFnReturnUniqInputNllTest()
    {
        // bro(&uniq made) -> &i32: gotten used then expires, &made OK after
        // 5 + 5 = 10
        AssertSuccess("borrow_method_tests/borrow_fn_return_uniq_input_nll_test", 10);
    }

    [Test]
    public void BorrowFnReturnSharedInputCoexistTest()
    {
        // bro(&made) -> &i32: shared input, more shared borrows OK
        // 21 + 21 = 42
        AssertSuccess("borrow_method_tests/borrow_fn_return_shared_input_coexist_test", 42);
    }

    [Test]
    public void BorrowFnReturnStaticNoLockTest()
    {
        // Box.Leak returns &'static uniq — no borrow on source = 42
        AssertSuccess("borrow_method_tests/borrow_fn_return_static_no_lock_test", 42);
    }

    [Test]
    public void BorrowFnSharedInputUniqOutputCoexistTest()
    {
        // fn takes &i32, returns &uniq i32 — borrow tracked as shared (from input)
        // additional &x OK while returned &uniq alive: 21 + 21 = 42
        AssertSuccess("borrow_method_tests/borrow_fn_shared_input_uniq_output_coexist_test", 42);
    }
}

public class ImportTests
{
    [Test]
    public void ImportSpecificTest()
    {
        // use std.core.ops.Add; use std.core.marker.Drop;
        AssertSuccess("import_tests/import_specific_test", 42);
    }

    [Test]
    public void ImportFullPathTest()
    {
        // impl std.core.ops.Add(Wrapper) for Wrapper — no import needed
        AssertSuccess("import_tests/import_full_path_test", 42);
    }

    [Test]
    public void ImportModuleTest()
    {
        // use std.core.ops; then impl ops.Add(Pair) for Pair
        AssertSuccess("import_tests/import_module_test", 42);
    }

    [Test]
    public void ImportMissingFailTest()
    {
        // Bare 'Drop' without import — should fail
        AssertFail("import_tests/import_missing_fail_test");
    }

    [Test]
    public void ImportWrongPathFailTest()
    {
        // use std.core.ops.Drop — Drop is in marker, not ops
        AssertFail("import_tests/import_wrong_path_fail_test");
    }
}

public class ArgTypeTests
{
    [Test]
    public void ArgTypeCorrectTest()
    {
        AssertSuccess("arg_type_tests/arg_type_correct_test", 42);
    }

    [Test]
    public void ArgTypeWrongTypeFailTest()
    {
        // i32 expected, bool provided
        AssertFail("arg_type_tests/arg_type_wrong_type_fail_test");
    }

    [Test]
    public void ArgTypeWrongStructFailTest()
    {
        // Foo expected, Bar provided (different structs, same layout)
        AssertFail("arg_type_tests/arg_type_wrong_struct_fail_test");
    }

    [Test]
    public void ArgTypeRefVsValueFailTest()
    {
        // &i32 expected, i32 provided
        AssertFail("arg_type_tests/arg_type_ref_vs_value_fail_test");
    }

    [Test]
    public void ArgTypeGenericCorrectTest()
    {
        AssertSuccess("arg_type_tests/arg_type_generic_correct_test", 42);
    }

    [Test]
    public void ArgTypeMultiArgCorrectTest()
    {
        // (Point, i32) both correct = 42
        AssertSuccess("arg_type_tests/arg_type_multi_arg_correct_test", 42);
    }

    [Test]
    public void ArgTypeMultiArgSecondWrongFailTest()
    {
        // second arg: Pair expected, i32 provided
        AssertFail("arg_type_tests/arg_type_multi_arg_second_wrong_fail_test");
    }

    [Test]
    public void ArgTypeNestedGenericCorrectTest()
    {
        // &Box(Foo) correct = 42
        AssertSuccess("arg_type_tests/arg_type_nested_generic_correct_test", 42);
    }

    [Test]
    public void ArgTypeGenericOnFnNotTypeFailTest()
    {
        // Box(i32).New(42) — generic belongs on Box, not new
        AssertFail("arg_type_tests/arg_type_generic_on_fn_not_type_fail_test");
    }
}

public class FieldRefTests
{
    // ── &Wrapper with various inner ref kinds ──

    [Test] public void FieldSharedWrapUniqSharedOkTest()
        => AssertSuccess("field_ref_tests/field_shared_wrap_uniq_shared_ok_test", 42);

    [Test] public void FieldSharedWrapUniqUniqFailTest()
        => AssertFail<GenericSemanticError>("field_ref_tests/field_shared_wrap_uniq_uniq_fail_test");

    [Test] public void FieldSharedWrapMutMutFailTest()
        => AssertFail<GenericSemanticError>("field_ref_tests/field_shared_wrap_mut_mut_fail_test");

    [Test] public void FieldSharedWrapMutSharedOkTest()
        => AssertSuccess("field_ref_tests/field_shared_wrap_mut_shared_ok_test", 42);

    [Test] public void FieldSharedWrapConstConstFailTest()
        => AssertFail<GenericSemanticError>("field_ref_tests/field_shared_wrap_const_const_fail_test");

    // ── Matching outer + inner ref kinds (should work) ──

    [Test] public void FieldUniqWrapUniqUniqOkTest()
        => AssertSuccess("field_ref_tests/field_uniq_wrap_uniq_uniq_ok_test", 42);

    [Test] public void FieldMutWrapMutMutOkTest()
        => AssertSuccess("field_ref_tests/field_mut_wrap_mut_mut_ok_test", 42);

    // ── Deep nesting (3 levels) ──

    [Test] public void FieldDeepSharedUniqUniqFailTest()
        => AssertFail<GenericSemanticError>("field_ref_tests/field_deep_shared_uniq_uniq_fail_test");

    [Test] public void FieldDeepUniqUniqUniqOkTest()
        => AssertSuccess("field_ref_tests/field_deep_uniq_uniq_uniq_ok_test", 42);

    // ── Mixed: &const outer with &mut field ──

    [Test] public void FieldConstWrapMutFailTest()
        => AssertFail<GenericSemanticError>("field_ref_tests/field_const_wrap_mut_fail_test");

    // ── Box + field ref combo ──

    [Test] public void FieldBoxWrapUniqSharedOkTest()
        => AssertSuccess("field_ref_tests/field_box_wrap_uniq_shared_ok_test", 42);

    [Test] public void FieldBoxWrapUniqUniqPassTest()
        => AssertSuccess("field_ref_tests/field_box_wrap_uniq_uniq_pass_test");
}

public class ManuallyDropTests
{
    [Test]
    public void ManuallyDropPreventsDropTest()
    {
        // Counter.drop adds 10, but ManuallyDrop prevents it → result stays 0
        AssertSuccess("drop_tests/manually_drop_prevents_drop_test", 0);
    }

    [Test]
    public void ManuallyDropBoxLeakTest()
    {
        // Box.Leak uses ManuallyDrop internally → no use-after-free = 42
        AssertSuccess("drop_tests/manually_drop_box_leak_test", 42);
    }
}
