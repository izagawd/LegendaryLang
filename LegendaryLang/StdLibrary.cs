using LegendaryLang.Lex;
using LegendaryLang.Parse;
using File = System.IO.File;

namespace LegendaryLang;

/// <summary>
/// Standard library loader. Reads .rs files from the std/ directory
/// next to the compiler executable.
/// </summary>
public static class StdLibrary
{
    private static string GetStdDirectory()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "std");
    }

    /// <summary>
    /// Parses all .rs files under std/ and returns them as ParseResults.
    /// </summary>
    public static IEnumerable<ParseResult> ParseAll()
    {
        var stdDir = GetStdDirectory();
        if (!Directory.Exists(stdDir))
            yield break;

        foreach (var file in Directory.GetFiles(stdDir, $"*.{Compiler.extension}", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            // Use a relative path so module paths resolve correctly (e.g., std::copy)
            var relativePath = Path.GetRelativePath(AppDomain.CurrentDomain.BaseDirectory, file);
            yield return Parser.Parse(Lexer.Lex(content, relativePath));
        }
    }
}
