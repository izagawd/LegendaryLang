using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public abstract class TypeDefinition : ITopLevel, IDefinition, IMonomorphizable
{
    /// <summary>
    /// Monomprhize and codegen for a specific generic variant
    /// </summary>
    /// <param name="context"></param>
    /// <param name="langPath"></param>
    /// <returns></returns>
    public abstract ConcreteDefinition. Type? Monomorphize(CodeGenContext context, LangPath langPath);

    IConcreteDefinition? IMonomorphizable.Monomorphize(CodeGenContext context, LangPath langPath)
    {
        return Monomorphize(context, langPath);
    }
    public abstract ImmutableArray<LangPath>? GetGenericArguments(LangPath langPath);

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
    public abstract Token LookUpToken { get; }


    public Token Token { get; }






    public abstract void Analyze(SemanticAnalyzer analyzer);
}