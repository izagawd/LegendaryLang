using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class TraitTests
{
    [Test]
    public void TraitBasicTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_basic_test", true, true);
        Assert.That(result.Success);
        Assert.That(40 == result.Function?.Invoke());
    }

    [Test]
    public void TraitGenericParamCallTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_generic_param_call_test", true, true);
        Assert.That(result.Success);
        Assert.That(3 == result.Function?.Invoke());
    }

    [Test]
    public void TraitBoundViolationTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_bound_violation_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<TraitBoundViolationError>());

        var violations = result.GetErrors<TraitBoundViolationError>().ToList();
        Assert.That(violations.Count == 1);
        Assert.That(violations[0].TypePath.ToString().Contains("bool"));
        Assert.That(violations[0].TraitPath.ToString().Contains("Foo"));
    }

    [Test]
    public void TraitEmptyBoundTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_empty_bound_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void TraitMultiBoundTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_multi_bound_test", true, true);
        Assert.That(result.Success);
        Assert.That(16 == result.Function?.Invoke());
    }

    [Test]
    public void TraitMultiBoundViolationTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_multi_bound_violation_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<TraitBoundViolationError>());

        var violations = result.GetErrors<TraitBoundViolationError>().ToList();
        Assert.That(violations.Count == 2);

        var traitNames = violations.Select(v => v.TraitPath.ToString()).ToList();
        Assert.That(traitNames.Any(t => t.Contains("Adder")));
        Assert.That(traitNames.Any(t => t.Contains("Multiplier")));
        Assert.That(violations.All(v => v.TypePath.ToString().Contains("bool")));
    }

    [Test]
    public void TraitDuplicateGenericParamTest()
    {
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_duplicate_generic_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<GenericSemanticError>());
        Assert.That(result.HasError<GenericSemanticError>());
    }

    [Test]
    public void TraitQualifiedCallTest()
    {
        // <i32 as Default>::default() should resolve Self to i32
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_qualified_call_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void TraitConcreteTypeCallTest()
    {
        // i32::default() should find the impl and resolve to 99
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_concrete_type_call_test", true, true);
        Assert.That(result.Success);
        Assert.That(99 == result.Function?.Invoke());
    }

    [Test]
    public void TraitImplMethodExtraBoundsFailTest()
    {
        // impl adds bounds not in trait definition — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_impl_method_extra_bounds_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<TraitImplBoundsMismatchError>());
    }

    [Test]
    public void TraitImplMethodMissingBoundsFailTest()
    {
        // impl is missing bounds required by trait — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_impl_method_missing_bounds_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<TraitImplBoundsMismatchError>());
    }

    [Test]
    public void TraitImplMethodGenericCountFailTest()
    {
        // impl has different generic param count than trait — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_impl_method_generic_count_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<TraitImplBoundsMismatchError>());
    }

    [Test]
    public void TraitImplMethodMatchingBoundsTest()
    {
        // impl method matches trait definition — should pass
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_impl_method_matching_bounds_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void TraitGenericBoundPropagationFailTest()
    {
        // bar<T: Foo + Bar> called inside idk<T: Foo> — T lacks Bar bound
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_generic_bound_propagation_fail_test", true, true);
        Assert.That(!result.Success);
    }

    [Test]
    public void TraitGenericBoundPropagationPassTest()
    {
        // bar<T: Foo + Bar> called inside idk<T: Foo + Bar> — bounds satisfied
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_generic_bound_propagation_pass_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void TraitQualifiedTurbofishTest()
    {
        // <i32 as Foo>::kk::<i32>() — qualified call with method-level turbofish
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_qualified_turbofish_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void TraitGenericParamTurbofishTest()
    {
        // <T as Foo>::kk::<U>() inside generic fn — qualified generic param turbofish
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_generic_param_turbofish_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void TraitShorthandTurbofishTest()
    {
        // T::kk::<U>() — shorthand turbofish on generic param
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_shorthand_turbofish_test", true, true);
        Assert.That(result.Success);
        Assert.That(13 == result.Function?.Invoke());
    }

    [Test]
    public void TraitBothTurbofishTest()
    {
        // <T as Foo>::kk::<U>() + T::kk::<U>() — both forms in same fn
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_both_turbofish_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void TraitTurbofishSemanticTest()
    {
        // Just checks that turbofish trait calls pass semantic analysis
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_turbofish_semantic_test", true, true);
        Assert.That(result.Success);
    }

    [Test]
    public void TraitSupertraitBasicTest()
    {
        // T: Sub where Sub: Base — needs_base::<T>() should work via supertrait
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_supertrait_basic_test", true, true);
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke());
    }

    [Test]
    public void TraitSupertraitTransitiveTest()
    {
        // C: B, B: A — needs_a::<T>() works when T: C (transitive)
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_supertrait_transitive_test", true, true);
        Assert.That(result.Success);
        Assert.That(4 == result.Function?.Invoke());
    }

    [Test]
    public void TraitSupertraitFailTest()
    {
        // T: Base does NOT satisfy T: Sub — should fail
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_supertrait_fail_test", true, true);
        Assert.That(!result.Success);
    }

    [Test]
    public void TraitSupertraitMultiTest()
    {
        // Both: Foo + Bar — T: Both satisfies both Foo and Bar
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_supertrait_multi_test", true, true);
        Assert.That(result.Success);
        Assert.That(6 == result.Function?.Invoke());
    }
}

public class MethodCallTests
{
    [Test]
    public void TraitMethodCallTest()
    {
        // f.value() where Foo impl Greet with self: &Self
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_method_call_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void TraitMethodCallI32Test()
    {
        // x.double() where i32 impl Double
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_method_call_i32_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void TraitMethodCallWithArgsTest()
    {
        // v.add_to(5) with explicit extra arg
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_method_call_with_args_test", true, true);
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke());
    }

    [Test]
    public void TraitMethodChainTest()
    {
        // w.get() on Wrapper
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_method_chain_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void TraitReceiverSupertraitTest()
    {
        // T: Child where Child: Base — use_base::<T>() works
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_receiver_supertrait_test", true, true);
        Assert.That(result.Success);
        Assert.That(30 == result.Function?.Invoke());
    }
}

public class SupertraitGenericTests
{
    [Test]
    public void TraitSupertraitGenericPassthroughTest()
    {
        // trait Foo<T>: Bar<T> — T: Foo<U> satisfies Bar<U>
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_supertrait_generic_passthrough_test", true, true);
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke());
    }

    [Test]
    public void TraitSupertraitGenericWithPlainTest()
    {
        // trait Foo<T>: Bar (non-generic supertrait) — T: Foo<U> satisfies Bar
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_supertrait_generic_with_plain_test", true, true);
        Assert.That(result.Success);
        Assert.That(23 == result.Function?.Invoke());
    }

    [Test]
    public void TraitSupertraitConcreteGenericTest()
    {
        // trait Foo: Bar<i32> — T: Foo satisfies Bar<i32>
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_supertrait_concrete_generic_test", true, true);
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke());
    }

    [Test]
    public void TraitSupertraitGenericMismatchFailTest()
    {
        // trait Foo: Bar<i32> — T: Foo should NOT satisfy Bar<bool>
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_supertrait_generic_mismatch_fail_test", true, true);
        Assert.That(!result.Success);
    }

    [Test]
    public void TraitSupertraitDeepGenericTest()
    {
        // C<T>: B<T>, B<T>: A<T> — T: C<U> satisfies A<U> transitively
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_supertrait_deep_generic_test", true, true);
        Assert.That(result.Success);
        Assert.That(4 == result.Function?.Invoke());
    }
}

public class MethodChainTests
{
    [Test]
    public void TraitMethodChainCallTest()
    {
        // x.get().get() — chained i32 method calls
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_method_chain_call_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void TraitMethodChainStructTest()
    {
        // n.inc().get() — inc returns Num, then get returns i32
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_method_chain_struct_test", true, true);
        Assert.That(result.Success);
        Assert.That(6 == result.Function?.Invoke());
    }
}

public class SelfInBoundsTests
{
    [Test]
    public void TraitMethodSelfBoundTest()
    {
        // fn add<T: Add<Self, Output=Self>>(self: &Self, input: T) -> Self — method call with Self in bounds
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_method_self_bound_test", true, true);
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke());
    }

    [Test]
    public void TraitSelfBoundQualifiedTest()
    {
        // <i32 as Idk>::add(5, 10) — qualified call with Self in bounds
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_self_bound_qualified_test", true, true);
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke());
    }

    [Test]
    public void TraitSelfBoundMismatchFailTest()
    {
        // Trait has Self arg type but impl has bool — parameter type mismatch
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_self_bound_mismatch_fail_test", true, true);
        Assert.That(!result.Success);
    }

    [Test]
    public void TraitSelfReturnAndBoundTest()
    {
        // Self in both return type and generic bound
        var result = Compiler.CompileWithResult(
            "compiler_tests/trait_tests/trait_self_return_and_bound_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }
}
