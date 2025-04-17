using System.Linq.Expressions;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;


public class ReassignUnmatchedTypeException : SemanticException
{
    public AssignVariableExpression Expression { get; }
    public SemanticAnalyzer Analyzer { get; }

    public ReassignUnmatchedTypeException(AssignVariableExpression expression, SemanticAnalyzer analyzer)
    {
        Expression = expression;
        Analyzer = analyzer;
    }

    public override string Message => $"Cannot assign variable of type {Expression.Assigner.TypePath} to an expression of type {Expression.EqualsTo.TypePath}";
}
public class AssignVariableExpression : IExpression
{
    public static AssignVariableExpression Parse(Parser parser, IExpression assignerExpression)
    {
        Equality.Parse(parser);
        var expression = IExpression.Parse(parser);
        return new AssignVariableExpression(assignerExpression,  expression);
    }
    public Token LookUpToken => Assigner.LookUpToken;
    
    
    public IExpression EqualsTo { get; set; }
    public IExpression Assigner { get; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        
        TypePath = LangPath.VoidBaseLangPath;
        Assigner.Analyze(analyzer);
        EqualsTo.Analyze(analyzer);
        if (EqualsTo != Assigner)
        {
            throw new ReassignUnmatchedTypeException(this, analyzer);
        }
    }

    public AssignVariableExpression(IExpression assigner,  IExpression equalsTo)
    {

        Assigner = assigner;

        EqualsTo = equalsTo;
        
    }

    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
       return  EqualsTo.GetAllFunctionsUsed();
    }

    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        var valueToEq = EqualsTo.DataRefCodeGen(codeGenContext);
        
        var variableRef = Assigner.DataRefCodeGen(codeGenContext);
        if (variableRef.ValueRef.TypeOf.Kind != LLVMTypeKind.LLVMPointerTypeKind)
        {
            throw new Exception("Assigner should not be RValue");
        }

        
        valueToEq.Type.AssignTo(codeGenContext, valueToEq, variableRef);
        return  codeGenContext.GetVoid();
    }

    public LangPath? TypePath { get; private set; } 
}