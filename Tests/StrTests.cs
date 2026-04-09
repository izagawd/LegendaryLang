using NUnit.Framework;
using LegendaryLang;
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

    [Test]
    public void StrFatPtrSizeTest()
    {
        // &const str is a fat pointer: size == 2 * size of a thin pointer
        AssertSuccess("str_tests/str_fat_ptr_size_test", 1);
    }

    [Test]
    public void StrTraitMethodTest()
    {
        // Trait impl for str, method dispatch via auto-ref with fat pointer metadata.
        AssertSuccess("str_tests/str_trait_method_test", 42);
    }

    [Test]
    public void StrInherentMethodTest()
    {
        // Inherent impl on str, method called via auto-ref with fat pointer.
        AssertSuccess("str_tests/str_inherent_method_test", 5);
    }
}
