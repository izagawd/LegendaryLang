using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class EdgeCaseTests
{
    [Test] public void ShadowTest() => AssertSuccess("edge_case_tests/shadow_test", 10);

    [Test] public void BlockExprValueTest() => AssertSuccess("edge_case_tests/block_expr_value_test", 7);

    [Test] public void IfElseExprTest() => AssertSuccess("edge_case_tests/if_else_expr_test", 10);

    [Test] public void OperatorPrecedenceTest() => AssertSuccess("edge_case_tests/operator_precedence_test", 14);

    [Test] public void NestedBlocksTest() => AssertSuccess("edge_case_tests/nested_blocks_test", 7);

    [Test] public void OperatorChainTest() => AssertSuccess("edge_case_tests/operator_chain_test", 15);

    [Test] public void RecursiveFactorialTest() => AssertSuccess("edge_case_tests/recursive_factorial_test", 120);

    [Test] public void EnumMoveTest() => AssertSuccess("edge_case_tests/enum_move_test", 5);

    [Test] public void EnumUseAfterMoveFailTest() => AssertFail<UseAfterMoveError>("edge_case_tests/enum_use_after_move_fail_test");

    [Test] public void MatchImplicitReturnTest() => AssertSuccess("edge_case_tests/match_implicit_return_test", 42);

    [Test] public void NestedMatchTest() => AssertSuccess("edge_case_tests/nested_match_test", 2);

    [Test] public void NestedFnCallTest() => AssertSuccess("edge_case_tests/nested_fn_call_test", 12);

    [Test] public void GenericMultiInstantiationTest() => AssertSuccess("edge_case_tests/generic_multi_instantiation_test", 15);

    [Test] public void EnumStructPayloadTest() => AssertSuccess("edge_case_tests/enum_struct_payload_test", 7);

    [Test] public void ChainedFieldAccessTest() => AssertSuccess("edge_case_tests/chained_field_access_test", 99);

    [Test] public void EarlyReturnTest() => AssertSuccess("edge_case_tests/early_return_test", 104);

    [Test] public void MultiReturnPathTest() => AssertSuccess("edge_case_tests/multi_return_path_test", 6);

    [Test] public void EmptyStructTest() => AssertSuccess("edge_case_tests/empty_struct_test", 42);

    [Test] public void EnumAllUnitMatchTest() => AssertSuccess("edge_case_tests/enum_all_unit_match_test", 10);

    [Test] public void EnumGenericMatchComputeTest() => AssertSuccess("edge_case_tests/enum_generic_match_compute_test", 10);

    [Test] public void GenericStructFnArgTest() => AssertSuccess("edge_case_tests/generic_struct_fn_arg_test", 55);
}
