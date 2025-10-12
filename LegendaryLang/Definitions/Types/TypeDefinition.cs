using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions.Types;

/// <summary>
///     A type definition, meaning its not monomorphized yet. so it cant be used in code
/// </summary>
public abstract class TypeDefinition : IItem,  IMonomorphizable, IAnalyzable,  IPathResolvable
{
    public virtual LangPath FullPath => Module.Append(Name);
    public abstract LangPath TypePath { get; }

    /// <summary>
    ///     Abstracts away loading a value, so it can be used for parameters and return types. done because if its
    ///     a primitive, simply return its value. if its not, load it from its pointer (since non primitive
    ///     value refs are pointers) then return it
    /// </summary>
    /// <param name="context"></param>
    /// <param name="variableRef"></param>
    /// <returns></returns>

    public abstract string Name { get; }

    public abstract NormalLangPath Module { get; }
    public bool HasBeenGened { get; set; } = false;
    public abstract IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments);


    public abstract ImmutableArray<LangPath>? GetGenericArguments(LangPath path);

    public virtual void ResolvePaths(PathResolver resolver)
    {
        
    }

    public IEnumerable<ISyntaxNode> Children => [];


    public abstract Token Token { get; }


    public abstract void Analyze(SemanticAnalyzer analyzer);

    /// <summary>
    ///     Monomprhize and codegen for a specific generic variant
    /// </summary>
    /// <param name="context"></param>
    /// <param name="langPath"></param>
    /// <returns></returns>
    public void ImplementMonomorphized(CodeGenContext context, Type typeDefinition)
    {
        typeDefinition.CodeGen(context);

    }

}