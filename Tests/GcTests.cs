using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class GcTests
{
    // ── Basic Gc operations ──

    [Test] public void GcBasicTest() => AssertSuccess("gc_tests/gc_basic_test", 42);

    [Test] public void GcDropTest() => AssertSuccess("gc_tests/gc_drop_test", 5);

    [Test] public void GcStructTest() => AssertSuccess("gc_tests/gc_struct_test", 42);

    [Test] public void GcMoveTest() => AssertSuccess("gc_tests/gc_move_test", 77);

    [Test] public void RawPtrBasicTest() => AssertSuccess("gc_tests/rawptr_basic_test", 42);

    // ── Deref assign ──

    [Test]
    public void GcDerefAssignTest()
    {
        // *b = 99 should write to heap, *b should read 99
        AssertSuccess("gc_tests/gc_deref_assign_test", 99);
    }

    [Test]
    public void GcDerefReadAfterAssignTest()
    {
        // *b = Pair{10,32}, (*b).x + (*b).y = 42
        AssertSuccess("gc_tests/gc_deref_read_after_assign_test", 42);
    }

    // ── Use-after-free prevention ──

    [Test]
    public void GcReturnDerefTest()
    {
        // fn get_val(b: Gc<i32>) -> i32 { *b }
        // Return value must survive Gc.Drop
        AssertSuccess("gc_tests/gc_return_deref_test", 42);
    }

    [Test]
    public void GcDropScopeTest()
    {
        // Value from *b inside inner scope must survive scope exit
        AssertSuccess("gc_tests/gc_drop_scope_test", 42);
    }

    // ── Multiple operations ──

    [Test]
    public void GcMultipleDerefTest()
    {
        // Multiple *b reads on same box (i32 is Copy): 7+7+7 = 21
        AssertSuccess("gc_tests/gc_multiple_deref_test", 21);
    }

    [Test]
    public void GcTwoGcsTest()
    {
        // Two boxes in same scope, both dropped correctly: 10+32 = 42
        AssertSuccess("gc_tests/gc_two_boxes_test", 42);
    }

    [Test]
    public void GcNestedStructTest()
    {
        // Gc<Outer> with nested structs, field access through deref: 10+32 = 42
        AssertSuccess("gc_tests/gc_nested_struct_test", 42);
    }

    [Test]
    public void GcUniqDerefInStructTest()
    {
        // &mut *box stored in a struct field, then accessed via *b.dd → 5
        AssertSuccess("gc_tests/gc_uniq_deref_in_struct_test", 5);
    }

    [Test]
    public void GcUniqDerefStructShadowTest()
    {
        // Same as above but the Gc binding is shadowed by the struct — old Gc
        // still lives on the stack, reference remains valid → *a.dd == 5
        AssertSuccess("gc_tests/gc_uniq_deref_struct_shadow_test", 5);
    }
}

public class DropFieldTests
{

    [Test]
    public void DropTransitiveFieldTest()
    {
        // A(Drop +10) -> B(no Drop) -> C(Drop +1) = 11
        AssertSuccess("gc_tests/drop_transitive_field_test", 11);
    }

    [Test]
    public void DropTransitiveNoParentTest()
    {
        // A(no Drop) -> B(no Drop) -> C(Drop +1) = 1
        AssertSuccess("gc_tests/drop_transitive_no_parent_test", 1);
    }

    [Test]
    public void DropMultiFieldTest()
    {
        // Multi(no Drop) has two Dropper fields (each +1) = 2
        AssertSuccess("gc_tests/drop_multi_field_test", 2);
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
            // & from &, &const→&, &mut→&, &mut→&, &mut→&const, &mut→&mut, &mut→&mut
            // 7 functions each returning 6 = 42
            AssertSuccess("deref_hierarchy_tests/deref_hierarchy_valid_test", 12);
        }

        // ── Invalid: can't upgrade ref kind through deref ──

        [Test]
        public void DerefSharedToConstFailTest()
            => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_shared_to_const_fail_test");

        [Test]
        public void DerefSharedToMutFailTest()
            => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_shared_to_mut_fail_test");

        [Test]
        public void DerefSharedToUniqFailTest()
            => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_shared_to_uniq_fail_test");



        [Test]
        public void DerefMutToConstFailTest()
            => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_mut_to_const_fail_test");


        [Test]
        public void DerefMethodConstToUniqFailTest()
            => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_method_const_to_uniq_fail_test");

