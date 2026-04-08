using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class TraitTests
{
    [Test] public void TraitBasicTest() => AssertSuccess("trait_tests/trait_basic_test", 40);
    [Test] public void TraitGenericParamCallTest() => AssertSuccess("trait_tests/trait_generic_param_call_test", 3);

    [Test]
    public void TraitBoundViolationTest()
    {
        var result = AssertFail<TraitBoundViolationError>("trait_tests/trait_bound_violation_test");
        var violations = result.GetErrors<TraitBoundViolationError>().ToList();
        Assert.That(violations.Count == 1);
        Assert.That(violations[0].TypePath.ToString().Contains("bool"));
        Assert.That(violations[0].TraitPath.ToString().Contains("Foo"));
    }

    [Test] public void TraitEmptyBoundTest() => AssertSuccess("trait_tests/trait_empty_bound_test", 5);
    [Test] public void TraitMultiBoundTest() => AssertSuccess("trait_tests/trait_multi_bound_test", 16);

    [Test]
    public void TraitMultiBoundViolationTest()
    {
        var result = AssertFail<TraitBoundViolationError>("trait_tests/trait_multi_bound_violation_test");
        var violations = result.GetErrors<TraitBoundViolationError>().ToList();
        Assert.That(violations.Count == 2);
        var traitNames = violations.Select(v => v.TraitPath.ToString()).ToList();
        Assert.That(traitNames.Any(t => t.Contains("Adder")));
        Assert.That(traitNames.Any(t => t.Contains("Multiplier")));
        Assert.That(violations.All(v => v.TypePath.ToString().Contains("bool")));
    }

    [Test] public void TraitDuplicateGenericParamTest() => AssertFail<GenericSemanticError>("trait_tests/trait_duplicate_generic_test");
    [Test] public void TraitQualifiedCallTest() => AssertSuccess("trait_tests/trait_qualified_call_test", 42);
    [Test] public void TraitConcreteTypeCallTest() => AssertSuccess("trait_tests/trait_concrete_type_call_test", 99);
    [Test] public void TraitImplMethodExtraBoundsFailTest() => AssertFail<TraitImplBoundsMismatchError>("trait_tests/trait_impl_method_extra_bounds_fail_test");
    [Test] public void TraitImplMethodMissingBoundsFailTest() => AssertFail<TraitImplBoundsMismatchError>("trait_tests/trait_impl_method_missing_bounds_fail_test");
    [Test] public void TraitImplMethodGenericCountFailTest() => AssertFail<TraitImplBoundsMismatchError>("trait_tests/trait_impl_method_generic_count_fail_test");
    [Test] public void TraitImplMethodMatchingBoundsTest() => AssertSuccess("trait_tests/trait_impl_method_matching_bounds_test", 5);
    [Test] public void TraitGenericBoundPropagationFailTest() => AssertFail("trait_tests/trait_generic_bound_propagation_fail_test");
    [Test] public void TraitGenericBoundPropagationPassTest() => AssertSuccess("trait_tests/trait_generic_bound_propagation_pass_test", 5);
    [Test] public void TraitQualifiedComptimeCallTest() => AssertSuccess("trait_tests/trait_qualified_comptime_call_test", 42);
    [Test] public void TraitGenericParamComptimeCallTest() => AssertSuccess("trait_tests/trait_generic_param_comptime_call_test", 7);
    [Test] public void TraitShorthandComptimeCallTest() => AssertSuccess("trait_tests/trait_shorthand_comptime_call_test", 13);
    [Test] public void TraitBothComptimeCallTest() => AssertSuccess("trait_tests/trait_both_comptime_call_test", 10);
    [Test] public void TraitComptimeCallSemanticTest() => AssertSuccess("trait_tests/trait_comptime_call_semantic_test");
    [Test] public void TraitSupertraitBasicTest() => AssertSuccess("trait_tests/trait_supertrait_basic_test", 15);
    [Test] public void TraitSupertraitTransitiveTest() => AssertSuccess("trait_tests/trait_supertrait_transitive_test", 4);
    [Test] public void TraitSupertraitFailTest() => AssertFail("trait_tests/trait_supertrait_fail_test");
    [Test] public void TraitSupertraitMultiTest() => AssertSuccess("trait_tests/trait_supertrait_multi_test", 6);
}

public class MethodCallTests
{
    [Test] public void TraitMethodCallTest() => AssertSuccess("trait_tests/trait_method_call_test", 42);
    [Test] public void TraitMethodCallI32Test() => AssertSuccess("trait_tests/trait_method_call_i32_test", 10);
    [Test] public void TraitMethodCallWithArgsTest() => AssertSuccess("trait_tests/trait_method_call_with_args_test", 15);
    [Test] public void TraitMethodChainTest() => AssertSuccess("trait_tests/trait_method_chain_test", 7);
    [Test] public void TraitReceiverSupertraitTest() => AssertSuccess("trait_tests/trait_receiver_supertrait_test", 30);
}

