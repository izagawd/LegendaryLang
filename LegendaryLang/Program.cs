namespace LegendaryLang;

public class Program
{
    public static void Main(string[] args)
    {
        var function = new Compiler().Compile("code", true);
        Console.WriteLine(function?.Invoke());
    }
}