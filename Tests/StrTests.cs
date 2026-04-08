using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

[TestFixture]
public class StrTests
{
    [Test]
    public void StrLiteralCompilesTest()
    {
        // Basic &const str from a string literal
        AssertSuccess("str_tests/str_literal_compiles_test", 5);
    }
}
