namespace LegendaryLang;

public class Program
{
    public static void Main(string[] args)
    {
        var function = Compiler.Compile("code", true, true);
        Console.WriteLine(function?.Invoke());
    }
}