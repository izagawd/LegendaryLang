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
            UseAfterMoveException e => new UseAfterMoveError
                { VariablePath = e.VariablePath },
            CannotInferGenericArgsException e => new CannotInferGenericArgsError
                { TypeOrFunctionName = e.TypeOrFunctionName },
            InferredTypeMismatchException e => new InferredTypeMismatchError
                { ExpectedType = e.ExpectedType, InferredType = e.InferredType },
            DuplicateDefinitionException e => new DuplicateDefinitionError
                { DefinitionPath = e.DefinitionPath },
            BorrowInvalidatedException e => new BorrowInvalidatedError
                { VariableName = e.VariableName },
            NonExhaustiveMatchException e => new NonExhaustiveMatchError
                { VariantName = e.VariantName },
            DerefNonReferenceException e => new DerefNonReferenceError
                { TypePath = e.TypePath },
            MoveOutOfReferenceException e => new MoveOutOfReferenceError
                { TypePath = e.TypePath },
            DanglingReferenceException => new DanglingReferenceError(),
            BorrowConflictException e => new BorrowConflictError
                { Source = e.Source, ExistingBorrower = e.ExistingBorrower,
                  NewKindName = RefTypeDefinition.GetRefName(e.NewKind),
                  ExistingKindName = RefTypeDefinition.GetRefName(e.ExistingKind) },
            UseWhileBorrowedException e => new UseWhileBorrowedError
                { Source = e.Source, Borrower = e.Borrower,
                  BorrowKindName = RefTypeDefinition.GetRefName(e.BorrowKind) },
            TraitImplBoundsMismatchException e => new TraitImplBoundsMismatchError
                { Details = e.Details },
            CopyDropConflictException e => new CopyDropConflictError
                { TypePath = e.TypePath },
            DropGenericsMismatchException e => new DropGenericsMismatchError
                { TypePath = e.TypePath, Details = e.Details },
            SupertraitNotImplementedException e => new SupertraitNotImplementedError
                { TypePath = e.TypePath, TraitPath = e.TraitPath, SupertraitPath = e.SupertraitPath },
            _ => new GenericSemanticError { Details = ex.Message }
        };
    }

    /// <summary>
    /// Shared parsing and semantic analysis used by both JIT and AOT paths.
    /// Returns (parseResults, mainModule) or null if fatal errors occurred.
    /// Errors are always populated via the out parameter.
    /// </summary>
    private static (List<ParseResult> parseResults, NormalLangPath mainModule)?
        ParseAndAnalyze(string codeDirectory, out List<CompileError> errors)
    {
        var errorList = new List<CompileError>();
        errors = errorList;

        const string extensionFinder = $"*.{extension}";
        Dictionary<string, string> codeFiles = new();
        if (Directory.Exists(codeDirectory))
        {
            var files = Directory.GetFiles(codeDirectory, extensionFinder, SearchOption.AllDirectories);

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
            errorList.Add(new DirectoryNotFoundError { Directory = codeDirectory });
            return null;
        }

        var mainFileDir = Path.Combine(codeDirectory, $"main.{extension}");
        if (!codeFiles.Any(i => Path.GetFullPath(i.Key) == Path.GetFullPath(mainFileDir)))
        {
            errorList.Add(new MainFileNotFoundError { Directory = codeDirectory });
            return null;
        }

        var parseResults = codeFiles.Select(i =>
            {
                try
                {
                    return Parser.Parse(Lexer.Lex(i.Value, i.Key));
                }
                catch (Exception e)
                {
                    errorList.Add(new ParseError { Details = e.Message });
                    return null;
                }
            })
            .Append(PrimitiveTypeGenerator.Generate())
            .Concat(StdLibrary.ParseAll())
            .ToList();

        if (errorList.Any())
        {
            foreach (var e in errorList) Console.WriteLine(e);
            return null;
        }

        var mainFile = parseResults.First(i => i.File != null && Path.GetFullPath(i.File.Path) == Path.GetFullPath(mainFileDir));

        // Compute crate root — the package directory as a module path
        var crateRoot = new NormalLangPath(null, codeDirectory.Split(new[] { '\\', '/' })
            .Select(s => (NormalLangPath.PathSegment)s));

        var analysis = new SemanticAnalyzer(parseResults, crateRoot).Analyze();
        if (analysis.Any())
        {
            Console.WriteLine("SEMANTIC ERRORS FOUND\n");
            foreach (var a in analysis)
            {
                Console.WriteLine(a.Message + "\n");
                errors.Add(MapSemanticException(a));
            }
            return null;
        }

        var mainFn = mainFile.Items.OfType<FunctionDefinition>().FirstOrDefault(i => i.Name == "main");
        if (mainFn == null)
        {
            errors.Add(new MainFunctionMissingError { FilePath = mainFileDir });
            return null;
        }

        if (mainFn.ReturnTypePath != new I32TypeDefinition().TypePath)
        {
            errors.Add(new MainReturnTypeError
            {
                ExpectedType = new I32TypeDefinition().TypePath.ToString(),
                FoundType = mainFn.ReturnTypePath.ToString()
            });
            return null;
        }

        if (mainFn.Arguments.Length != 0)
        {
            errors.Add(new MainArgumentsError());
            return null;
        }

        var mainModuleSegments = codeDirectory.Split(new[] { '\\', '/' })
            .Select(s => (NormalLangPath.PathSegment)s)
            .ToList();
        var mainModule = new NormalLangPath(null, mainModuleSegments);

        return (parseResults, mainModule);
    }

    public static CompileResult CompileWithResult(string codeDirectory, bool showLLVMIR = false, bool optimized = false)
    {
        var parsed = ParseAndAnalyze(codeDirectory, out var errors);
        if (parsed == null)
            return new CompileResult { Errors = errors };

        // Don't attempt codegen if semantic analysis found errors —
        // the AST may be in an inconsistent state (e.g., types used as values)
        if (errors.Count > 0)
            return new CompileResult { Errors = errors };

        var (parseResults, mainModule) = parsed.Value;

        var function = CodeGenContext.CodeGenMain(parseResults, mainModule, showLLVMIR, optimized);

        if (function == null)
        {
            errors.Add(new CodeGenError());
            return new CompileResult { Errors = errors };
        }

        return new CompileResult { Function = function, Errors = errors };
    }

    /// <summary>
    /// Compiles source code to an executable file.
    /// </summary>
    public static CompileResult CompileToExecutable(string codeDirectory, string outputPath,
        bool showLLVMIR = false, bool optimized = false)
    {
        var parsed = ParseAndAnalyze(codeDirectory, out var errors);
        if (parsed == null)
            return new CompileResult { Errors = errors };

        var (parseResults, mainModule) = parsed.Value;

        // Emit object file to a temp path
        var objectPath = Path.ChangeExtension(outputPath, ".o");

        var success = CodeGenContext.CodeGenToObjectFile(parseResults, mainModule, objectPath, showLLVMIR, optimized);
        if (!success)
        {
            errors.Add(new CodeGenError());
            return new CompileResult { Errors = errors };
        }

        // Link the object file into an executable
        var linkSuccess = LinkObjectFile(objectPath, outputPath);
        if (!linkSuccess)
        {
            errors.Add(new LinkerError { Details = $"Failed to link '{objectPath}' into '{outputPath}'. Ensure clang or gcc is on PATH." });
            return new CompileResult { Errors = errors };
        }

        // Clean up object file
        try { File.Delete(objectPath); } catch { }

        Console.WriteLine($"Executable created: {outputPath}");
        return new CompileResult { Errors = errors };
    }

    /// <summary>
    /// Invokes a system linker (clang or gcc) to link an object file into an executable.
    /// </summary>
    private static bool LinkObjectFile(string objectPath, string outputPath)
    {
        // Try linkers in order of preference
        var linkers = OperatingSystem.IsWindows()
            ? new[] { "clang", "gcc", "cc" }
            : new[] { "cc", "clang", "gcc" };

        foreach (var linker in linkers)
        {
            try
            {
                var args = $"\"{objectPath}\" -o \"{outputPath}\"";
                if (OperatingSystem.IsWindows() && !outputPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    args = $"\"{objectPath}\" -o \"{outputPath}.exe\"";

                Console.WriteLine($"Linking with {linker}: {linker} {args}");

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = linker,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Linked successfully with {linker}");
                    return true;
                }

                Console.WriteLine($"{linker} failed (exit {process.ExitCode}): {stderr}");
            }
            catch (Exception ex)
            {
                // Linker not found, try next
                Console.WriteLine($"{linker} not found: {ex.Message}");
            }
        }

        Console.WriteLine("No suitable linker found. Install clang or gcc and ensure it's on PATH.");
        return false;
    }
}