public class SupertraitGenericTests
{
    [Test] public void TraitSupertraitGenericPassthroughTest() => AssertSuccess("trait_tests/trait_supertrait_generic_passthrough_test", 15);
    [Test] public void TraitSupertraitGenericWithPlainTest() => AssertSuccess("trait_tests/trait_supertrait_generic_with_plain_test", 23);
    [Test] public void TraitSupertraitConcreteGenericTest() => AssertSuccess("trait_tests/trait_supertrait_concrete_generic_test", 15);
    [Test] public void TraitSupertraitGenericMismatchFailTest() => AssertFail("trait_tests/trait_supertrait_generic_mismatch_fail_test");
    [Test] public void TraitSupertraitDeepGenericTest() => AssertSuccess("trait_tests/trait_supertrait_deep_generic_test", 4);
}

public class MethodChainTests
{
    [Test] public void TraitMethodChainCallTest() => AssertSuccess("trait_tests/trait_method_chain_call_test", 7);
    [Test] public void TraitMethodChainStructTest() => AssertSuccess("trait_tests/trait_method_chain_struct_test", 6);
}

public class SelfInBoundsTests
{
    [Test] public void TraitMethodSelfBoundTest() => AssertSuccess("trait_tests/trait_method_self_bound_test", 15);
    [Test] public void TraitSelfBoundQualifiedTest() => AssertSuccess("trait_tests/trait_self_bound_qualified_test", 15);
    [Test] public void TraitSelfBoundMismatchFailTest() => AssertFail("trait_tests/trait_self_bound_mismatch_fail_test");
    [Test] public void TraitSelfReturnAndBoundTest() => AssertSuccess("trait_tests/trait_self_return_and_bound_test", 10);
}

public class SupertraitValidationTests
{
    // --- Supertrait definition validation ---

    [Test]
    public void TraitSupertraitUndefinedFailTest()
    {
        // trait A : B {} — B doesn't exist
        AssertFail<TraitNotFoundError>("trait_tests/trait_supertrait_undefined_fail_test");
    }

    [Test]
    public void TraitSupertraitUndefinedGenericFailTest()
    {
        // trait A : B<i32> {} — B doesn't exist
        AssertFail<TraitNotFoundError>("trait_tests/trait_supertrait_undefined_generic_fail_test");
    }

    [Test]
    public void TraitSupertraitNotATraitFailTest()
    {
        // trait A : Foo {} — Foo is a struct, not a trait
        AssertFail("trait_tests/trait_supertrait_not_a_trait_fail_test");
    }

    // --- Supertrait implementation validation (direct) ---

    [Test]
    public void TraitSupertraitNotImplFailTest()
    {
        // trait A : B {}, impl A for i32 — but i32 doesn't implement B
        AssertFail<SupertraitNotImplementedError>("trait_tests/trait_supertrait_not_impl_fail_test");
    }

    [Test]
    public void TraitSupertraitImplPassTest()
    {
        // trait A : B {}, impl B for i32, impl A for i32 — OK
        AssertSuccess("trait_tests/trait_supertrait_impl_pass_test", 42);
    }

    // --- Transitive supertrait validation ---

    [Test]
    public void TraitSupertraitTransitiveNotImplFailTest()
    {
        // trait C {}, trait B : C {}, trait A : B {}
        // impl B for i32, impl A for i32 — but i32 doesn't implement C
        AssertFail<SupertraitNotImplementedError>("trait_tests/trait_supertrait_transitive_not_impl_fail_test");
    }

    [Test]
    public void TraitSupertraitTransitiveImplPassTest()
    {
        // impl C for i32, impl B for i32, impl A for i32 — all OK
        AssertSuccess("trait_tests/trait_supertrait_transitive_impl_pass_test", 42);
    }

    // --- Multiple supertrait validation ---

    [Test]
    public void TraitSupertraitMultiNotImplFailTest()
    {
        // trait A : B + C {}, impl B for i32, impl A for i32 — but missing C
        AssertFail<SupertraitNotImplementedError>("trait_tests/trait_supertrait_multi_not_impl_fail_test");
    }

    [Test]
    public void TraitSupertraitMultiImplPassTest()
    {
        // trait A : B + C {}, impl B + C + A for i32 — all OK
        AssertSuccess("trait_tests/trait_supertrait_multi_impl_pass_test", 42);
    }

    // --- Generic supertrait validation ---

    [Test]
    public void TraitSupertraitGenericNotImplFailTest()
    {
        // trait A : Marker {}, impl A for Foo — but Foo doesn't implement Marker
        AssertFail<SupertraitNotImplementedError>("trait_tests/trait_supertrait_generic_not_impl_fail_test");
    }

    [Test]
    public void TraitSupertraitGenericParamNotImplFailTest()
    {
        // trait Foo<T> : Bar<T> {}, impl Foo<i32> for MyType — but MyType doesn't implement Bar<i32>
        AssertFail<SupertraitNotImplementedError>("trait_tests/trait_supertrait_generic_param_not_impl_fail_test");
    }

    [Test]
    public void TraitSupertraitGenericParamImplPassTest()
    {
        // trait Foo<T> : Bar<T> {}, impl Bar<i32> for MyType, impl Foo<i32> for MyType — OK
        AssertSuccess("trait_tests/trait_supertrait_generic_param_impl_pass_test", 42);
    }

