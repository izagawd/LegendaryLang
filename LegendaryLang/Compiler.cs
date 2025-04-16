using LegendaryLang.Lex;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Statements;
using LegendaryLang.Parse.Types;
using LegendaryLang.Semantics;
using File = System.IO.File;

namespace LegendaryLang;

public class Compiler
{
    public Compiler(){}

    public void Compile(string codeDirectory)
    {
        string directoryPath = codeDirectory;
        const string ext = "rs";
        const string extensionFinder = $"*.{ext}"; // Change this to your desired extension
        Dictionary<string, string> codeFiles = new();
        if (Directory.Exists(directoryPath))
        {
            string[] files = Directory.GetFiles(directoryPath, extensionFinder);

            foreach (string file in files)
            {
                Console.WriteLine($"Loading file: {file}");
                string content = File.ReadAllText(file);
                codeFiles.Add(file, content);
            }
        }
        else
        {
            Console.WriteLine("Directory does not exist.");
            return;
        }

        var mainFileDir = $"{codeDirectory}\\main.{ext}";
        if (!codeFiles.Any(i => i.Key == mainFileDir))
        {
            Console.WriteLine($"No main.{ext} file found!!!");
        }
        
       var parseResults = codeFiles.Select(
            i => new Parser(Lexer.Lex(i.Value, i.Key)).Parse()
        )
           .Append(PrimitiveTypeGenerator.Generate())
           .ToList();
        var mainFile = parseResults.First(i => i.File.Path == $"{codeDirectory}\\main.{ext}");

        new SemanticAnalyzer(parseResults).Analyze();
        var mainFn = mainFile.Definitions.OfType<Function>().FirstOrDefault(i => i.Name == "main");
        if (mainFn == null)
        {
            Console.WriteLine($"'fn main' function not found in {mainFileDir}!!!");
            return;
        }

        if (mainFn.ReturnType != new I32().TypePath)
        {
            Console.WriteLine($"'fn main' return type must be '{new I32().TypePath}'!!!");
            return;
        }

        if (mainFn.Arguments.Length != 0)
        {
            Console.WriteLine($"'fn main' arguments are not empty!!!");
            return;
        }
        new CodeGenContext(parseResults).CodeGen();
    }
}