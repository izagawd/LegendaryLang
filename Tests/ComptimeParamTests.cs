using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class ComptimeParamTests
{
    [Test]
    public void ComptimeInterleavedTest() => AssertSuccess("comptime_param_tests/comptime_interleaved_test", 33);

    [Test]
    public void ComptimeDeducedRejectsTypeArgTest() => AssertFail<LegendaryLang.GenericSemanticError>("comptime_param_tests/comptime_deduced_rejects_type_arg_test");

    [Test]
    public void ComptimeDeducedInfersTest() => AssertSuccess("comptime_param_tests/comptime_deduced_infers_test", 42);

    [Test]
    public void ComptimeAllExplicitTest() => AssertSuccess("comptime_param_tests/comptime_all_explicit_test", 42);
}