    // --- Generic argument count validation on supertrait references ---

    [Test]
    public void TraitSupertraitMissingGenericArgsFailTest()
    {
        // trait Foo<T> {}, trait Bar: Foo {} — Foo requires 1 generic arg, 0 provided
        AssertFail<GenericParamCountError>("trait_tests/trait_supertrait_missing_generic_args_fail_test");
    }

    [Test]
    public void TraitSupertraitTooManyGenericArgsFailTest()
    {
        // trait Foo<T> {}, trait Bar: Foo<i32, bool> {} — Foo requires 1 generic arg, 2 provided
        AssertFail<GenericParamCountError>("trait_tests/trait_supertrait_too_many_generic_args_fail_test");
    }

    // --- Wrong generic arg on supertrait implementation ---

    [Test]
    public void TraitSupertraitWrongGenericImplFailTest()
    {
        // trait Foo<T> {}, trait Bar: Foo<i32> {}
        // impl Foo<bool> for i32, impl Bar for i32 — has Foo<bool> but needs Foo<i32>
        AssertFail<SupertraitNotImplementedError>("trait_tests/trait_supertrait_wrong_generic_impl_fail_test");
    }

    [Test]
    public void TraitSupertraitCorrectGenericImplPassTest()
    {
        // trait Foo<T> {}, trait Bar: Foo<i32> {}
        // impl Foo<i32> for i32, impl Bar for i32 — correct generic arg
        AssertSuccess("trait_tests/trait_supertrait_correct_generic_impl_pass_test", 42);
    }

    // --- Forwarded generic param on supertraits ---

    [Test]
    public void TraitSupertraitForwardedGenericPassTest()
    {
        // trait Foo<T> {}, trait Bar<T>: Foo<T> {}
        // impl Foo<i32> for i32, impl Bar<i32> for i32 — T forwarded correctly
        AssertSuccess("trait_tests/trait_supertrait_forwarded_generic_pass_test", 42);
    }

    [Test]
    public void TraitSupertraitForwardedWrongImplFailTest()
    {
        AssertFail<SupertraitNotImplementedError>("trait_tests/trait_supertrait_forwarded_wrong_impl_fail_test");
    }

    // --- Supertrait associated type constraints ---

    [Test]
    public void TraitSupertraitAssocConstraintPassTest()
    {
        // trait IntProducer: Producer<Output = i32> — i32 implements Producer with Output = i32
        AssertSuccess("trait_tests/trait_supertrait_assoc_constraint_pass_test", 42);
    }

    [Test]
    public void TraitSupertraitAssocConstraintFailTest()
    {
        // trait IntProducer: Producer<Output = i32> — but i32's Producer has Output = bool
        AssertFail("trait_tests/trait_supertrait_assoc_constraint_fail_test");
    }

    [Test]
    public void TraitSupertraitGenericAssocConstraintPassTest()
    {
        // trait Addable: Add(i32, Output = i32) — i32 satisfies Add(i32) with Output = i32
        AssertSuccess("trait_tests/trait_supertrait_generic_assoc_constraint_pass_test", 42);
    }

    [Test]
    public void TraitSupertraitGenericAssocConstraintFailTest()
    {
        // trait WantsString: Producer<Output = bool> — but i32's Producer has Output = i32
        AssertFail("trait_tests/trait_supertrait_generic_assoc_constraint_fail_test");
    }

    [Test]
    public void TraitSupertraitForwardedAssocPassTest()
    {
        // trait IntConverter<T>: Converter<T, Output = i32> — correctly constrained
        AssertSuccess("trait_tests/trait_supertrait_forwarded_assoc_pass_test", 42);
    }

    [Test]
    public void TraitSupertraitForwardedAssocFailTest()
    {
        // trait IntConverter<T>: Converter<T, Output = i32> — but impl has Output = bool
        AssertFail("trait_tests/trait_supertrait_forwarded_assoc_fail_test");
    }

    [Test]
    public void TraitSupertraitMultiAssocConstraintPassTest()
    {
        // trait I32Transformer: Transformer<Input = i32, Output = i32> — both match
        AssertSuccess("trait_tests/trait_supertrait_multi_assoc_constraint_pass_test", 42);
    }

    [Test]
    public void TraitSupertraitMultiAssocConstraintFailTest()
    {
        // trait I32Transformer: Transformer<Input = i32, Output = i32> — Output is bool, not i32
        AssertFail("trait_tests/trait_supertrait_multi_assoc_constraint_fail_test");
    }
}

public class AssociatedTypeConstraintTests
{
    // --- Basic associated type constraint on function bounds ---

    [Test]
    public void TraitAssocConstraintPassTest()
    {
        // fn consume<T: Producer<Output = i32>> — i32 implements Producer with Output = i32
        AssertSuccess("trait_tests/trait_assoc_constraint_pass_test", 42);
    }

    [Test]
    public void TraitAssocConstraintMismatchFailTest()
    {
        // fn consume<T: Producer<Output = i32>> — but i32's Producer has Output = bool
        AssertFail("trait_tests/trait_assoc_constraint_mismatch_fail_test");
    }