        [Test]
        public void DerefMethodConstToMutFailTest()
            => AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_method_const_to_mut_fail_test");

        [Test]
        public void DerefMethodSharedToMutFailTest()
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
            // &mut &mut Foo → &mut Foo → Foo, call get_mut() via &mut = 42
            AssertSuccess("deref_hierarchy_tests/deref_double_ref_mut_method_test", 42);
        }

        // ── Custom types implementing Deref ──

        [Test]
        public void DerefRefBoxChainTest()
        {
            // &Gc(Foo) → Gc(Foo) → Foo, 2-level chain = 42
            AssertSuccess("deref_hierarchy_tests/deref_ref_gc_chain_test", 42);
        }

        [Test]
        public void DerefBoxChainMethodTest()
        {
            // Gc<Gc(Foo)> → Gc(Foo) → Foo, 2-level smart pointer chain = 42
            AssertSuccess("deref_hierarchy_tests/deref_gc_chain_method_test", 42);
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
        public void DerefGenericRefTest()
        {
            // T = & Foo which implements Deref(Target = Foo), call Foo.Faah = 42
            AssertSuccess("deref_hierarchy_tests/deref_generic_ref_test", 42);
        }

        // ── Chain capability (weakest link) ──

        [Test]
        public void DerefSharedOuterBlocksUniqFailTest()
        {
            // &&mut i32 → outer & is shared, caps chain to shared → can't call &mut method
            AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_shared_outer_blocks_uniq_fail_test");
        }

        [Test]
        public void DerefUniqUniqChainTest()
        {
            // &mut &mut i32 → both levels are mut → can call &mut method = 42
            AssertSuccess("deref_hierarchy_tests/deref_uniq_uniq_chain_test", 42);
        }


        [Test]
        public void DerefSharedMutChainFailTest()
        {
            // &&mut i32 → min(Shared, Mut) = Shared → can't call &mut Self method
            AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_shared_mut_chain_fail_test");
        }


        // ── Field access through reference (capability caps) ──

        [Test]
        public void DerefFieldThroughSharedRefTest()
        {
            // &Wrapper with &mut field → can call &Self method through it = 42
            AssertSuccess("deref_hierarchy_tests/deref_field_through_shared_ref_test", 42);
        }

        [Test]
        public void DerefFieldThroughSharedRefFailTest()
        {
            // &Wrapper with &mut field → can't call &mut method (shared outer caps it)
            AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_field_through_shared_ref_fail_test");
        }

        [Test]
        public void DerefNestedFieldSharedTest()
        {
            // &Outer → &mut Middle → &mut Holder, call &Self method = 42
            AssertSuccess("deref_hierarchy_tests/deref_nested_field_shared_test", 42);
        }

        [Test]
        public void DerefNestedFieldSharedFailTest()
        {
            // &Outer → &mut Middle → &mut Holder, call &mut method → shared outer caps chain
            AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_nested_field_shared_fail_test");
        }

        [Test]
        public void DerefNestedFieldUniqTest()
        {
            // &mut Outer → &mut Middle → &mut Holder, call &mut method → all mut = 42
            AssertSuccess("deref_hierarchy_tests/deref_nested_field_uniq_test", 42);
        }

        // ── Deep field access with various ref-kind combinations ──

        [Test]
        public void DerefFieldDeepSharedFailTest()
        {
            // &Outer → &mut Inner → &mut Holder → call &mut — shared outer caps everything
            AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_field_deep_shared_fail_test");
        }

        [Test]
        public void DerefFieldUniqThroughUniqTest()
        {
            // &mut Wrapper → &mut Holder → call &mut method — all mut = 42
            AssertSuccess("deref_hierarchy_tests/deref_field_uniq_through_uniq_test", 42);
        }

        [Test]
        public void DerefFieldMutWrapperUniqFieldMutTest()
        {
            // &mut Wrapper → &mut field → call &mut method — min(Mut, Uniq) = Mut → ok = 42
            AssertSuccess("deref_hierarchy_tests/deref_field_mut_wrapper_uniq_field_mut_test", 42);
        }


        [Test]
        public void DerefFieldDoubleNestedSharedFailTest()
        {
            // &Top → &mut Mid → &mut Holder → call &mut — shared at top caps chain
            AssertFail<GenericSemanticError>("deref_hierarchy_tests/deref_field_double_nested_shared_fail_test");
        }

        [Test]
        public void DerefFieldDoubleNestedUniqTest()
        {
            // &mut Top → &mut Mid → &mut Holder → call &mut — all mut = 42
            AssertSuccess("deref_hierarchy_tests/deref_field_double_nested_uniq_test", 42);
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
            // NLL: mut borrow used and expires, then original method call OK
            AssertSuccess("borrow_method_tests/borrow_method_nll_uniq_expires_test", 42);
        }

        [Test]
        public void BorrowMethodNllMutExpiresTest()
        {
            // NLL: mut borrow used and expires, then original method call OK
            AssertSuccess("borrow_method_tests/borrow_method_nll_mut_expires_test", 42);
        }





        [Test]
        public void BorrowFnReturnUniqInputNllTest()
        {
            // bro(&mut made) -> &i32: gotten used then expires, &made OK after
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
        public void BorrowEnumVariantReleasedAfterMoveTest()
        {
            // After moving enum to DropNow, borrow is released → idk accessible = 1
            AssertSuccess("borrow_method_tests/borrow_enum_variant_released_after_move_test", 1);
        }
    }

    public class ImportTests
    {
        [Test]
        public void ImportSpecificTest()
        {
            // use Std.Ops.Add; use Std.Ops.Drop;
            AssertSuccess("import_tests/import_specific_test", 42);
        }

        [Test]
        public void ImportFullPathTest()
        {
            // impl Std.Ops.Add(Wrapper) for Wrapper — no import needed
            AssertSuccess("import_tests/import_full_path_test", 42);
        }

        [Test]
        public void ImportModuleTest()
        {
            // use Std.Ops; then impl Ops.Add(Pair) for Pair
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
            // use Std.Marker.Drop — Drop is in Ops, not Marker
            AssertFail("import_tests/import_wrong_path_fail_test");
        }

        [Test]
        public void ImportNonexistentFailTest()
        {
            // use Std.Nonexistent.Foo — path doesn't exist
            AssertFail<GenericSemanticError>("import_tests/import_nonexistent_fail_test");
        }

        [Test]
        public void ImportWrongVariantFailTest()
        {
            // use Color.Yellow — Yellow is not a variant of Color
            AssertFail<GenericSemanticError>("import_tests/import_wrong_variant_fail_test");
        }

        [Test]
        public void ImportValidFunctionTest()
        {
            AssertSuccess("import_tests/import_valid_function_test", 1);
        }

        [Test]
        public void ImportValidVariantTest()
        {
            // use Dir.Up; then bare Up in match
            AssertSuccess("import_tests/import_valid_variant_test", 1);
        }

        [Test]
        public void ImportCrateNoMainFailTest()
        {
            // crate does not include 'main' — use crate.main.Foo should fail
            AssertFail<GenericSemanticError>("import_tests/import_crate_no_main_fail_test");
        }

        [Test]
        public void ImportCrateCorrectTest()
        {
            // crate expands to module path without 'main'
            AssertSuccess("import_tests/import_crate_correct_test", 1);
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
            // &Gc(Foo) correct = 42
            AssertSuccess("arg_type_tests/arg_type_nested_generic_correct_test", 42);
        }

        [Test]
        public void ArgTypeGenericOnFnNotTypeFailTest()
        {
            // Gc(i32).New(42) — generic belongs on Gc, not new
            AssertFail("arg_type_tests/arg_type_generic_on_fn_not_type_fail_test");
        }

        [Test]
        public void ArgTypeTooManyArgsFailTest()
        {
            AssertFail<GenericSemanticError>("arg_type_tests/arg_type_too_many_args_fail_test");
        }

        [Test]
        public void ArgTypeTooFewArgsFailTest()
        {
            AssertFail<GenericSemanticError>("arg_type_tests/arg_type_too_few_args_fail_test");
        }

        [Test]
        public void ArgTypeVoidReturnMismatchFailTest()
        {
            AssertFail<ReturnTypeMismatchError>("arg_type_tests/arg_type_void_return_mismatch_fail_test");
        }
    }

    public class FieldRefTests
    {
        // ── &Wrapper with various inner ref kinds ──

        [Test]
        public void FieldSharedWrapUniqSharedOkTest()
            => AssertSuccess("field_ref_tests/field_shared_wrap_uniq_shared_ok_test", 42);

        [Test]
        public void FieldSharedWrapUniqUniqFailTest()
            => AssertFail<GenericSemanticError>("field_ref_tests/field_shared_wrap_uniq_uniq_fail_test");

        [Test]
        public void FieldSharedWrapMutMutFailTest()
            => AssertFail<GenericSemanticError>("field_ref_tests/field_shared_wrap_mut_mut_fail_test");

        [Test]
        public void FieldSharedWrapMutSharedOkTest()
            => AssertSuccess("field_ref_tests/field_shared_wrap_mut_shared_ok_test", 42);


        // ── Matching outer + inner ref kinds (should work) ──

        [Test]
        public void FieldUniqWrapUniqUniqOkTest()
            => AssertSuccess("field_ref_tests/field_uniq_wrap_uniq_uniq_ok_test", 42);

        [Test]
        public void FieldMutWrapMutMutOkTest()
            => AssertSuccess("field_ref_tests/field_mut_wrap_mut_mut_ok_test", 42);

        // ── Deep nesting (3 levels) ──

        [Test]
        public void FieldDeepSharedUniqUniqFailTest()
            => AssertFail<GenericSemanticError>("field_ref_tests/field_deep_shared_uniq_uniq_fail_test");

        [Test]
        public void FieldDeepUniqUniqUniqOkTest()
            => AssertSuccess("field_ref_tests/field_deep_uniq_uniq_uniq_ok_test", 42);



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
        public void ManuallyDropNestedTest()
        {
            // Outer has Drop (+10) and contains Inner with Drop (+1). ManuallyDrop suppresses both → 0
            AssertSuccess("drop_tests/manually_drop_nested_test", 0);
        }

        [Test]
        public void ManuallyDropSiblingStillDropsTest()
        {
            // c1 wrapped in ManuallyDrop (suppressed), c2 not wrapped (drops). a=0, b=1 → 1
            AssertSuccess("drop_tests/manually_drop_sibling_still_drops_test", 1);
        }

        [Test]
        public void ManuallyDropMultipleTest()
        {
            // Two counters both wrapped in ManuallyDrop → neither drops → 0
            AssertSuccess("drop_tests/manually_drop_multiple_test", 0);
        }

        [Test]
        public void ManuallyDropInFunctionTest()
        {
            // Value moved into function, wrapped in ManuallyDrop inside → drop suppressed → 0
            AssertSuccess("drop_tests/manually_drop_in_function_test", 0);
        }

        [Test]
        public void ManuallyDropWrapThenDropOtherTest()
        {
            // Suppressed adds 100 (not fired), active adds 7 (fired) → 7
            AssertSuccess("drop_tests/manually_drop_wrap_then_drop_other_test", 7);
        }

        [Test]
        public void DropMutBorrowVisibleInCopyReturnTest()
        {
            // Returning a while non-Copy dd borrows &mut a → rejected
            AssertFail<MoveWhileBorrowedError>("drop_tests/drop_mut_borrow_visible_in_copy_return_test");
        }

        [Test]
        public void DropTemporaryMethodReceiverTest()
        {
            // make Foo { &mut a }.get_val() → get_val returns 0, temp Foo dropped → a=1, return 0+1=1
            AssertSuccess("drop_tests/drop_temporary_method_receiver_test", 1);
        }

        [Test]
        public void DropChainedTemporaryReceiversTest()
        {
            // .inc().inc() consuming self → 3 Drops total (2 in inc + 1 at scope exit) = 3
            AssertSuccess("drop_tests/drop_chained_temporary_receivers_test", 3);
        }

        [Test]
        public void DropChainOrderTest()
        {
            // Drop encodes order as digits: self(id=1) → self(id=2) → result(id=3) = 123
            AssertSuccess("drop_tests/drop_chain_order_test", 123);
        }

        [Test]
        public void DropTemporaryFieldAccessTest()
        {
            // make Wrapper { val: 7, tracker: Tracker }.val → Wrapper dropped → Tracker.Drop → c=100, return 107
            AssertSuccess("drop_tests/drop_temporary_field_access_test", 107);
        }

        // ═══════════════════════════════════════════════════════════════
        //  TEMPORARY METHOD CALL — lifetime-dependent return from temporary receiver
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void TemporaryLifetimeRefTest()
        {
            // make Foo { val: 42 }.get_ref() → &i32 tied to temporary's lifetime, used in same scope
            AssertSuccess("drop_tests/drop_temporary_lifetime_ref_test", 42);
        }

        [Test]
        public void TemporaryLifetimeRefDropTest()
        {
            // Tracker temporary spilled, get_ref() returns &i32, drop fires at block exit.
            // *r == 42, c == 100 after drop → 142
            AssertSuccess("drop_tests/drop_temporary_lifetime_ref_drop_test", 142);
        }

        [Test]
        public void TemporaryLifetimeRefEscapeFailTest()
        {
            // Reference from temporary.get_ref() escapes block where temporary lives → dangling
            AssertFail<DanglingReferenceError>("drop_tests/drop_temporary_lifetime_ref_escape_fail_test");
        }

        [Test]
        public void TemporaryLifetimeRefMultiTest()
        {
            // Two separate temporaries, each returning &i32 via get_ref(). Both valid in scope.
            AssertSuccess("drop_tests/drop_temporary_lifetime_ref_multi_test", 42);
        }

        [Test]
        public void TemporaryLifetimeRefChainTest()
        {
            // make Foo{val:42}.as_wrapper() returns Wrapper['a]{inner: &'a i32}. Lifetime-dep struct.
            AssertSuccess("drop_tests/drop_temporary_lifetime_ref_chain_test", 42);
        }

        // ═══════════════════════════════════════════════════════════════
        //  TEMPORARY SELF CONSUME — temp.method(self: Self)
        // ═══════════════════════════════════════════════════════════════

        [Test]
        public void TemporarySelfConsumeTest()
        {
            // make Foo{val:42}.consume() — self: Self, returns i32. No spill needed.
            AssertSuccess("drop_tests/drop_temporary_self_consume_test", 42);
        }

        [Test]
        public void TemporarySelfConsumeDropTest()
        {
            // Tracker temporary consumed by method. Drop fires at method exit → c=100. 42+100=142.
            AssertSuccess("drop_tests/drop_temporary_self_consume_drop_test", 142);
        }

        [Test]
        public void TemporarySelfConsumeChainTest()
        {
            // Temporary → double() → add(22) → get_val(). All self: Self. 10→20→42.
            AssertSuccess("drop_tests/drop_temporary_self_consume_chain_test", 42);
        }

        [Test]
        public void TemporarySelfConsumeFieldTest()
        {
            // make Pair{x:20,y:22}.sum() — consumes Pair, returns x+y = 42.
            AssertSuccess("drop_tests/drop_temporary_self_consume_field_test", 42);
        }

        [Test]
        public void TemporarySelfConsumeToNewTypeTest()
        {
            // make Foo{val:42}.into_bar() — consumes Foo, returns Bar{inner:42}.
            AssertSuccess("drop_tests/drop_temporary_self_consume_then_ref_test", 42);
        }

        [Test]
        public void SelfConsumeDoubleUseFailTest()
        {
            // f.consume() moves f (non-Copy). Second f.consume() is use-after-move.
            AssertFail<UseAfterMoveError>("drop_tests/drop_temporary_self_consume_double_use_fail_test");
        }

        [Test]
        public void RefReturnThenConsumeFailTest()
        {
            // get_bar_ref() returns &Bar. r.consume() takes self: Self (Bar).
            // Cannot move non-Copy Bar out of a reference.
            AssertFail("drop_tests/drop_temporary_ref_return_consume_fail_test");
        }

        // ═══════════════════════════════════════════════════════════════
        //  METHOD CHAINING — auto-deref, borrow propagation, capability
        // ═══════════════════════════════════════════════════════════════

        // ── Chained calls: x.foo().bar() ──

        [Test]
        public void ChainSharedSharedTest()
            => AssertSuccess("method_chain_tests/chain_shared_shared", 42);

        [Test]
        public void ChainSharedReturnsRefThenSharedTest()
            => AssertSuccess("method_chain_tests/chain_shared_returns_ref_then_shared", 42);

        [Test]
        public void ChainConsumeThenSharedTest()
            => AssertSuccess("method_chain_tests/chain_consume_then_shared", 42);

        [Test]
        public void ChainConsumeThenConsumeCopyTest()
            => AssertSuccess("method_chain_tests/chain_consume_then_consume_copy", 42);

        [Test]
        public void ChainBuilderPatternTest()
            => AssertSuccess("method_chain_tests/chain_builder_pattern", 42);

        [Test]
        public void ChainRefCopyConsumeCopyTest()
            => AssertSuccess("method_chain_tests/chain_ref_copy_consume_copy", 42);

        [Test]
        public void ChainConsumeNoncopyThenMethodTest()
            => AssertSuccess("method_chain_tests/chain_consume_noncopy_then_method", 42);

        [Test]
        public void ChainSharedReturnsRefFieldAccessTest()
            => AssertSuccess("method_chain_tests/chain_shared_returns_ref_field_access", 42);

        // ── Chained calls through Gc auto-deref ──

        [Test]
        public void ChainBoxSharedSharedTest()
            => AssertSuccess("method_chain_tests/chain_gc_shared_shared", 42);

        [Test]
        public void ChainBoxRefReturnThenMethodTest()
            => AssertSuccess("method_chain_tests/chain_gc_ref_return_then_method", 42);

        [Test]
        public void ChainBoxConsumeThenMethodTest()
            => AssertSuccess("method_chain_tests/chain_gc_consume_inner_then_method", 42);

        [Test]
        public void ChainBoxFieldThenMethodTest()
            => AssertSuccess("method_chain_tests/chain_gc_field_then_method", 42);

        [Test]
        public void ChainBoxNestedFieldDerefTest()
            => AssertSuccess("method_chain_tests/chain_gc_nested_field_deref", 42);

        // ── Sequential calls on Gc (mutation + read, NLL) ──

        [Test]
        public void ChainBoxUniqThenSharedTest()
            => AssertSuccess("method_chain_tests/chain_gc_uniq_then_shared", 42);

        [Test]
        public void ChainBoxMutThenSharedTest()
            => AssertSuccess("method_chain_tests/chain_gc_mut_then_shared", 42);

        [Test]
        public void ChainNllBorrowExpiresThenChainTest()
            => AssertSuccess("method_chain_tests/chain_nll_borrow_expires_then_chain", 42);

        // ── Single-level through deref / ref kinds ──

        [Test]
        public void ChainMutThenSharedTest()
            => AssertSuccess("method_chain_tests/chain_mut_then_shared", 42);

        [Test]
        public void ChainDoubleDerefRefBoxTest()
            => AssertSuccess("method_chain_tests/chain_double_deref_ref_gc", 42);

        [Test]
        public void ChainCopySelfThroughRefTwiceTest()
            => AssertSuccess("method_chain_tests/chain_copy_self_through_ref_twice", 42);

        // ── Fail: move out of deref ──

        [Test]
        public void ChainRefReturnConsumeNoncopyFailTest()
            => AssertFail("method_chain_tests/chain_ref_return_consume_noncopy_fail");

        [Test]
        public void ChainMoveThroughRefBoxFailTest()
            => AssertFail("method_chain_tests/chain_move_through_ref_gc_fail");


        [Test]
        public void ChainCopyFieldThroughRefOkTest()
            => AssertSuccess("method_chain_tests/chain_copy_field_through_ref_ok", 42);

        // ── Fail: borrow conflicts through Gc ──


        [Test]
        public void ChainBoxSharedThenMutCoexistTest()
            => AssertSuccess("method_chain_tests/chain_gc_shared_then_mut_coexist_test", 99);

        // ── Fail: use after move ──

        [Test]
        public void ChainConsumeThenUseMovedFailTest()
            => AssertFail<UseAfterMoveError>("method_chain_tests/chain_consume_then_use_moved_fail");

        [Test]
        public void ChainDoubleConsumeFailTest()
            => AssertFail<UseAfterMoveError>("method_chain_tests/chain_double_consume_fail");

        [Test]
        public void ChainBuilderUseAfterMoveFailTest()
            => AssertFail<UseAfterMoveError>("method_chain_tests/chain_builder_use_after_move_fail");

        // ── Fail: dangling reference ──

        [Test]
        public void ChainBoxRefEscapeScopeFailTest()
            => AssertFail<DanglingReferenceError>("method_chain_tests/chain_gc_ref_escape_scope_fail");

        [Test]
        public void ChainTemporaryLifetimeRefEscapeFailTest()
            => AssertFail<DanglingReferenceError>("method_chain_tests/chain_temporary_lifetime_ref_escape_fail");

        // ═══════════════════════════════════════════════════════════════
        //  TEMPORARY DEREF — *tempExpr on non-Copy smart pointers is rejected
        // ═══════════════════════════════════════════════════════════════


        [Test]
        public void TemporaryDerefCustomWrapperFailTest()
        {
            AssertFail<GenericSemanticError>("drop_tests/drop_temporary_deref_custom_wrapper_test");
        }

        [Test]
        public void TemporaryDerefInBlockFailTest()
        {
            AssertFail<GenericSemanticError>("drop_tests/drop_temporary_deref_in_block_test");
        }

        [Test]
        public void TemporaryDerefMultipleFailTest()
        {
            AssertFail<GenericSemanticError>("drop_tests/drop_temporary_deref_multiple_test");
        }
    }
}
