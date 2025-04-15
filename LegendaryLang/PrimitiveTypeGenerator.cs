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
            Definitions = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsAssignableTo(typeof(PrimitiveType)) && !t.IsAbstract).Select(i=>(IDefinition) Activator.CreateInstance(i)).ToList(),
            File = null
        };
    }
}