    // --- Self-referencing associated type constraint ---

    [Test]
    public void TraitAssocConstraintSelfRefPassTest()
    {
        // fn add_twice<T: Add(T, Output = T) + Copy> — i32 satisfies Add(i32, Output = i32)
        AssertSuccess("trait_tests/trait_assoc_constraint_self_ref_pass_test", 42);
    }

    // --- Extra associated type in impl (not defined in trait) ---

    [Test]
    public void TraitAssocExtraFailTest()
    {
        // impl Marker for i32 { type Bogus = bool; } — Marker has no associated types
        AssertFail("trait_tests/trait_assoc_extra_fail_test");
    }

    // --- Associated type bound violation (multi-field) ---

    [Test]
    public void TraitAssocBoundMultiFailTest()
    {
        // type Output: Copy but assigned NonCopy — one of two associated types violates bound
        AssertFail("trait_tests/trait_assoc_bound_multi_fail_test");
    }

    // --- Multiple associated type constraints on a single bound ---

    [Test]
    public void TraitAssocMultiConstraintPassTest()
    {
        // fn use<T: Transformer<Input = bool, Output = i32>> — both constraints match
        AssertSuccess("trait_tests/trait_assoc_multi_constraint_pass_test", 99);
    }

    [Test]
    public void TraitAssocMultiConstraintFailTest()
    {
        // fn use<T: Transformer<Input = i32, Output = i32>> — Input is actually bool, not i32
        AssertFail("trait_tests/trait_assoc_multi_constraint_fail_test");
    }

    // --- Associated type constraint on impl generic bound ---

    [Test]
    public void TraitAssocConstraintOnImplPassTest()
    {
        // impl<T: Add(T, Output = T) + Copy> Summable for Wrapper(T)
        AssertSuccess("trait_tests/trait_assoc_constraint_on_impl_pass_test", 42);
    }

    [Test]
    public void TraitAssocTypeSelfReturnTest()
    {
        // trait Foo { type Bruh; fn dude() -> Self.Bruh; }
        // impl Foo for i32 { type Bruh = Self; fn dude() -> (Self as Foo).Bruh { 4 } }
        // Self.Bruh and (Self as Foo).Bruh should resolve to the same concrete type
        AssertSuccess("trait_tests/assoc_type_self_return_test", 4);
    }
}

public class EqualityTests
{
    [Test] public void EqI32EqualTest() => AssertSuccess("trait_tests/eq_i32_equal_test", 1);
    [Test] public void EqI32NotEqualTest() => AssertSuccess("trait_tests/eq_i32_not_equal_test", 0);

    [Test]
    public void EqBoolTest()
    {
        // true==true → 1, true==false → 0, false==false → 100. Total 101
        AssertSuccess("trait_tests/eq_bool_test", 101);
    }

    [Test]
    public void EqVariableTest()
    {
        // 42==42 → 1, 42==99 → 0. Total 1
        AssertSuccess("trait_tests/eq_variable_test", 1);
    }

    [Test]
    public void EqInWhileTest()
    {
        // 5 iterations (+1 each) + when i==3 (+100) = 105
        AssertSuccess("trait_tests/eq_in_while_test", 105);
    }

    [Test]
    public void EqCustomTypeTest()
    {
        // Point{1,2}==Point{1,2} → 1, Point{1,2}==Point{3,4} → 0. Total 1
        AssertSuccess("trait_tests/eq_custom_type_test", 1);
    }

    [Test]
    public void EqNoImplFailTest()
    {
        // Foo has no PartialEq impl — == should fail
        AssertFail<GenericSemanticError>("trait_tests/eq_no_impl_fail_test");
    }
}

public class ComparisonTests
{
    [Test]
    public void OrdLessThanTest()
    {
        // 3<5 → 1, 5<3 → 0, 5<5 → 0. Total 1
        AssertSuccess("trait_tests/ord_less_than_test", 1);
    }

    [Test]
    public void OrdGreaterThanTest()
    {
        // 5>3 → 1, 3>5 → 0, 5>5 → 0. Total 1
        AssertSuccess("trait_tests/ord_greater_than_test", 1);
    }

    [Test]
    public void OrdChainedComparisonTest()
    {
        // clamp(50,0,100)=50 + clamp(200,0,100)=100 + clamp(-5,0,100)=0 = 150
        AssertSuccess("trait_tests/ord_chained_comparison_test", 150);
    }

    [Test]
    public void OrdCustomTypeTest()
    {
        // Num(5)<Num(10) → 1, Num(10)>Num(5) → 10, Num(5)==Num(5) → 100. Total 111
        AssertSuccess("trait_tests/ord_custom_type_test", 111);
    }

    [Test]
    public void OrdNoImplFailTest()
    {
        // Foo has no PartialOrd impl — < should fail
        AssertFail<GenericSemanticError>("trait_tests/ord_no_impl_fail_test");
    }
}

