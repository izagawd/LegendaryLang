using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class InherentImplTests
{
    [Test] public void InherentBasicTest() => AssertSuccess("inherent_impl_tests/inherent_basic_test", 42);

    [Test] public void InherentMutTest() => AssertSuccess("inherent_impl_tests/inherent_mut_test", 11);

    [Test] public void InherentReturnSelfTest() => AssertSuccess("inherent_impl_tests/inherent_return_self_test", 10);

    [Test] public void InherentGenericTest() => AssertSuccess("inherent_impl_tests/inherent_generic_test", 99);

    [Test]
    public void InherentPlusTraitTest()
    {
        // Both inherent and trait impl on same type — both methods callable
        var result = Compile("inherent_impl_tests/inherent_plus_trait_test");
        Assert.That(result.Success);
        Assert.That(15 == result.Function?.Invoke()); // double(5)=10 + greet(5)=5
    }

    [Test] public void InherentQualifiedCallTest() => AssertSuccess("inherent_impl_tests/inherent_qualified_call_test", 77);

    [Test] public void InherentMultipleMethodsTest() => AssertSuccess("inherent_impl_tests/inherent_multiple_methods_test", 18);

    [Test] public void InherentWithDropTest() => AssertSuccess("inherent_impl_tests/inherent_with_drop_test", 100);

    [Test] public void InherentReturnGenericSelfTest() => AssertSuccess("inherent_impl_tests/inherent_return_generic_self_test", 500);

    // ═══════════════════════════════════════════════════════════════
    //  RECEIVER DISPATCH — self param naming determines instance vs static
    // ═══════════════════════════════════════════════════════════════

    [Test] public void ReceiverSelfSharedTest()
        => AssertSuccess("receiver_tests/receiver_self_shared", 42);

    [Test] public void ReceiverSelfUniqTest()
        => AssertSuccess("receiver_tests/receiver_self_uniq", 42);

    [Test] public void ReceiverSelfMutTest()
        => AssertSuccess("receiver_tests/receiver_self_mut", 42);

    [Test] public void ReceiverSelfConsumeTest()
        => AssertSuccess("receiver_tests/receiver_self_consume", 42);

    [Test] public void ReceiverNotSelfInstanceFailTest()
        => AssertFail("receiver_tests/receiver_not_self_instance_fail");

    [Test] public void ReceiverNotSelfStaticOkTest()
        => AssertSuccess("receiver_tests/receiver_not_self_static_ok", 42);
}
