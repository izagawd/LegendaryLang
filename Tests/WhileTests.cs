using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class WhileTests
{
    [Test]
    public void WhileBasicCountTest()
    {
        // Count from 0 to 5
        AssertSuccess("while_tests/while_basic_count_test", 5);
    }

    [Test]
    public void WhileSumTest()
    {
        // Sum 0+1+2+...+9 = 45
        AssertSuccess("while_tests/while_sum_test", 45);
    }

    [Test]
    public void WhileZeroIterationsTest()
    {
        // Condition false from the start — body never runs
        AssertSuccess("while_tests/while_zero_iterations_test", 100);
    }

    [Test]
    public void WhileSingleIterationTest()
    {
        // Exactly one iteration
        AssertSuccess("while_tests/while_single_iteration_test", 1);
    }

    [Test]
    public void WhileNestedTest()
    {
        // 3 outer × 4 inner = 12 increments
        AssertSuccess("while_tests/while_nested_test", 12);
    }

    [Test]
    public void WhileWithIfTest()
    {
        // 0..9: 5 evens (0,2,4,6,8), 5 odds → 500 + 5 = 505
        AssertSuccess("while_tests/while_with_if_test", 505);
    }

    [Test]
    public void WhileCountdownTest()
    {
        // Count down from 10 to 0 — 10 steps
        AssertSuccess("while_tests/while_countdown_test", 10);
    }

    [Test]
    public void WhileMultiplyTest()
    {
        // 2^10 = 1024
        AssertSuccess("while_tests/while_multiply_test", 1024);
    }

    [Test]
    public void WhileWithEqTest()
    {
        // Find 13 in 0..19
        AssertSuccess("while_tests/while_with_eq_test", 1);
    }

    [Test]
    public void WhileWithNeTest()
    {
        // Count elements != 5 in 0..9 → 9
        AssertSuccess("while_tests/while_with_ne_test", 9);
    }

    [Test]
    public void WhileWithLogicalTest()
    {
        // Count elements where i > 10 && i < 20 in 0..99 → 11,12,...,19 = 9
        AssertSuccess("while_tests/while_with_logical_test", 9);
    }

    [Test]
    public void WhileFibonacciTest()
    {
        // fib(10) = 55
        AssertSuccess("while_tests/while_fibonacci_test", 55);
    }

    [Test]
    public void WhileConditionNotBoolFailTest()
    {
        // while 5 — condition must be bool
        AssertFail<GenericSemanticError>("while_tests/while_condition_not_bool_fail_test");
    }

    [Test]
    public void WhileBodyNotVoidFailTest()
    {
        // while body returns non-void — should fail
        AssertFail<GenericSemanticError>("while_tests/while_body_not_void_fail_test");
    }

    [Test]
    public void WhileFnCallInConditionTest()
    {
        // Condition is a function call returning bool — sum 0+1+2+3+4 = 10
        AssertSuccess("while_tests/while_fn_call_in_condition_test", 10);
    }

    [Test]
    public void WhileStructMutationTest()
    {
        // Mutate struct field in loop — count to 5
        AssertSuccess("while_tests/while_struct_mutation_test", 5);
    }
}