public class LogicalOperatorTests
{
    [Test]
    public void LogicalAndTest()
    {
        // T&&T=1, T&&F=0, F&&T=0, F&&F=0. Total 1
        AssertSuccess("trait_tests/logical_and_test", 1);
    }

    [Test]
    public void LogicalOrTest()
    {
        // T||T=1, T||F=10, F||T=100, F||F=0. Total 111
        AssertSuccess("trait_tests/logical_or_test", 111);
    }

    [Test]
    public void LogicalCombinedTest()
    {
        // a&&c=1, a||b=10, b&&c=0, b||b=0. Total 11
        AssertSuccess("trait_tests/logical_combined_test", 11);
    }

    [Test]
    public void LogicalWithComparisonTest()
    {
        // in_range(5,0,10)=true→1, in_range(15,0,10)=false→0, in_range(0,0,10)=false→0. Total 1
        AssertSuccess("trait_tests/logical_with_comparison_test", 1);
    }

    [Test]
    public void LogicalAndNonBoolFailTest()
    {
        // 5 && 3 — operands must be bool
        AssertFail<GenericSemanticError>("trait_tests/logical_and_non_bool_fail_test");
    }

    [Test]
    public void LogicalOrNonBoolFailTest()
    {
        // 5 || 3 — operands must be bool
        AssertFail<GenericSemanticError>("trait_tests/logical_or_non_bool_fail_test");
    }
}

public class OperatorPrecedenceTests
{
    [Test]
    public void PrecedenceMulBeforeAddTest()
    {
        // 2 + 3 * 4 = 2 + 12 = 14
        AssertSuccess("trait_tests/precedence_mul_before_add_test", 14);
    }

    [Test]
    public void PrecedenceAddBeforeCompareTest()
    {
        // (3 + 4) > 5 → true → 1
        AssertSuccess("trait_tests/precedence_add_before_compare_test", 1);
    }

    [Test]
    public void PrecedenceCompareBeforeAndTest()
    {
        // (3 < 5) && (10 > 7) → true && true → 1
        AssertSuccess("trait_tests/precedence_compare_before_and_test", 1);
    }

    [Test]
    public void PrecedenceAndBeforeOrTest()
    {
        // (false && true) || true → false || true → 1
        AssertSuccess("trait_tests/precedence_and_before_or_test", 1);
    }

    [Test]
    public void PrecedenceParensOverrideTest()
    {
        // (2 + 3) * 4 = 5 * 4 = 20
        AssertSuccess("trait_tests/precedence_parens_override_test", 20);
    }

    [Test]
    public void PrecedenceEqBeforeAndTest()
    {
        // (5 == 5) && (3 == 3) → true && true → 1
        AssertSuccess("trait_tests/precedence_eq_before_and_test", 1);
    }

    [Test]
    public void PrecedenceComplexExprTest()
    {
        // x=10,y=20: (10+20 > 25) && (10*2 == 20) → (30>25) && (20==20) → true && true → 1
        AssertSuccess("trait_tests/precedence_complex_expr_test", 1);
    }

    [Test]
    public void PrecedenceOrWithParensTest()
    {
        // false && (true || true) → false && true → false → 0
        AssertSuccess("trait_tests/precedence_or_with_parens_test", 0);
    }
}

public class DefaultTraitMethodTests
{
    [Test]
    public void DefaultMethodUsedTest()
    {
        // Impl provides no methods, trait has default → default used → 42
        AssertSuccess("trait_tests/default_method_used_test", 42);
    }

    [Test]
    public void DefaultMethodOverrideTest()
    {
        // Impl overrides default method → override wins → 99
        AssertSuccess("trait_tests/default_method_override_test", 99);
    }

    [Test]
    public void DefaultMethodMixedTest()
    {
        // required(10)=20, optional(10)=11 (default) → 31
        AssertSuccess("trait_tests/default_method_mixed_test", 31);
    }

    [Test]
    public void DefaultMethodMissingRequiredFailTest()
    {
        // Impl omits required method (no default) → compile error
        AssertFail<TraitMethodNotImplementedError>("trait_tests/default_method_missing_required_fail_test");
    }

    [Test]
    public void DefaultMethodNeUsedTest()
    {
        // Ne is default impl of PartialEq — 5 != 3 → true → 1
        AssertSuccess("trait_tests/default_method_ne_used_test", 1);
    }

    [Test]
    public void DefaultMethodNeEqualTest()
    {
        // 5 != 5 → false → 0
        AssertSuccess("trait_tests/default_method_ne_equal_test", 0);
    }

    [Test]
    public void DefaultMethodNeCustomTypeTest()
    {
        // Custom type implements Eq only, Ne uses default: Num(5)!=Num(5)→0, Num(5)!=Num(9)→1. Total 1
        AssertSuccess("trait_tests/default_method_ne_custom_type_test", 1);
    }
}

public class NegateOperatorTests
{
    [Test] public void NegateTrueTest() => AssertSuccess("trait_tests/negate_true_test", 0);
    [Test] public void NegateFalseTest() => AssertSuccess("trait_tests/negate_false_test", 1);
    [Test] public void NegateDoubleTest() => AssertSuccess("trait_tests/negate_double_test", 1);

