using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public class PathExpression : IExpression
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
    public ValueRefItem CodeGen(CodeGenContext context)
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

    public bool HasGuaranteedExplicitReturn => false;
    /// <summary>
    ///     During semantic analysis, you would resolve this symbol's definition.
    ///     For example, binding the variable use to its declaration.
    /// </summary>
    public void Analyze(SemanticAnalyzer analyzer)
    {
        TypePath = analyzer.GetVariableTypePath(Path);
        if (TypePath is null)
        {
            analyzer.AddException(new UndefinedVariableException(
                Path, Token.GetLocationStringRepresentation()));
            return;
        }

        // Check if this variable has been moved
        if (!analyzer.SuppressMoveChecks && Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            var varName = nlp.PathSegments[0].ToString();
            if (analyzer.IsMoved(varName))
            {
                analyzer.AddException(new UseAfterMoveException(
                    Path, Token.GetLocationStringRepresentation()));
            }
        }
    }

    public Token Token => Path.FirstIdentifierToken;

    public void ResolvePaths(PathResolver resolver)
    {
        Path = Path.Resolve(resolver);

    }
}