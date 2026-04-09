using LegendaryLang;
using NUnit.Framework;

namespace Tests;

/// <summary>
/// Shared test helpers that eliminate boilerplate in compiler test methods.
/// Most tests follow one of three patterns:
///   1. Compile, assert success, check return value
///   2. Compile, assert failure, check error type
///   3. Compile, assert success (no return value check)
/// </summary>
public static class CompilerTestHelper
{
    /// <summary>
    /// Compiles a test case from the compiler_tests directory.
    /// </summary>
    public static CompileResult Compile(string testPath)
        => Compiler.CompileWithResult($"compiler_tests/{testPath}", true, true);

    /// <summary>
    /// Asserts that compilation succeeds and the main function returns the expected value.
    /// </summary>
    public static void AssertSuccess(string testPath, int expected)
    {
        var result = Compile(testPath);
        Assert.That(result.Success, $"Expected compilation to succeed for '{testPath}' but got errors: {string.Join(", ", result.Errors)}");
        Assert.That(expected == result.Function?.Invoke(),
            $"Expected {expected} but got {result.Function?.Invoke()} for '{testPath}'");
    }

    /// <summary>
    /// Asserts that compilation succeeds (without checking the return value).
    /// </summary>
    public static void AssertSuccess(string testPath)
    {
        var result = Compile(testPath);
        Assert.That(result.Success, $"Expected compilation to succeed for '{testPath}' but got errors: {string.Join(", ", result.Errors)}");
    }

    /// <summary>
    /// Asserts that compilation fails with at least one error of the given type.
    /// </summary>
    public static CompileResult AssertFail<TError>(string testPath) where TError : CompileError
    {
        var result = Compile(testPath);
        Assert.That(!result.Success, $"Expected compilation to fail for '{testPath}'");
        Assert.That(result.HasError<TError>(),
            $"Expected error type {typeof(TError).Name} for '{testPath}' but got: {string.Join(", ", result.Errors.Select(e => e.GetType().Name))}");
        return result;
    }

    /// <summary>
    /// Asserts that compilation fails (without checking a specific error type).
    /// </summary>
    public static void AssertFail(string testPath)
    {
        var result = Compile(testPath);
        Assert.That(!result.Success, $"Expected compilation to fail for '{testPath}'");
    }
}
