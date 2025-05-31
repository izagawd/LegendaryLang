using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using File = System.IO.File;

namespace LegendaryLang;

public class Compiler
{
    public const string extension = "rs";

    public Func<int>? Compile(string codeDirectory, bool showLLVMIR = false, bool optimized = false)
    {
        var directoryPath = codeDirectory;

        const string extensionFinder = $"*.{extension}"; // Change this to your desired extension
        Dictionary<string, string> codeFiles = new();
        if (Directory.Exists(directoryPath))
        {
            var files = Directory.GetFiles(directoryPath, extensionFinder, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                Console.WriteLine($"Loading code file: {file}");
                var content = File.ReadAllText(file);
                codeFiles.Add(file, content);
            }

            Console.WriteLine("");
        }
        else
        {
            Console.WriteLine("Directory does not exist.");
            return null;
        }

        var mainFileDir = $"{codeDirectory}\\main.{extension}";
        if (!codeFiles.Any(i => i.Key == mainFileDir)) Console.WriteLine($"No main.{extension} file found!!!");

        List<string> parserExceptionsText = [];

        var parseResults = codeFiles.Select(i =>
            {
                try
                {
                    return new Parser(Lexer.Lex(i.Value, i.Key)).Parse();
                }
                catch (Exception e)
                {
                    parserExceptionsText.Add(e.Message);
                    return null;
                }
            })
            .Append(PrimitiveTypeGenerator.Generate())
            .ToList();
        if (parserExceptionsText.Any())
        {
            foreach (var i in parserExceptionsText) Console.WriteLine(i);
            return null;
        }

        var mainFile = parseResults.First(i => i.File!.Path == $"{codeDirectory}\\main.{extension}");

        var analysis = new SemanticAnalyzer(parseResults).Analyze();
        if (analysis.Any())
        {
            Console.WriteLine("SEMANTIC ERRORS FOUND\n");
            Console.WriteLine(string.Join("\n\n", analysis.Select(i => i.Message)));
            return null;
        }

        var mainFn = mainFile.TopLevels.OfType<FunctionDefinition>().FirstOrDefault(i => i.Name == "main");
        if (mainFn == null)
        {
            Console.WriteLine($"'fn main' function not found in {mainFileDir}!!!");
            return null;
        }

        if (mainFn.ReturnTypePath != new I32TypeDefinition().TypePath)
        {
            Console.WriteLine(
                $"'fn main' return type must be '{new I32TypeDefinition().TypePath}', not '{mainFn.ReturnTypePath}'!!!");
            return null;
        }

        if (mainFn.Arguments.Length != 0)
        {
            Console.WriteLine("'fn main' arguments are not empty!!!");
            return null;
        }

        return new CodeGenContext(parseResults, new NormalLangPath(null, [codeDirectory])).CodeGen(showLLVMIR,
            optimized);
    }
}