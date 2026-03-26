using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using File = System.IO.File;

namespace LegendaryLang;

public class CompileResult
{
    public Func<int>? Function { get; init; }
    public List<CompileError> Errors { get; init; } = [];

    public bool Success => Function != null && Errors.Count == 0;

    /// <summary>
    /// Whether any error of the given type occurred
    /// </summary>
    public bool HasError<T>() where T : CompileError => Errors.Any(e => e is T);

    /// <summary>
    /// All errors of a given type
    /// </summary>
    public IEnumerable<T> GetErrors<T>() where T : CompileError => Errors.OfType<T>();
}

public class Compiler
{
    public const string extension = "rs";

    /// <summary>
    /// Old signature kept for backwards compat — returns null on failure
    /// </summary>
    public static Func<int>? Compile(string codeDirectory, bool showLLVMIR = false, bool optimized = false)
    {
        return CompileWithResult(codeDirectory, showLLVMIR, optimized).Function;
    }

    /// <summary>
    /// Maps a SemanticException to its typed CompileError counterpart
    /// </summary>
    private static CompileError MapSemanticException(SemanticException ex)
    {
        return ex switch
        {
            TraitBoundViolationException e => new TraitBoundViolationError
                { TypePath = e.TypePath, TraitPath = e.TraitPath },
            TraitNotFoundException e => new TraitNotFoundError
                { TraitPath = e.TraitPath },
            TraitMethodNotImplementedException e => new TraitMethodNotImplementedError
                { MethodName = e.MethodName, TraitPath = e.TraitPath },
            TraitExtraMethodException e => new TraitExtraMethodError
                { MethodName = e.MethodName, TraitPath = e.TraitPath },
            FunctionNotFoundException e => new FunctionNotFoundError
                { FunctionPath = e.FunctionPath },
            GenericParamCountException e => new GenericParamCountError
                { Expected = e.Expected, Found = e.Found },
            UndefinedVariableException e => new UndefinedVariableError
                { VariablePath = e.VariablePath },
            ReturnTypeMismatchException e => new ReturnTypeMismatchError
                { ExpectedType = e.ExpectedType, FoundType = e.FoundType },
            TypeMismatchException e => new TypeMismatchError
                { ExpectedType = e.ExpectedType, FoundType = e.FoundType, Context = e.Context },
            _ => new GenericSemanticError { Details = ex.Message }
        };
    }

    public static CompileResult CompileWithResult(string codeDirectory, bool showLLVMIR = false, bool optimized = false)
    {
        var errors = new List<CompileError>();
        var directoryPath = codeDirectory;

        const string extensionFinder = $"*.{extension}";
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
            errors.Add(new DirectoryNotFoundError { Directory = directoryPath });
            return new CompileResult { Errors = errors };
        }

        var mainFileDir = Path.Combine(codeDirectory, $"main.{extension}");
        if (!codeFiles.Any(i => Path.GetFullPath(i.Key) == Path.GetFullPath(mainFileDir)))
        {
            errors.Add(new MainFileNotFoundError { Directory = codeDirectory });
            return new CompileResult { Errors = errors };
        }

        var parseResults = codeFiles.Select(i =>
            {
                try
                {
                    return Parser.Parse(Lexer.Lex(i.Value, i.Key));
                }
                catch (Exception e)
                {
                    errors.Add(new ParseError { Details = e.Message });
                    return null;
                }
            })
            .Append(PrimitiveTypeGenerator.Generate())
            .ToList();

        if (errors.Any())
        {
            foreach (var e in errors) Console.WriteLine(e);
            return new CompileResult { Errors = errors };
        }

        var mainFile = parseResults.First(i => i.File != null && Path.GetFullPath(i.File.Path) == Path.GetFullPath(mainFileDir));

        var analysis = new SemanticAnalyzer(parseResults).Analyze();
        if (analysis.Any())
        {
            Console.WriteLine("SEMANTIC ERRORS FOUND\n");
            foreach (var a in analysis)
            {
                Console.WriteLine(a.Message + "\n");
                errors.Add(MapSemanticException(a));
            }
            return new CompileResult { Errors = errors };
        }

        var mainFn = mainFile.Items.OfType<FunctionDefinition>().FirstOrDefault(i => i.Name == "main");
        if (mainFn == null)
        {
            errors.Add(new MainFunctionMissingError { FilePath = mainFileDir });
            return new CompileResult { Errors = errors };
        }

        if (mainFn.ReturnTypePath != new I32TypeDefinition().TypePath)
        {
            errors.Add(new MainReturnTypeError
            {
                ExpectedType = new I32TypeDefinition().TypePath.ToString(),
                FoundType = mainFn.ReturnTypePath.ToString()
            });
            return new CompileResult { Errors = errors };
        }

        if (mainFn.Arguments.Length != 0)
        {
            errors.Add(new MainArgumentsError());
            return new CompileResult { Errors = errors };
        }

        var function = CodeGenContext.CodeGenMain(parseResults, new NormalLangPath(null, [codeDirectory, "main"]), showLLVMIR,
            optimized);

        if (function == null)
        {
            errors.Add(new CodeGenError());
            return new CompileResult { Errors = errors };
        }

        return new CompileResult { Function = function, Errors = errors };
    }
}