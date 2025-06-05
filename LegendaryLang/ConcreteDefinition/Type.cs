using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public abstract class Type : IConcreteDefinition,  IPathResolvable
{
    public Type(TypeDefinition definition)
    {
        TypeDefinition = definition;
    }

    public abstract LLVMTypeRef TypeRef { get; protected set; }
    public TypeDefinition TypeDefinition { get; }
    public abstract LangPath TypePath { get; }
    public IEnumerable<ISyntaxNode> Children => [];


    public Token Token => TypeDefinition.Token;

    public abstract string Name { get; }
    public NormalLangPath Module => TypeDefinition.Module;
    public bool HasBeenGened { get; set; }
    public IDefinition? Definition => TypeDefinition;


    public abstract void CodeGen(CodeGenContext context);

    public unsafe LLVMValueRef CreateUninitialized(CodeGenContext context)
    {
        return LLVM.GetUndef(TypeRef);
    }

    public ValueRefItem CreateUninitializedValRef(CodeGenContext context)
    {
        return new ValueRefItem
        {
            Type = this,
            ValueRef = CreateUninitialized(context)
        };
    }

    /// <summary>
    /// </summary>
    /// <param name="dataRefItem"></param>
    /// <returns>Stack pointer</returns>
    public virtual LLVMValueRef AssignToStack(CodeGenContext context, ValueRefItem dataRefItem)
    {
        // types that arent primitive, whether they are intended to be an lvalue or not, their 
        // value refs are pointers to stack allocated memory.
        // if its intended to be an rvalue simply use that pointer

        var allocated = context.Builder.BuildAlloca(TypeRef);
        AssignTo(context, dataRefItem, new ValueRefItem
        {
            Type = this,
            ValueRef = allocated
        });

        return allocated;
    }


    public abstract int GetPrimitivesCompositeCount(CodeGenContext context);
    public abstract LLVMValueRef LoadValue(CodeGenContext context, ValueRefItem valueRef);

    public abstract void AssignTo(CodeGenContext codeGenContext, ValueRefItem value, ValueRefItem ptr);

    public void ResolvePaths(PathResolver resolver)
    {
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
    }
}