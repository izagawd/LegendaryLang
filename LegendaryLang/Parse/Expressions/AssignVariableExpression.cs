using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class AssignVariableExpression : IExpression
{
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
        Assigner.Analyze(analyzer);
        EqualsTo.Analyze(analyzer);
        if (EqualsTo.TypePath != Assigner.TypePath)
            analyzer.AddException(new SemanticException(
                $"Cannot assign variable of type '{Assigner.TypePath}' to an expression of type '{EqualsTo.TypePath}'\n{Token.GetLocationStringRepresentation()}"));
    }


    public ValueRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        var valueToEq = EqualsTo.DataRefCodeGen(codeGenContext);

        var variableRef = Assigner.DataRefCodeGen(codeGenContext);
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