using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class AssignVariableExpression : IExpression
{
    public bool HasGuaranteedExplicitReturn => EqualsTo.HasGuaranteedExplicitReturn;

    public AssignVariableExpression(IExpression assigner, IExpression equalsTo, EqualityToken equalityToken)
    {
        Assigner = assigner;

        EqualsTo = equalsTo;
        EqualityToken = equalityToken;
    }


    public IExpression EqualsTo { get; set; }
    public EqualityToken EqualityToken { get; }
    public IExpression Assigner { get; }
    public IEnumerable<ISyntaxNode> Children => [EqualsTo, Assigner];



    public Token Token => EqualityToken;


    public void Analyze(SemanticAnalyzer analyzer)
    {
        TypePath = LangPath.VoidBaseLangPath;

        // Suppress move checks for the LHS — assignment to a moved variable restores it
        var prevSuppress = analyzer.SuppressMoveChecks;
        analyzer.SuppressMoveChecks = true;
        Assigner.Analyze(analyzer);
        analyzer.SuppressMoveChecks = prevSuppress;

        EqualsTo.Analyze(analyzer);
        if (EqualsTo.TypePath != Assigner.TypePath)
            analyzer.AddException(new SemanticException(
                $"Cannot assign variable of type '{Assigner.TypePath}' to an expression of type '{EqualsTo.TypePath}'\n{Token.GetLocationStringRepresentation()}"));

        // Mark the RHS variable as moved if not Copy
        analyzer.TryMarkExpressionAsMoved(EqualsTo);

        // Reassignment restores usability of the LHS variable
        if (Assigner is ChainExpression { SimpleVariableName: string varName })
            analyzer.UnmarkMoved(varName);
        else if (Assigner is PathExpression pe && pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
            analyzer.UnmarkMoved(nlp.PathSegments[0].ToString());
    }


    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var valueToEq = EqualsTo.CodeGen(codeGenContext);

        var variableRef = Assigner.CodeGen(codeGenContext);
        if (variableRef.ValueRef.TypeOf.Kind != LLVMTypeKind.LLVMPointerTypeKind)
            throw new Exception("Assigner should not be RValue");


        valueToEq.Type.AssignTo(codeGenContext, valueToEq, variableRef);
        return codeGenContext.GetVoid();
    }

    public LangPath? TypePath { get; private set; }

    public static AssignVariableExpression Parse(Parser parser, IExpression assignerExpression)
    {
        var equality = Equality.Parse(parser);
        var expression = IExpression.Parse(parser);
        return new AssignVariableExpression(assignerExpression, expression, equality);
    }
}