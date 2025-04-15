using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public abstract class Type : IDefinition
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dataRefItem"></param>
    /// <returns>Stack pointer</returns>
    public virtual LLVMValueRef AssignToStack(CodeGenContext context, VariableRefItem dataRefItem)
    {
        
        
        // types that arent primitive, whether they are intended to be an lvalue or not, their 
        // value refs are pointers to stack allocated memory.
        // if its intended to be an rvalue simply use that pointer

        var allocated = context.Builder.BuildAlloca(TypeRef);
        AssignTo(context, dataRefItem, new VariableRefItem()
        {
            Type = this,
            ValueRef = allocated
        });
        
        return allocated;
    }
    
    public abstract void AssignTo(CodeGenContext codeGenContext, VariableRefItem value, VariableRefItem ptr);
    public abstract int GetPrimitivesCompositeCount(CodeGenContext context);

    /// <summary>
    /// Abstracts away loading a value, so it can be used for parameters and return types. done because if its
    /// a primitive, simply return its value. if its not, load it from its pointer (since non primitive
    /// value refs are pointers) then return it
    /// </summary>
    /// <param name="context"></param>
    /// <param name="variableRef"></param>
    /// <returns></returns>
    public  abstract LLVMValueRef LoadValueForRetOrArg(CodeGenContext context, VariableRefItem variableRef);

    

    public abstract LLVMTypeRef TypeRef { get;  protected set; }
    public bool HasBeenGened { get; set; } = false;
    public abstract LangPath Ident { get; }
    public abstract Token LookUpToken { get; }
    void IDefinition.Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }

    public Token Token { get; }

    public abstract void CodeGen(CodeGenContext context);


    public abstract int Priority { get; }

    public abstract void Analyze(SemanticAnalyzer analyzer);
}