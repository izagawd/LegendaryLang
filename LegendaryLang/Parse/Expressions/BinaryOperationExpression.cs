using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class BinaryOperationExpression : IExpression
{
    
    public IExpression Left { get; }
    public IExpression Right { get; }
    public IOperatorToken OperatorToken { get;  }

    public BinaryOperationExpression(IExpression left, IOperatorToken @operatorToken, IExpression right) 
    {                                                 
        Left = left;
        Right = right;
        OperatorToken = @operatorToken;
    }



    public IEnumerable<ISyntaxNode> Children => [Left, Right];

    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        var leftVal = Left.DataRefCodeGen(codeGenContext);
        var rightVal = Right.DataRefCodeGen(codeGenContext);
;
        LLVMValueRef valueRef = null;
        var type = leftVal.Type;
        
        var leftValRef = type.LoadValue(codeGenContext, leftVal);
        var rightValRef = type.LoadValue(codeGenContext, rightVal);
        
        switch (OperatorToken.Operator)
        {
            case Operator.Add:
                valueRef = codeGenContext.Builder.BuildAdd(  leftValRef, rightValRef);
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
            default:
                throw new ArgumentOutOfRangeException();
        }

        return new VariableRefItem()
        {
            ValueRef = valueRef,
            Type = type,
        };
    }

    public LangPath? TypePath { get; } = new I32TypeDefinition().TypePath;




    public void Analyze(SemanticAnalyzer analyzer)
    {
       Left.Analyze(analyzer);
       Right.Analyze(analyzer);

       if (Left.TypePath != TypePath)
       {
           analyzer.AddException(new SemanticException($"Both operands must be i32s!\n{(Left.Token.GetLocationStringRepresentation())}"));
       }
       if (Right.TypePath != TypePath)
       {
           analyzer.AddException(new SemanticException($"Both operands must be i32s!\n{(Right.Token.GetLocationStringRepresentation())}"));
       }


       if (!new[] { Operator.Add, Operator.Divide, Operator.Multiply, Operator.Subtract }.Contains(OperatorToken
               .Operator))
       {
           analyzer.AddException(new SemanticException($"Operator '{OperatorToken.Operator}' is not supported with binary expressions\n{Token.GetLocationStringRepresentation()}"));
       }
       
    }

    public Token Token => (Token)OperatorToken;
}