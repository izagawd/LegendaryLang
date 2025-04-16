using System.Reflection;
using LegendaryLang.Lex;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Statements;
using LegendaryLang.Semantics;
using File = System.IO.File;

namespace LegendaryLang;

public class Program
{
    public static void Main(string[] args)
    {
        var lexed = Lexer.Lex(File.ReadAllText("code.rs"), "bitch");
        
        
        var parsed = new Parser(lexed).Parse();
        var gen = PrimitiveTypeGenerator.Generate();
       new SemanticAnalyzer([parsed, gen]).Analyze();
        var count = parsed.Definitions.OfType<Function>().Select(i => i.BlockExpression).SelectMany(i => i.SyntaxNodes)
            .Where(i => i is LetStatement).Count();
        new CodeGenContext([parsed, gen]).CodeGen();
    }
    
}