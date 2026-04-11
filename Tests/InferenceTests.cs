using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class InferenceTests
{
    [Test] public void InferStructFromFieldTest() => AssertSuccess("inference_tests/infer_struct_from_field_test", 42);

    [Test] public void InferStructFromAnnotationTest() => AssertSuccess("inference_tests/infer_struct_from_annotation_test", 5);

    [Test] public void InferStructMismatchTest() => AssertFail("inference_tests/infer_struct_mismatch_test");

    [Test] public void InferFnFromArgTest() => AssertSuccess("inference_tests/infer_fn_from_arg_test", 42);

    [Test] public void InferFnFromWrapperArgTest() => AssertSuccess("inference_tests/infer_fn_from_wrapper_arg_test", 5);

    [Test] public void InferFnMultiParamTest() => AssertSuccess("inference_tests/infer_fn_multi_param_test", 30);

    [Test] public void InferFnNestedCallTest() => AssertSuccess("inference_tests/infer_fn_nested_call_test", 5);

    [Test] public void InferStructPairFromFieldsTest() => AssertSuccess("inference_tests/infer_struct_pair_from_fields_test", 30);

    [Test]
    public void InferExplicitComptimeStillWorksTest()
    {
        // Mix of explicit comptime and inferred — both should work
        var result = Compile("inference_tests/infer_explicit_comptime_still_works_test");
        Assert.That(result.Success);
        Assert.That(52 == result.Function?.Invoke()); // 42 + 10
    }

    [Test] public void InferFnAnnotationConflictTest() => AssertFail("inference_tests/infer_fn_annotation_conflict_test");

    [Test]
    public void MatchDisambiguateTryIntoTest()
    {
        // match made.TryInto() { Option.Some(gotten) => gotten, _ => 0 }
        // Expected return i32 → scrutinee expected Option(i32) → disambiguates to TryInto(i32)
        AssertSuccess("inference_tests/match_disambiguate_tryinto_test", 5);
    }

    [Test]
    public void QualifiedTraitGenericReturnTest()
    {
        // (usize as TryInto(i32)).TryInto(made) — trait generic T=i32 must be
        // substituted in return type Option(T) → Option(i32)
        AssertSuccess("inference_tests/qualified_trait_generic_return_test", 42);
    }
}
