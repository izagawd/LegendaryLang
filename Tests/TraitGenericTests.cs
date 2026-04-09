using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class TraitGenericTests
{
    [Test] public void TraitGenericTraitTest() => AssertSuccess("trait_tests/trait_generic_trait_test", 5);

    [Test] public void TraitMultiGenericTraitTest() => AssertSuccess("trait_tests/trait_multi_generic_trait_test", 30);

    [Test] public void TraitAssocTypeBasicTest() => AssertSuccess("trait_tests/trait_assoc_type_basic_test", 42);

    [Test] public void TraitAssocTypeBoundTest() => AssertSuccess("trait_tests/trait_assoc_type_bound_test", 7);

    [Test] public void TraitAssocTypeBoundFailTest() => AssertFail<GenericSemanticError>("trait_tests/trait_assoc_type_bound_fail_test");

    [Test] public void TraitAssocTypeMissingTest() => AssertFail<GenericSemanticError>("trait_tests/trait_assoc_type_missing_test");

    [Test] public void TraitMultiAssocTypeTest() => AssertSuccess("trait_tests/trait_multi_assoc_type_test", 10);

    [Test] public void TraitGenericBoundWithGenericArgTest() => AssertSuccess("trait_tests/trait_generic_bound_with_generic_arg_test", 5);

    [Test] public void TraitImplGenericBoundWithGenericArgTest() => AssertSuccess("trait_tests/trait_impl_generic_bound_with_generic_arg_test", 10);
}

public class OperatorTraitTests
{
    [Test] public void OpsAddI32Test() => AssertSuccess("trait_tests/ops_add_i32_test", 10);

    [Test] public void OpsAllFourTest() => AssertSuccess("trait_tests/ops_all_four_test", 4);

    [Test] public void OpsNoImplFailTest() => AssertFail<GenericSemanticError>("trait_tests/ops_no_impl_fail_test");

    [Test]
    public void OpsCustomAddTest()
    {
        // Vec2 + Vec2 via custom impl Add(Vec2) for Vec2
        // (1+3) + (2+4) = 10
        var result = Compile("trait_tests/ops_custom_add_test");
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test] public void OpsCustomTypeAddOperatorTest() => AssertSuccess("trait_tests/ops_custom_type_add_test", 5);

    [Test] public void OpsPrimitiveLhsCustomRhsTest() => AssertSuccess("trait_tests/ops_primitive_lhs_custom_rhs_test", 4);

    [Test] public void OpsNestedCustomAddTest() => AssertSuccess("trait_tests/ops_nested_custom_add_test", 5);

    [Test] public void OpsNestedPrimitiveLhsTest() => AssertSuccess("trait_tests/ops_nested_primitive_lhs_test", 4);
}
