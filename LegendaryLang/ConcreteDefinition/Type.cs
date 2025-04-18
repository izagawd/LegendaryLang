using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public abstract class Type : IConcreteDefinition
{
    
    public Type(TypeDefinition definition)
    {
        TypeDefinition = definition;
    }
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
    

    public abstract int GetPrimitivesCompositeCount(CodeGenContext context);
    public  abstract LLVMValueRef LoadValue(CodeGenContext context, VariableRefItem variableRef);

    public abstract void AssignTo(CodeGenContext codeGenContext, VariableRefItem value, VariableRefItem ptr);

    public abstract LLVMTypeRef TypeRef { get;  protected set; }
    public  TypeDefinition TypeDefinition { get;  }
    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return [];
    }

    public Token Token => TypeDefinition.Token;
    public void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }
    public abstract LangPath TypePath { get; }

    public abstract string Name { get; }
    public  NormalLangPath Module => TypeDefinition.Module;
    public bool HasBeenGened { get; set; }
    public IDefinition? Definition => TypeDefinition;


    public abstract void CodeGen(CodeGenContext context);

}