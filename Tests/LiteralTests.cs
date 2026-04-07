using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class NumericLiteralInferenceTests
{
    [Test]
    public void LiteralInferU8Test()
    {
        // let x: u8 = 200 — literal coerced to u8
        AssertSuccess("literal_tests/literal_infer_u8_test", 1);
    }

    [Test]
    public void LiteralInferUsizeTest()
    {
        // let x: usize = 1000 — literal coerced to usize
        AssertSuccess("literal_tests/literal_infer_usize_test", 1);
    }

    [Test]
    public void LiteralDefaultI32Test()
    {
        // let x = 42 — no annotation, stays i32
        AssertSuccess("literal_tests/literal_default_i32_test", 42);
    }

    [Test]
    public void LiteralInferU8ArithmeticTest()
    {
        // let x: u8 = 100 then TryCastPrimitive(i32, x) → 100
        AssertSuccess("literal_tests/literal_infer_u8_arithmetic_test", 100);
    }

    [Test]
    public void LiteralInferNoAnnotationTest()
    {
        // let x = 42; let y = 10; x + y → 52 (both default i32)
        AssertSuccess("literal_tests/literal_infer_no_annotation_test", 52);
    }
}
