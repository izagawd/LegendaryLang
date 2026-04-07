using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions.Types;

/// <summary>
///     A type definition, meaning its not monomorphized yet. so it cant be used in code
/// </summary>
public abstract class TypeDefinition : IItem,  IMonomorphizable, IAnalyzable,  IPathResolvable
{


    public virtual LangPath TypePath =>Module.Append(Name);

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

    /// <summary>
    /// For types, no need to just define it. fully codegen it! If its not fully codegenned it will lead to issues
    /// </summary>
    public abstract IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments);


    public abstract ImmutableArray<LangPath>? GetGenericArguments(LangPath path);

    public virtual void ResolvePaths(PathResolver resolver)
    {
        
    }

    public IEnumerable<ISyntaxNode> Children => [];


    public abstract Token Token { get; }


    public abstract void Analyze(SemanticAnalyzer analyzer);

    /// <summary>
    /// Returns the LLVM type of the pointer metadata for this type, or null if the type is sized (thin pointer).
    /// Unsized types override this: str and [T] return usize, trait objects return ptr (vtable).
    /// </summary>
    public virtual LLVMTypeRef? GetMetadataLLVMType() => null;



}