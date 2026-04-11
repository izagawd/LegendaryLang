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
        // T.ToString(self.deref()) where self.deref() returns &Wrapper(T) (not &T)
        // must produce valid LLVM IR — dispatches to Wrapper(T).ToString, not str.ToString.
        // Would stack overflow at runtime, so only check compilation.
        AssertSuccess("print_tests/qualified_trait_call_dispatch_test");
    }
}
