using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class GenericTests
{
    [Test]
    public void GenericNestedTests()
    {
        var compiled =Compiler.Compile("compiler_tests/generic_tests/generic_nested_test",false,false);
        if (compiled() != 7)
        {
            throw new Exception();
        }
    }
    [Test]
    public void GenericReturnTests()
    {
        var compiled =Compiler.Compile("compiler_tests/generic_tests/generic_return_test",false,false);
        if (compiled() != 5)
        {
            throw new Exception();
        }
    }
}