    [Test]
    public void NegateComparisonTest()
    {
        // !(3 > 5) → !(false) → true → 1
        AssertSuccess("trait_tests/negate_comparison_test", 1);
    }

    [Test]
    public void NegateWithAndTest()
    {
        // true && !false → true && true → 1
        AssertSuccess("trait_tests/negate_with_and_test", 1);
    }

    [Test]
    public void NegateNonBoolFailTest()
    {
        // !5 — not bool, should fail
        AssertFail<GenericSemanticError>("trait_tests/negate_non_bool_fail_test");
    }
}

public class NotEqualsTests
{
    [Test]
    public void NeI32Test()
    {
        // 5!=3 → 1, 5!=5 → 0. Total 1
        AssertSuccess("trait_tests/ne_i32_test", 1);
    }

    [Test]
    public void NeBoolTest()
    {
        // true!=false → 1, true!=true → 0. Total 1
        AssertSuccess("trait_tests/ne_bool_test", 1);
    }

    [Test]
    public void NeInLoopTest()
    {
        // 0..9, skip 5 → 9 iterations counted
        AssertSuccess("trait_tests/ne_in_loop_test", 9);
    }

    [Test]
    public void NePrecedenceTest()
    {
        // (2+3 != 6) && (10 > 5) → (5!=6) && true → true && true → 1
        AssertSuccess("trait_tests/ne_precedence_test", 1);
    }

    [Test]
    public void NeNoImplFailTest()
    {
        // Foo has no PartialEq — != should fail
        AssertFail<GenericSemanticError>("trait_tests/ne_no_impl_fail_test");
    }
}

public class CombinedOperatorTests
{
    [Test]
    public void CombinedAllOperatorsTest()
    {
        // eq=1, ne=2, lt=4, gt=8, and=16, or=32, neg=64. Total 127
        AssertSuccess("trait_tests/combined_all_operators_test", 127);
    }

    [Test]
    public void CombinedComplexConditionTest()
    {
        // is_valid(25)=true→1, is_valid(50)=false→0, is_valid(0)=false→0, is_valid(100)=false→0. Total 1
        AssertSuccess("trait_tests/combined_complex_condition_test", 1);
    }
}

public class LessEqualGreaterEqualTests
{
    [Test]
    public void LeBasicTest()
    {
        // 3<=5 → 1, 5<=5 → 10, 7<=5 → 0. Total 11
        AssertSuccess("trait_tests/le_basic_test", 11);
    }

    [Test]
    public void GeBasicTest()
    {
        // 5>=3 → 1, 5>=5 → 10, 3>=5 → 0. Total 11
        AssertSuccess("trait_tests/ge_basic_test", 11);
    }

    [Test]
    public void LeGeBoundaryTest()
    {
        // clamp(0,0,100)=0 + clamp(100,0,100)=100 + clamp(50,0,100)=50 = 150
        AssertSuccess("trait_tests/le_ge_boundary_test", 150);
    }

    [Test]
    public void LeGeInWhileTest()
    {
        // sum 1+2+...+10 = 55
        AssertSuccess("trait_tests/le_ge_in_while_test", 55);
    }

    [Test]
    public void LeGeWithLogicalTest()
    {
        // in_range_inclusive: 5→1, 1→10, 10→100, 0→0, 11→0. Total 111
        AssertSuccess("trait_tests/le_ge_with_logical_test", 111);
    }

    [Test]
    public void LeGePrecedenceTest()
    {
        // (2+3 <= 5) && (10-1 >= 9) → (5<=5) && (9>=9) → true && true → 1
        AssertSuccess("trait_tests/le_ge_precedence_test", 1);
    }

    [Test]
    public void LeGeCustomTypeTest()
    {
        // Score(5)<=Score(10)→1, Score(5)<=Score(5)→10, Score(10)>=Score(5)→100, Score(5)>=Score(5)→1000. Total 1111
        AssertSuccess("trait_tests/le_ge_custom_type_test", 1111);
    }
}

public class ReferenceComparisonTests
{
    [Test]
    public void EqRefSharedTest()
    {
        // &42 == &42 → 1, &42 == &99 → 0. Total 1
        AssertSuccess("trait_tests/eq_ref_shared_test", 1);
    }

    [Test]
    public void EqRefMutTest()
    {
        // &mut 42 == &mut 42 → 1
        AssertSuccess("trait_tests/eq_ref_mut_test", 1);
    }

    [Test]
    public void NeRefTest()
    {
        // &10 != &20 → 1
        AssertSuccess("trait_tests/ne_ref_test", 1);
    }

    [Test]
    public void OrdRefSharedTest()
    {
        // &3<&7→1, &7>&3→10, &3<=&7→100, &7>=&3→1000. Total 1111
        AssertSuccess("trait_tests/ord_ref_shared_test", 1111);
    }

    [Test]
    public void EqRefFnParamTest()
    {
        // are_equal(&5, &5)→1, are_equal(&5, &9)→0. Total 1
        AssertSuccess("trait_tests/eq_ref_fn_param_test", 1);
    }

