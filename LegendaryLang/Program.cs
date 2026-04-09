namespace LegendaryLang;

public class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var sourceDir = args[0];
        var outputPath = (string?)null;
        var showIR = false;
        var optimize = false;
        var jitMode = false;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-o" or "--output":
                    if (i + 1 < args.Length) outputPath = args[++i];
                    break;
                case "--ir":
                    showIR = true;
                    break;
                case "--optimize" or "-O":
                    optimize = true;
                    break;
                case "--jit":
                    jitMode = true;
                    break;
                case "--help" or "-h":
                    PrintUsage();
                    return 0;
            }
        }

        if (jitMode)
        {
            // JIT mode: compile and run immediately (old behavior)
            var result = Compiler.CompileWithResult(sourceDir, showIR, optimize);
            if (!result.Success)
            {
                foreach (var err in result.Errors)
                    Console.Error.WriteLine(err.Message);
                return 1;
            }
            var exitCode = result.Function!.Invoke();
            Console.WriteLine($"Program exited with code: {exitCode}");
            return exitCode;
        }
        else
        {
            // AOT mode: compile to executable (default)
            outputPath ??= OperatingSystem.IsWindows() ? "output.exe" : "output";

            var result = Compiler.CompileToExecutable(sourceDir, outputPath, showIR, optimize);
            if (!result.Success)
            {
                foreach (var err in result.Errors)
                    Console.Error.WriteLine(err.Message);
                return 1;
            }

            return 0;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("LegendaryLang Compiler");
        Console.WriteLine();
        Console.WriteLine("Usage: legendary <source_dir> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o, --output <path>   Output executable path (default: output / output.exe)");
        Console.WriteLine("  --ir                  Print LLVM IR to stdout");
        Console.WriteLine("  -O, --optimize        Enable LLVM optimizations");
        Console.WriteLine("  --jit                 JIT compile and run immediately (no executable)");
        Console.WriteLine("  -h, --help            Show this help");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  legendary code                    # Compile code/main.rs → output");
        Console.WriteLine("  legendary code -o myapp           # Compile code/main.rs → myapp");
        Console.WriteLine("  legendary code --jit              # JIT compile and run");
        Console.WriteLine("  legendary code --ir -O            # Show optimized LLVM IR");
    }
}