using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions.Types;

/// <summary>
/// A type definition, meaning its not monomorphized yet. so it cant be used in code
/// </summary>
public abstract class TypeDefinition : ITopLevel, IDefinition, IMonomorphizable, IPathHaver
{
    /// <summary>
    /// Monomprhize and codegen for a specific generic variant
    /// </summary>
    /// <param name="context"></param>
    /// <param name="langPath"></param>
    /// <returns></returns>
    public  Type? Monomorphize(CodeGenContext context, LangPath langPath)
    {
        if (GetGenericArguments(langPath) is not null)
        {

   
            var str = GenerateIncompleteMono(context, langPath);
   
            return str;
        }
        return null;
    }
    public abstract Type GenerateIncompleteMono(CodeGenContext context, LangPath langPath);
    IConcreteDefinition? IMonomorphizable.Monomorphize(CodeGenContext context, LangPath langPath)
    {
        return Monomorphize(context, langPath);
    }
    public abstract ImmutableArray<LangPath>? GetGenericArguments(LangPath path);

    /// <summary>
    /// Abstracts away loading a value, so it can be used for parameters and return types. done because if its
    /// a primitive, simply return its value. if its not, load it from its pointer (since non primitive
    /// value refs are pointers) then return it
    /// </summary>
    /// <param name="context"></param>
    /// <param name="variableRef"></param>
    /// <returns></returns>

    public abstract string Name { get; }

    public abstract NormalLangPath Module { get; }
    public bool HasBeenGened { get; set; } = false;
    public abstract LangPath TypePath { get; }
    public IEnumerable<ISyntaxNode> Children => [];

    public virtual void SetFullPathOfShortCutsDirectly(SemanticAnalyzer analyzer)
    {
        
    }



    public abstract Token Token { get; }


  






    public abstract void Analyze(SemanticAnalyzer analyzer);
}