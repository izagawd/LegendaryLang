using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class BinaryOperationExpression : IExpression
{
    public BinaryOperationExpression(IExpression left, OperatorToken operatorToken, IExpression right)
    {
        Left = left;
        Right = right;
        OperatorToken = operatorToken;
    }

    public IExpression Left { get; }
    public IExpression Right { get; }
    public OperatorToken OperatorToken { get; }


    public IEnumerable<ISyntaxNode> Children => [Left, Right];

    public bool HasGuaranteedExplicitReturn => Left.HasGuaranteedExplicitReturn || Right.HasGuaranteedExplicitReturn;

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var leftVal = Left.CodeGen(codeGenContext);
        var rightVal = Right.CodeGen(codeGenContext);
        ;
        LLVMValueRef valueRef = null;
        var type = leftVal.Type;

        var leftValRef = type.LoadValue(codeGenContext, leftVal);
        var rightValRef = type.LoadValue(codeGenContext, rightVal);
        switch (OperatorToken.OperatorType)
        {
            case Operator.Add:
                valueRef = codeGenContext.Builder.BuildAdd(leftValRef, rightValRef);
                break;
            case Operator.Subtract:
                valueRef = codeGenContext.Builder.BuildSub(leftValRef, rightValRef);
                break;
            case Operator.Multiply:
                valueRef = codeGenContext.Builder.BuildMul(leftValRef, rightValRef);
                break;
            case Operator.Divide:
                valueRef = codeGenContext.Builder.BuildFDiv(leftValRef, rightValRef);
                break;
            case Operator.LessThan:
                valueRef = codeGenContext.Builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, leftValRef, rightValRef);
                break;
            case Operator.GreaterThan:
                valueRef = codeGenContext.Builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, leftValRef, rightValRef);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        // if less than or greater thanm should return a bool, if not return an int
        if (OperatorToken.OperatorType is Operator.LessThan or Operator.GreaterThan)
        {
            return new ValueRefItem()
            {
                ValueRef = valueRef,
                Type = new BoolType(new BoolTypeDefinition())
            };
        }

        return new ValueRefItem
        {
            ValueRef = valueRef,
            Type = type
        };
    
    }
    private static BoolTypeDefinition BoolDef = new BoolTypeDefinition();
    private static I32TypeDefinition i32Def = new I32TypeDefinition();
    public LangPath? TypePath
    {
        get
        {
            if (OperatorToken.OperatorType is Operator.LessThan or Operator.GreaterThan)
            {
                return BoolDef.TypePath;
            }
            else
            {
                return i32Def.TypePath;
            }
        }
    }


    public void Analyze(SemanticAnalyzer analyzer)
    {
        Left.Analyze(analyzer);
        Right.Analyze(analyzer);

        if (OperatorToken.OperatorType != Operator.LessThan && OperatorToken.OperatorType != Operator.GreaterThan)
        {
            if (Left.TypePath != TypePath)
                analyzer.AddException(
                    new SemanticException($"Both operands must be i32s!\n{Left.Token.GetLocationStringRepresentation()}"));
            if (Right.TypePath != TypePath)
                analyzer.AddException(
                    new SemanticException($"Both operands must be i32s!\n{Right.Token.GetLocationStringRepresentation()}"));

        } else if (Left.TypePath != Right.TypePath)
        {
            analyzer.AddException(
                new SemanticException($"Both operands must be the same!\n{Left.TypePath} is not the same as {Right.TypePath}!\n {Right.Token.GetLocationStringRepresentation()}"));
        }
        
        

        if (!new[] { Operator.Add, Operator.Divide, Operator.Multiply, Operator.Subtract, Operator.LessThan, Operator.GreaterThan }.Contains(OperatorToken
                .OperatorType))
            analyzer.AddException(new SemanticException(
                $"Operator '{OperatorToken.OperatorType}' is not supported with binary expressions\n{Token.GetLocationStringRepresentation()}"));
    }

    public Token Token => (Token)OperatorToken;
}