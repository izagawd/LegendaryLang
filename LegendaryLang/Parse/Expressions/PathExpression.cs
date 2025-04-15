using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class PathExpression : IExpression
{
 
    public LangPath Path { get; }

    public PathExpression(LangPath path)
    {
     
        Path = path;
    }

    /// <summary>
    /// Generates LLVM IR to load the runtime value of the variable
    /// referenced by the path.
    /// </summary>
    public unsafe VariableRefItem DataRefCodeGen(CodeGenContext context)
    {

        if (BaseLangPath is null)
        {
      
            BaseLangPath = (context.GetRefItemFor(Path) as IHasType).Type.Ident;
        }
        string pathSuffix = Path.ToString();
        uint* major = null;
        uint* other = null;

        var refItem = context.GetRefItemFor(Path) as VariableRefItem;
        var gotten = refItem.ValueRef;
           
        // 3. Emit a load instruction to get the current value from the variable's pointer.
        return new VariableRefItem()
        {
            ValueRef = gotten,
            Type = refItem.Type
        };
    }

    public LangPath? BaseLangPath { get; set; }


    /// <summary>
    /// Retrieves the semantic type identified during analysis.
    /// This can be used later for type checking or further code generation.
    /// </summary>
    /// <param name="semanticAnalyzer"></param>
    public LangPath SetTypePath(SemanticAnalyzer semanticAnalyzer)
    {
        // For example, you could look up the type information for this variable
        // in a semantic symbol table maintained during the Analyze phase.
        // Here you might do something like:
        // return analyzer.LookupType(FirstIdent.Text, Path);
        throw new NotImplementedException();
    }

    /// <summary>
    /// During semantic analysis, you would resolve this symbol's definition.
    /// For example, binding the variable use to its declaration.
    /// </summary>
    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Resolve the variable (i.e. "bind" the identifier to its declaration).
        // This process may also check for errors like "undefined variable".
        // You can use a method in your SemanticAnalyzer to register or verify the symbol.
        throw new NotImplementedException();
    }

    public Token LookUpToken => Path.FirstIdentifierToken;
}