    [Test]
    public void OrdRefFnParamTest()
    {
        // is_less(&3, &7)→1, is_less(&7, &3)→0. Total 1
        AssertSuccess("trait_tests/ord_ref_fn_param_test", 1);
    }
}

public class AllComparisonOpsTests
{
    [Test]
    public void AllComparisonOpsTest()
    {
        // ==1, !=2, <4, >8, <=16, <=32, >=64, >=128. Total 255
        AssertSuccess("trait_tests/all_comparison_ops_test", 255);
    }

    [Test]
    public void ComparisonChainTest()
    {
        // is_sorted3(1,2,3)→1, (1,1,1)→10, (3,2,1)→0, (1,3,2)→0. Total 11
        AssertSuccess("trait_tests/comparison_chain_test", 11);
    }

    [Test]
    public void BinarySearchTest()
    {
        // floor(sqrt(100)) = 10
        AssertSuccess("trait_tests/binary_search_test", 10);
    }
}

public class NestedRefComparisonTests
{
    [Test]
    public void EqDoubleRefTest()
    {
        // &&42 == &&42 → 1
        AssertSuccess("trait_tests/eq_double_ref_test", 1);
    }

    [Test]
    public void EqMutRefInnerTest()
    {
        // &(&mut 10) == &(&mut 10) → 1
        AssertSuccess("trait_tests/eq_mut_ref_inner_test", 1);
    }

    [Test]
    public void OrdDoubleRefTest()
    {
        // &&3 < &&7 → 1, &&7 > &&3 → 10. Total 11
        AssertSuccess("trait_tests/ord_double_ref_test", 11);
    }
}

public class GenericComparisonTests
{
    [Test]
    public void GenericEqConstraintTest()
    {
        // are_equal[i32](&42, &42) → 1, (&42, &99) → 0. Total 1
        AssertSuccess("trait_tests/generic_eq_constraint_test", 1);
    }

    [Test]
    public void GenericOrdConstraintTest()
    {
        // max_of(10,20)=20 + max_of(5,3)=5 = 25
        AssertSuccess("trait_tests/generic_ord_constraint_test", 25);
    }

    [Test]
    public void GenericOrdRefConstraintTest()
    {
        // clamp(50,0,100)=50 + clamp(200,0,100)=100 + clamp(-5,0,100)=0 = 150
        AssertSuccess("trait_tests/generic_ord_ref_constraint_test", 150);
    }

    [Test]
    public void GenericEqNoConstraintFailTest()
    {
        // T:! type has no PartialEq bound — == should fail
        AssertFail<GenericSemanticError>("trait_tests/generic_eq_no_constraint_fail_test");
    }
}

public class ComparisonBorrowTests
{
    [Test]
    public void EqNoMoveTest()
    {
        // Copy struct compared 3 times — all succeed → 111
        AssertSuccess("trait_tests/eq_no_move_test", 111);
    }

    [Test]
    public void EqNoMoveNonCopyTest()
    {
        // Non-copy struct compared twice — takes &Self so no move → 1
        AssertSuccess("trait_tests/eq_no_move_noncopy_test", 1);
    }

    [Test]
    public void ComparisonPreservesValuesTest()
    {
        // All 6 comparison ops used, then a + b still accessible → 30
        AssertSuccess("trait_tests/comparison_preserves_values_test", 30);
    }

    [Test]
    public void AllSixComparisonOpsTest()
    {
        // ==1, !=2, <4, >8, <=16, <=32, >=64, >=128. Total 255
        AssertSuccess("trait_tests/all_six_comparison_ops_test", 255);
    }
}

public class ByValParamAutoAdjustTests
{
    [Test]
    public void EqCustomByvalParamsTest()
    {
        // Point eq by val: ==→1, ==miss→0, !=hit→100, !=miss→0. Total 101
        AssertSuccess("trait_tests/eq_custom_byval_params_test", 101);
    }

    [Test]
    public void OrdCustomByvalParamsTest()
    {
        // Score all ops by val: <1, >2, <=4, <=8, >=16, >=32, ==64, !=128. Total 255
        AssertSuccess("trait_tests/ord_custom_byval_params_test", 255);
    }

    [Test]
    public void EqCustomRefParamsTest()
    {
        // Explicit &Num params: ==→1, !=→10. Total 11
        AssertSuccess("trait_tests/eq_custom_ref_params_test", 11);
    }

    [Test]
    public void EqNonCopyByvalMultiCompareTest()
    {
        // Non-copy struct compared 3 times — takes &Self so no move: ==→1, ==miss→0, !=→100. Total 101
        AssertSuccess("trait_tests/eq_noncopy_byval_multi_compare_test", 101);
    }

    [Test]
    public void AllOpsCustomByvalTest()
    {
        // All 6 comparison ops on custom type with by-value params. Total 255
        AssertSuccess("trait_tests/all_ops_custom_byval_test", 255);
    }

    [Test]
    public void GenericCustomByvalTest()
    {
        // Custom type with by-val params used through generic max_of → 200
        AssertSuccess("trait_tests/generic_custom_byval_test", 200);
    }
}

