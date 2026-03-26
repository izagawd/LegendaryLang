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
                valueRef = codeGenContext.Builder.BuildSDiv(leftValRef, rightValRef);
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
        // if less than or greater than should return a bool, if not return the resolved type
        if (OperatorToken.OperatorType is Operator.LessThan or Operator.GreaterThan)
        {
            return new ValueRefItem()
            {
                ValueRef = valueRef,
                Type = new BoolType(new BoolTypeDefinition())
            };
        }

        // Use the resolved output type from Analyze
        var outputTypeRef = codeGenContext.GetRefItemFor(_resolvedOutputType ?? type.TypePath) as TypeRefItem;
        return new ValueRefItem
        {
            ValueRef = valueRef,
            Type = outputTypeRef?.Type ?? type
        };
    }

    private static BoolTypeDefinition BoolDef = new BoolTypeDefinition();
    private static I32TypeDefinition i32Def = new I32TypeDefinition();

    private LangPath? _resolvedOutputType;

    public LangPath? TypePath { get; private set; }

    private static NormalLangPath? GetOperatorTraitPath(Operator op)
    {
        return op switch
        {
            Operator.Add => SemanticAnalyzer.AddTraitPath,
            Operator.Subtract => SemanticAnalyzer.SubTraitPath,
            Operator.Multiply => SemanticAnalyzer.MulTraitPath,
            Operator.Divide => SemanticAnalyzer.DivTraitPath,
            _ => null
        };
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Analyze and mark operands as moved (same as function call args)
        Left.Analyze(analyzer);
        analyzer.TryMarkExpressionAsMoved(Left);
        Right.Analyze(analyzer);
        analyzer.TryMarkExpressionAsMoved(Right);

        if (OperatorToken.OperatorType is Operator.LessThan or Operator.GreaterThan)
        {
            // Comparison operators: both sides must be same type
            if (Left.TypePath != Right.TypePath)
                analyzer.AddException(new SemanticException(
                    $"Both operands must be the same type! '{Left.TypePath}' is not the same as '{Right.TypePath}'\n{Right.Token.GetLocationStringRepresentation()}"));
            TypePath = BoolDef.TypePath;
            return;
        }

        if (!new[] { Operator.Add, Operator.Divide, Operator.Multiply, Operator.Subtract }
                .Contains(OperatorToken.OperatorType))
        {
            analyzer.AddException(new SemanticException(
                $"Operator '{OperatorToken.OperatorType}' is not supported with binary expressions\n{Token.GetLocationStringRepresentation()}"));
            TypePath = LangPath.VoidBaseLangPath;
            return;
        }

        // Check operator trait
        var traitPath = GetOperatorTraitPath(OperatorToken.OperatorType);
        if (traitPath == null)
        {
            TypePath = LangPath.VoidBaseLangPath;
            return;
        }

        // Build trait path with Rhs generic: Add<Rhs> where Rhs = Right.TypePath
        var traitWithRhs = traitPath.Append(new NormalLangPath.GenericTypesPathSegment([Right.TypePath!]));

        // Check if Left type implements the operator trait
        if (!analyzer.TypeImplementsTrait(Left.TypePath!, traitWithRhs))
        {
            analyzer.AddException(new SemanticException(
                $"Type '{Left.TypePath}' does not implement '{traitPath.GetLastPathSegment()}<{Right.TypePath}>'.\n" +
                $"Cannot use operator '{OperatorToken.OperatorType}'\n{Token.GetLocationStringRepresentation()}"));
            TypePath = Left.TypePath;
            _resolvedOutputType = Left.TypePath;
            return;
        }

        // Resolve Output associated type
        var outputType = analyzer.ResolveAssociatedType(Left.TypePath!, traitWithRhs, "Output");
        if (outputType != null)
        {
            TypePath = outputType;
            _resolvedOutputType = outputType;
        }
        else
        {
            // If the left type is a generic param, the Output is genuinely unknown
            // without an associated type constraint like Add<T, Output = T>
            bool isGenericParam = Left.TypePath is NormalLangPath nlpLeft
                && nlpLeft.PathSegments.Length == 1
                && analyzer.IsGenericParam(nlpLeft.PathSegments[0].ToString());

            if (isGenericParam)
            {
                var opName = traitPath.GetLastPathSegment();
                analyzer.AddException(new SemanticException(
                    $"Cannot determine the output type of '{opName}<{Right.TypePath}>' for generic type '{Left.TypePath}'. " +
                    $"Consider constraining the associated type: '{opName}<{Right.TypePath}, Output = ...>'\n{Token.GetLocationStringRepresentation()}"));
            }

            // Fallback: same as left type
            TypePath = Left.TypePath;
            _resolvedOutputType = Left.TypePath;
        }
    }

    public Token Token => (Token)OperatorToken;
}