using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class PathExpression : IExpression
{
 
    public LangPath Path { get; set; }

    public PathExpression(LangPath path)
    {
     
        Path = path;
    }

    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return [];
    }

    /// <summary>
    /// Generates LLVM IR to load the runtime value of the variable
    /// referenced by the path.
    /// </summary>
    public unsafe VariableRefItem DataRefCodeGen(CodeGenContext context)
    {

        var gottenRefItem = context.GetRefItemFor(Path);
        if (gottenRefItem is TypeRefItem typeRefItem)
        {
            Path = typeRefItem.Type.TypePath;
        } else if (gottenRefItem is FunctionRefItem variableRefItem)
        {
            Path = variableRefItem.Function.FullPath;
        }
        if (TypePath is null)
        {
      
            TypePath = (context.GetRefItemFor(Path) as IHasType).Type.TypePath;
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

    /// <summary>
    /// Should be set during semantic analysis
    /// </summary>
    public LangPath? TypePath { get; set; }


    /// <summary>
    /// During semantic analysis, you would resolve this symbol's definition.
    /// For example, binding the variable use to its declaration.
    /// </summary>
    public void Analyze(SemanticAnalyzer analyzer)
    {
        Path.LoadAsShortCutIfPossible(analyzer);
        // Resolve the variable (i.e. "bind" the identifier to its declaration).
        // This process may also check for errors like "undefined variable".
        // You can use a method in your SemanticAnalyzer to register or verify the symbol.
        throw new NotImplementedException();
    }

    public Token LookUpToken => Path.FirstIdentifierToken;
}