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
        Path = Path.GetFromShortCutIfPossible(analyzer);
        TypePath= analyzer.GetVariableTypePath(Path);
        if (TypePath is null)
        {
            analyzer.AddException(new SemanticException($"Path to variable '{Path}' not found, or the path is not a variable\n{Token.GetLocationStringRepresentation()}"));
        }
    }

    public Token Token => Path.FirstIdentifierToken;
}