public class ComparisonBorrowVsArithmeticMoveTests
{
    [Test]
    public void EqNonCopyNoMoveTest()
    {
        // Non-Copy struct compared 3 times — == borrows, doesn't move → 11
        AssertSuccess("trait_tests/eq_noncopy_no_move_test", 11);
    }

    [Test]
    public void AddNonCopyMovesFailTest()
    {
        // Non-Copy struct: a + b moves both, then a + b again → use after move
        AssertFail<UseAfterMoveError>("trait_tests/add_noncopy_moves_fail_test");
    }

    [Test]
    public void SubNonCopyMovesFailTest()
    {
        // Non-Copy struct: a - b moves both, then a - b again → use after move
        AssertFail<UseAfterMoveError>("trait_tests/sub_noncopy_moves_fail_test");
    }

    [Test]
    public void LtNonCopyNoMoveTest()
    {
        // Non-Copy struct: all 6 comparison ops used — none move → 11
        AssertSuccess("trait_tests/lt_noncopy_no_move_test", 11);
    }

    [Test]
    public void EqUniqBorrowNllReleasedTest()
    {
        // &uniq a exists but u is never used after == → NLL releases borrow → OK
        AssertSuccess("trait_tests/eq_uniq_borrow_nll_released_test", 0);
    }

    [Test]
    public void EqConflictsWithLiveUniqFailTest()
    {
        // &uniq a is used after a == 5 → shared borrow invalidates exclusive → *u fails
        AssertFail<BorrowInvalidatedError>("trait_tests/eq_conflicts_with_live_uniq_fail_test");
    }

    [Test]
    public void EqAfterUniqReleasedTest()
    {
        // &uniq borrow released, then a == 10 → OK
        AssertSuccess("trait_tests/eq_after_uniq_released_test", 1);
    }

    [Test]
    public void EqCoexistsWithSharedBorrowTest()
    {
        // a == 42 while &a exists — shared borrows coexist
        AssertSuccess("trait_tests/eq_coexists_with_shared_borrow_test", 1);
    }

    [Test]
    public void EqCoexistsWithMutBorrowTest()
    {
        // a == 42 while &mut a exists — shared and mut coexist
        AssertSuccess("trait_tests/eq_coexists_with_mut_borrow_test", 1);
    }
}

public class TryCastPrimitiveTests
{
    [Test]
    public void TryCastI32ToU8SuccessTest()
    {
        AssertSuccess("trait_tests/try_cast_i32_to_u8_success_test", 1);
    }

    [Test]
    public void TryCastI32ToU8OverflowTest()
    {
        // 256 doesn't fit in u8 → None → 1
        AssertSuccess("trait_tests/try_cast_i32_to_u8_overflow_test", 1);
    }

    [Test]
    public void TryCastI32ToU8NegativeTest()
    {
        // -1 doesn't fit in u8 → None → 1
        AssertSuccess("trait_tests/try_cast_i32_to_u8_negative_test", 1);
    }

    [Test]
    public void TryCastU8ToI32Test()
    {
        // u8 200 always fits in i32 → Some(200) → 200
        AssertSuccess("trait_tests/try_cast_u8_to_i32_test", 200);
    }

    [Test]
    public void TryCastI32ToUsizeTest()
    {
        // 100 fits in usize → Some → 1
        AssertSuccess("trait_tests/try_cast_i32_to_usize_test", 1);
    }

    [Test]
    public void TryCastI32NegativeToUsizeTest()
    {
        // -5 doesn't fit in usize → None → 1
        AssertSuccess("trait_tests/try_cast_i32_negative_to_usize_test", 1);
    }

    [Test]
    public void TryCastU8ToUsizeTest()
    {
        // u8 255 always fits in usize → Some → 1
        AssertSuccess("trait_tests/try_cast_u8_to_usize_test", 1);
    }

    [Test]
    public void TryCastI32BoundaryTest()
    {
        // 0→Some(+1), 255→Some(+10), 256→None(+100) = 111
        AssertSuccess("trait_tests/try_cast_i32_boundary_test", 111);
    }
}

public class TryIntoTests
{
    [Test]
    public void TryIntoI32ToU8Test()
    {
        // 42.TryInto() → Some → 1
        AssertSuccess("trait_tests/try_into_i32_to_u8_test", 1);
    }

    [Test]
    public void TryIntoI32ToUsizeTest()
    {
        // 100.TryInto() → Some → 1
        AssertSuccess("trait_tests/try_into_i32_to_usize_test", 1);
    }

    [Test]
    public void TryIntoU8ToI32Test()
    {
        // 123.TryInto() → Some(123) → 123
        AssertSuccess("trait_tests/try_into_u8_to_i32_test", 123);
    }

    [Test]
    public void TryIntoOverflowTest()
    {
        // 300.TryInto() as u8 → None → 1
        AssertSuccess("trait_tests/try_into_overflow_test", 1);
    }

    [Test]
    public void TryIntoNegativeToUnsignedTest()
    {
        // -10 → u8 None(+1), usize None(+10) = 11
        AssertSuccess("trait_tests/try_into_negative_to_unsigned_test", 11);
    }
}
