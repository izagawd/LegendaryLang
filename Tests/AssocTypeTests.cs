using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class AssocTypeTests
{
    [Test] public void AssocQualifiedLetTest() => AssertSuccess("assoc_type_tests/assoc_qualified_let_test", 5);

    [Test] public void AssocQualifiedNestedTest() => AssertSuccess("assoc_type_tests/assoc_qualified_nested_test", 7);

    [Test] public void AssocTripleNestedTest() => AssertSuccess("assoc_type_tests/assoc_triple_nested_test", 3);

    [Test] public void AssocFnReturnTypeTest() => AssertSuccess("assoc_type_tests/assoc_fn_return_type_test", 7);

    [Test] public void AssocSelfInImplTest() => AssertSuccess("assoc_type_tests/assoc_self_in_impl_test", 10);

    [Test] public void AssocNoBoundFailTest() => AssertFail("assoc_type_tests/assoc_no_bound_fail_test");

    [Test] public void AssocWrongTraitFailTest() => AssertFail("assoc_type_tests/assoc_wrong_trait_fail_test");

    [Test] public void AssocQualifiedInNestedFnTest() => AssertSuccess("assoc_type_tests/assoc_qualified_in_nested_fn_test", 12);

    [Test] public void AssocCustomTraitQualifiedTest() => AssertSuccess("assoc_type_tests/assoc_custom_trait_qualified_test", 42);

    [Test] public void AssocTraitNotFoundFailTest() => AssertFail("assoc_type_tests/assoc_trait_not_found_fail_test");

    [Test] public void AssocTypeMismatchFailTest() => AssertFail("assoc_type_tests/assoc_type_mismatch_fail_test");

    [Test] public void AssocQualifiedArgTypeTest() => AssertSuccess("assoc_type_tests/assoc_qualified_arg_type_test", 11);

    [Test] public void AssocInStructFieldTest() => AssertSuccess("assoc_type_tests/assoc_in_struct_field_test", 13);

    [Test] public void AssocGenericOperatorOutputTest() => AssertSuccess("assoc_type_tests/assoc_generic_operator_output_test", 16);

    [Test] public void AssocGenericSubOutputTest() => AssertSuccess("assoc_type_tests/assoc_generic_sub_output_test", 7);

    [Test] public void AssocImplBoundMismatchFailTest() =>
        AssertFail<GenericSemanticError>("assoc_type_tests/assoc_impl_bound_mismatch_fail_test");

    [Test] public void AssocImplBoundMatchOkTest() =>
        AssertSuccess("assoc_type_tests/assoc_impl_bound_match_ok_test", 0);

    [Test] public void AssocImplBoundStricterFailTest() =>
        AssertFail<GenericSemanticError>("assoc_type_tests/assoc_impl_bound_stricter_fail_test");

    [Test] public void AssocImplBoundMetasizedMatchOkTest() =>
        AssertSuccess("assoc_type_tests/assoc_impl_bound_metasized_match_ok_test", 0);

    [Test] public void AssocImplBoundWiderFailTest() =>
        AssertFail<GenericSemanticError>("assoc_type_tests/assoc_impl_bound_wider_fail_test");
}
