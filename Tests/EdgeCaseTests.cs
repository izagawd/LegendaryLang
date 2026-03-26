using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class EdgeCaseTests
{
    [Test]
    public void ShadowTest()
    {
        // let a = 5; let a = 10; a — shadowing returns latest binding
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/shadow_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void BlockExprValueTest()
    {
        // let a = { let x = 3; let y = 4; x + y }; a — block as value expression
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/block_expr_value_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void IfElseExprTest()
    {
        // let a = if 3 > 2 { 10 } else { 20 }; a — if-else as expression
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/if_else_expr_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void OperatorPrecedenceTest()
    {
        // 2 + 3 * 4 should be 14, not 20
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/operator_precedence_test", true, true);
        Assert.That(result.Success);
        Assert.That(14 == result.Function?.Invoke());
    }

    [Test]
    public void NestedBlocksTest()
    {
        // {{{ let x = 5; { x + { 2 } } }}} — deeply nested blocks
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/nested_blocks_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void OperatorChainTest()
    {
        // 1 + 2 + 3 + 4 + 5 = 15
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/operator_chain_test", true, true);
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke());
    }

    [Test]
    public void RecursiveFactorialTest()
    {
        // factorial(5) = 120
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/recursive_factorial_test", true, true);
        Assert.That(result.Success);
        Assert.That(120 == result.Function?.Invoke());
    }

    [Test]
    public void EnumMoveTest()
    {
        // Enum without Copy: let y = x; consume(y) — valid move chain
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/enum_move_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void EnumUseAfterMoveFailTest()
    {
        // Enum without Copy: consume(x); consume(x) — second use after move
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/enum_use_after_move_fail_test", true, true);
        Assert.That(!result.Success);
        Assert.That(result.HasError<UseAfterMoveError>());
    }

    [Test]
    public void MatchImplicitReturnTest()
    {
        // match as last expression (implicit return, no semicolon)
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/match_implicit_return_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void NestedMatchTest()
    {
        // match inside match arm body
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/nested_match_test", true, true);
        Assert.That(result.Success);
        Assert.That(2 == result.Function?.Invoke());
    }

    [Test]
    public void NestedFnCallTest()
    {
        // double(add_one(5)) = 12
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/nested_fn_call_test", true, true);
        Assert.That(result.Success);
        Assert.That(12 == result.Function?.Invoke());
    }

    [Test]
    public void GenericMultiInstantiationTest()
    {
        // identity::<i32>(5) + identity::<i32>(10) = 15
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/generic_multi_instantiation_test", true, true);
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke());
    }

    [Test]
    public void EnumStructPayloadTest()
    {
        // Struct inside enum variant payload — Shape::Dot(Point{3,4}) => 7
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/enum_struct_payload_test", true, true);
        Assert.That(result.Success);
        Assert.That(7 == result.Function?.Invoke());
    }

    [Test]
    public void ChainedFieldAccessTest()
    {
        // o.inner.val = 99
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/chained_field_access_test", true, true);
        Assert.That(result.Success);
        Assert.That(99 == result.Function?.Invoke());
    }

    [Test]
    public void EarlyReturnTest()
    {
        // check(5) = 5, check(20) = 99 → 104
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/early_return_test", true, true);
        Assert.That(result.Success);
        Assert.That(104 == result.Function?.Invoke());
    }

    [Test]
    public void MultiReturnPathTest()
    {
        // classify(0)=0 + classify(5)=1 + classify(50)=2 + classify(200)=3 = 6
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/multi_return_path_test", true, true);
        Assert.That(result.Success);
        Assert.That(6 == result.Function?.Invoke());
    }

    [Test]
    public void EmptyStructTest()
    {
        // Zero-field struct passed to function
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/empty_struct_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void EnumAllUnitMatchTest()
    {
        // 4-variant all-unit enum, sum all scores = 1+2+3+4 = 10
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/enum_all_unit_match_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void EnumGenericMatchComputeTest()
    {
        // unwrap_or(Just(7), 0) + unwrap_or(Nothing, 3) = 10
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/enum_generic_match_compute_test", true, true);
        Assert.That(result.Success);
        Assert.That(10 == result.Function?.Invoke());
    }

    [Test]
    public void GenericStructFnArgTest()
    {
        // Generic struct Wrapper<i32> passed to generic fn extract<i32>
        var result = Compiler.CompileWithResult(
            "compiler_tests/edge_case_tests/generic_struct_fn_arg_test", true, true);
        Assert.That(result.Success);
        Assert.That(55 == result.Function?.Invoke());
    }
}
