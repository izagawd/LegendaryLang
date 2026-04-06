using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class EnumTests
{
    [Test] public void EnumBasicTest() => AssertSuccess("enum_tests/enum_basic_test", 2);

    [Test] public void EnumTupleVariantTest() => AssertSuccess("enum_tests/enum_tuple_variant_test", 42);

    [Test] public void EnumGenericTest() => AssertSuccess("enum_tests/enum_generic_test", 7);

    [Test] public void EnumMultiGenericTest() => AssertSuccess("enum_tests/enum_multi_generic_test", 5);

    [Test] public void EnumWildcardTest() => AssertSuccess("enum_tests/enum_wildcard_test", 99);

    [Test] public void EnumNestedTest() => AssertSuccess("enum_tests/enum_nested_test", 10);

    [Test] public void EnumNonExhaustiveFailTest() => AssertFail<NonExhaustiveMatchError>("enum_tests/enum_non_exhaustive_fail_test");

    [Test] public void EnumMultiFieldVariantTest() => AssertSuccess("enum_tests/enum_multi_field_variant_test", 10);

    [Test] public void EnumFnArgTest() => AssertSuccess("enum_tests/enum_fn_arg_test", 15);

    [Test] public void EnumFnReturnTest() => AssertSuccess("enum_tests/enum_fn_return_test", 20);

    [Test] public void EnumDuplicateVariantFailTest() => AssertFail<GenericSemanticError>("enum_tests/enum_duplicate_variant_fail_test");

    [Test] public void EnumWrongFieldCountFailTest() => AssertFail<GenericSemanticError>("enum_tests/enum_wrong_field_count_fail_test");

    [Test] public void EnumMatchReturnInArmTest() => AssertSuccess("enum_tests/enum_match_return_in_arm_test");

    [Test] public void EnumMatchAsExprTest() => AssertSuccess("enum_tests/enum_match_as_expr_test", 2);
}
