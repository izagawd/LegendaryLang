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

        new Compiler().Compile("code",true);
    }
    
}