using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public class PathExpression : IExpression, IPathHaver
{
    public PathExpression(LangPath path)
    {
        Path = path;
    }

    public LangPath Path { get; set; }
    public IEnumerable<ISyntaxNode> Children => [];


    /// <summary>
    ///     Generates LLVM IR to load the runtime value of the variable
    ///     referenced by the path.
    /// </summary>
    public ValueRefItem DataRefCodeGen(CodeGenContext context)
    {
        if (TypePath is null) TypePath = (context.GetRefItemFor(Path) as IHasType).Type.TypePath;


        var refItem = context.GetRefItemFor(Path) as ValueRefItem;
        var gotten = refItem.ValueRef;

        // 3. Emit a load instruction to get the current value from the variable's pointer.
        return new ValueRefItem
        {
            ValueRef = gotten,
            Type = refItem.Type
        };
    }

    /// <summary>
    ///     Should be set during semantic analysis
    /// </summary>
    public LangPath? TypePath { get; set; }


    /// <summary>
    ///     During semantic analysis, you would resolve this symbol's definition.
    ///     For example, binding the variable use to its declaration.
    /// </summary>
    public void Analyze(SemanticAnalyzer analyzer)
    {
        TypePath = analyzer.GetVariableTypePath(Path);
        if (TypePath is null)
            analyzer.AddException(new SemanticException(
                $"Path to variable '{Path}' not found, or the path is not a variable\n{Token.GetLocationStringRepresentation()}"));
    }

    public Token Token => Path.FirstIdentifierToken;

    public void SetFullPathOfShortCutsDirectly(SemanticAnalyzer analyzer)
    {
        Path = Path.GetFromShortCutIfPossible(analyzer);
    }
}