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
    [Test] public void TraitQualifiedTurbofishTest() => AssertSuccess("trait_tests/trait_qualified_turbofish_test", 42);
    [Test] public void TraitGenericParamTurbofishTest() => AssertSuccess("trait_tests/trait_generic_param_turbofish_test", 7);
    [Test] public void TraitShorthandTurbofishTest() => AssertSuccess("trait_tests/trait_shorthand_turbofish_test", 13);
    [Test] public void TraitBothTurbofishTest() => AssertSuccess("trait_tests/trait_both_turbofish_test", 10);
    [Test] public void TraitTurbofishSemanticTest() => AssertSuccess("trait_tests/trait_turbofish_semantic_test");
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
