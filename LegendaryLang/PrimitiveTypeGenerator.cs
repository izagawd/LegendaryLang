using System.Reflection;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Types;

namespace LegendaryLang;

public class PrimitiveTypeGenerator
{
    public static ParseResult Generate()
    {
        return new ParseResult()
        {
            TopLevels = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsAssignableTo(typeof(PrimitiveTypeDefinition)) && !t.IsAbstract).Select(i=>(ITopLevel) Activator.CreateInstance(i)).ToList(),
            File = null
        };
    }
}