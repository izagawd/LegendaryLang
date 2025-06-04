using LegendaryLang;
using LegendaryLang.Parse;
using NUnit.Framework;

namespace Tests;

public class IfTests
{
    [Test]
    public void IfImplicitReturnTest()
    {
        var compiled =new Compiler().Compile("compiler_tests/if_tests/if_implicit_return",false,false);
        if (compiled() != 9)
        {
            throw new Exception();
        }
    }
}