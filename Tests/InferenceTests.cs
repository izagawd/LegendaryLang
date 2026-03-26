using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class InferenceTests
{
    [Test]
    public void InferStructFromFieldTest()
    {
        // Wrapper { val = 42 } — infers T = i32 from the field
        var result = Compiler.CompileWithResult(
            "compiler_tests/inference_tests/infer_struct_from_field_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void InferStructFromAnnotationTest()
    {
        // let a : Wrapper<i32> = Wrapper { val = 5 } — infers from declared type
        var result = Compiler.CompileWithResult(
            "compiler_tests/inference_tests/infer_struct_from_annotation_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void InferStructMismatchTest()
    {
        // let a : Wrapper<bool> = Wrapper { val = 5 } — annotation says bool, field is i32
        var result = Compiler.CompileWithResult(
            "compiler_tests/inference_tests/infer_struct_mismatch_test", true, true);
        Assert.That(!result.Success);
    }

    [Test]
    public void InferFnFromArgTest()
    {
        // identity(42) — infers T = i32 from the argument
        var result = Compiler.CompileWithResult(
            "compiler_tests/inference_tests/infer_fn_from_arg_test", true, true);
        Assert.That(result.Success);
        Assert.That(42 == result.Function?.Invoke());
    }

    [Test]
    public void InferFnFromWrapperArgTest()
    {
        // do_something(Wrapper { kk = 5 }) — infers T = i32 from Wrapper<T> arg
        var result = Compiler.CompileWithResult(
            "compiler_tests/inference_tests/infer_fn_from_wrapper_arg_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void InferFnMultiParamTest()
    {
        // make_pair(10, 20) — infers A = i32, B = i32 from two args
        var result = Compiler.CompileWithResult(
            "compiler_tests/inference_tests/infer_fn_multi_param_test", true, true);
        Assert.That(result.Success);
        Assert.That(30 == result.Function?.Invoke());
    }

    [Test]
    public void InferFnNestedCallTest()
    {
        // identity(identity(5)) — inner infers T=i32, outer infers T=i32
        var result = Compiler.CompileWithResult(
            "compiler_tests/inference_tests/infer_fn_nested_call_test", true, true);
        Assert.That(result.Success);
        Assert.That(5 == result.Function?.Invoke());
    }

    [Test]
    public void InferStructPairFromFieldsTest()
    {
        // Pair { first = 10, second = 20 } — infers A = i32, B = i32
        var result = Compiler.CompileWithResult(
            "compiler_tests/inference_tests/infer_struct_pair_from_fields_test", true, true);
        Assert.That(result.Success);
        Assert.That(30 == result.Function?.Invoke());
    }

    [Test]
    public void InferTurbofishStillWorksTest()
    {
        // Mix of explicit turbofish and inferred — both should work
        var result = Compiler.CompileWithResult(
            "compiler_tests/inference_tests/infer_turbofish_still_works_test", true, true);
        Assert.That(result.Success);
        Assert.That(52 == result.Function?.Invoke()); // 42 + 10
    }

    [Test]
    public void InferFnAnnotationConflictTest()
    {
        // let a : bool = identity(5) — arg says i32, annotation says bool
        var result = Compiler.CompileWithResult(
            "compiler_tests/inference_tests/infer_fn_annotation_conflict_test", true, true);
        Assert.That(!result.Success);
    }
}
