using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class PrintTests
{
    [Test]
    public void PrintGcStrTest()
    {
        AssertSuccess("print_tests/print_gc_str_test", 0);
    }

    [Test]
    public void PrintQualifiedDispatchTest()
    {
        AssertSuccess("print_tests/print_qualified_dispatch_test", 0);
    }

    [Test]
    public void QualifiedTraitCallDispatchTest()
    {
        // self.deref() on &Wrapper(T) should dispatch to Wrapper(T): Deref (not &T: Deref).
        // T.ToString(self.deref()) then correctly passes &str to str.ToString.
        AssertSuccess("print_tests/qualified_trait_call_dispatch_test", 5);
    }
}
