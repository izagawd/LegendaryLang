using NUnit.Framework;
using LegendaryLang;
using static Tests.CompilerTestHelper;

namespace Tests;

[TestFixture]
public class TupleTests
{
    [Test]
    public void EmptyTupleTest()
    {
        AssertSuccess("tuple_tests/empty_tuple_test", 5);
    }

    [Test]
    public void TwoElementTupleTest()
    {
        AssertSuccess("tuple_tests/two_element_tuple_test", 5);
    }

    [Test]
    public void SingleElementTupleTest()
    {
        AssertSuccess("tuple_tests/single_element_tuple_test", 5);
    }

    [Test]
    public void ImplicitUnitReturnTest()
    {
        // fn foo() with no return type — returning () explicitly should work
        AssertSuccess("tuple_tests/implicit_unit_return_test", 5);
    }

    [Test]
    public void GroupingParensNotTupleTest()
    {
        // (5) is just 5 — grouping parens, not a tuple
        AssertSuccess("tuple_tests/grouping_parens_not_tuple_test", 5);
    }

    [Test]
    public void SingleTupleNotI32FailTest()
    {
        // (5,) is a 1-tuple, not i32 — type mismatch
        AssertFail<TypeMismatchError>("tuple_tests/single_tuple_not_i32_fail_test");
    }
}
