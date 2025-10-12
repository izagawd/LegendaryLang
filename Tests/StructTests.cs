using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class StructTests
{
    [Test]
    public void IfImplicitReturnTest()
    {
        var compiled =Compiler.Compile("compiler_tests/struct_tests/nested_struct_test",false,false);
        if (compiled() != 15)
        {
            throw new Exception();
        }
    }
}