using LegendaryLang.Lex;
using LegendaryLang.Parse;

namespace LegendaryLang;

/// <summary>
/// Standard library definitions embedded as source code.
/// Parsed alongside user code — no compiler magic.
/// </summary>
public static class StdLibrary
{
    /// <summary>
    /// The Copy marker trait and its implementations for primitives.
    /// Types that implement Copy are bitwise-copied on assignment
    /// instead of being moved.
    /// </summary>
    private static readonly string CopySource =
        "trait Copy {}\n" +
        "impl Copy for i32 {}\n" +
        "impl Copy for bool {}\n";

    public static ParseResult ParseCopy()
    {
        return Parser.Parse(Lexer.Lex(CopySource, Path.Combine("std", "copy")));
    }

    /// <summary>
    /// Returns all standard library parse results.
    /// Add new modules here as the std grows.
    /// </summary>
    public static IEnumerable<ParseResult> ParseAll()
    {
        yield return ParseCopy();
    }
}
