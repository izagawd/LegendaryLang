using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions;
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

    private static string? GetOperatorMethodName(Operator op) => op switch
    {
        Operator.Add => "Add",
        Operator.Subtract => "Sub",
        Operator.Multiply => "Mul",
        Operator.Divide => "Div",
        Operator.Equals => "Eq",
        Operator.NotEquals => "Ne",
        Operator.LessThan => "Lt",
        Operator.GreaterThan => "Gt",
        _ => null
    };

    private static NormalLangPath? GetOperatorTraitPath(Operator op) => op switch
    {
        Operator.Add => SemanticAnalyzer.AddTraitPath,
        Operator.Subtract => SemanticAnalyzer.SubTraitPath,
        Operator.Multiply => SemanticAnalyzer.MulTraitPath,
        Operator.Divide => SemanticAnalyzer.DivTraitPath,
        Operator.Equals => SemanticAnalyzer.PartialEqTraitPath,
        Operator.NotEquals => SemanticAnalyzer.PartialEqTraitPath,
        Operator.LessThan => SemanticAnalyzer.PartialOrdTraitPath,
        Operator.GreaterThan => SemanticAnalyzer.PartialOrdTraitPath,
        _ => null
    };

    private static bool IsComparisonOperator(Operator op) =>
        op is Operator.Equals or Operator.NotEquals or Operator.LessThan or Operator.GreaterThan;

    private static bool IsArithmeticOperator(Operator op) =>
        op is Operator.Add or Operator.Subtract or Operator.Multiply or Operator.Divide;

    private static bool IsLogicalOperator(Operator op) =>
        op is Operator.And or Operator.Or;

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var leftVal = Left.CodeGen(codeGenContext);
        var rightVal = Right.CodeGen(codeGenContext);

        var type = leftVal.Type;
        var op = OperatorToken.OperatorType;

        // For non-primitive types, dispatch through trait impl methods
        if ((type is not PrimitiveType || rightVal.Type is not PrimitiveType)
            && (IsArithmeticOperator(op) || IsComparisonOperator(op)))
        {
            var traitPath = GetOperatorTraitPath(op);
            var methodName = GetOperatorMethodName(op);
            if (traitPath != null && methodName != null)
            {
                var traitWithRhs = traitPath.AppendGenerics([rightVal.Type.TypePath]);
                var methodPath = traitWithRhs.Append(new NormalLangPath.NormalPathSegment(methodName));

                var funcRef = CallExpressionHelper.ResolveWithTraitBound(
                    codeGenContext, traitWithRhs, type.TypePath, methodPath);

                if (funcRef != null)
                    return CallExpressionHelper.EmitCall(funcRef, [leftVal, rightVal], codeGenContext);
            }
        }

        // Primitive path: use LLVM intrinsic instructions
        var leftValRef = type.LoadValue(codeGenContext, leftVal);
        var rightValRef = type.LoadValue(codeGenContext, rightVal);

        LLVMValueRef valueRef;
        switch (op)
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
            case Operator.Equals:
                valueRef = codeGenContext.Builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, leftValRef, rightValRef);
                break;
            case Operator.NotEquals:
                valueRef = codeGenContext.Builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, leftValRef, rightValRef);
                break;
            case Operator.And:
                valueRef = codeGenContext.Builder.BuildAnd(leftValRef, rightValRef);
                break;
            case Operator.Or:
                valueRef = codeGenContext.Builder.BuildOr(leftValRef, rightValRef);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (IsComparisonOperator(op) || IsLogicalOperator(op))
        {
            return new ValueRefItem()
            {
                ValueRef = valueRef,
                Type = new PrimitiveType(new BoolTypeDefinition(), LLVMTypeRef.Int1, "bool")
            };
        }

        var outputTypeRef = codeGenContext.GetRefItemFor(_resolvedOutputType ?? type.TypePath) as TypeRefItem;
        return new ValueRefItem
        {
            ValueRef = valueRef,
            Type = outputTypeRef?.Type ?? type
        };
    }

    private static BoolTypeDefinition BoolDef = new BoolTypeDefinition();

    private LangPath? _resolvedOutputType;

    public LangPath? TypePath { get; private set; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        CallExpressionHelper.AnalyzeExpressionWithReborrow(Left, analyzer);
        CallExpressionHelper.AnalyzeExpressionWithReborrow(Right, analyzer);

        var op = OperatorToken.OperatorType;

        // Logical operators: both sides must be bool
        if (IsLogicalOperator(op))
        {
            if (Left.TypePath != BoolDef.TypePath)
                analyzer.AddException(new SemanticException(
                    $"Left operand of '{op.ToSymbol()}' must be bool, found '{Left.TypePath}'\n{Left.Token.GetLocationStringRepresentation()}"));
            if (Right.TypePath != BoolDef.TypePath)
                analyzer.AddException(new SemanticException(
                    $"Right operand of '{op.ToSymbol()}' must be bool, found '{Right.TypePath}'\n{Right.Token.GetLocationStringRepresentation()}"));
            TypePath = BoolDef.TypePath;
            return;
        }

        // Comparison operators: check PartialEq/PartialOrd traits
        if (IsComparisonOperator(op))
        {
            var traitPath = GetOperatorTraitPath(op);
            if (traitPath != null)
            {
                var traitWithRhs = traitPath.AppendGenerics([Right.TypePath!]);
                if (!analyzer.TypeImplementsTrait(Left.TypePath!, traitWithRhs))
                {
                    analyzer.AddException(new SemanticException(
                        $"Type '{Left.TypePath}' does not implement '{traitPath.GetLastPathSegment()}({Right.TypePath})'.\n" +
                        $"Cannot use operator '{op.ToSymbol()}'\n{Token.GetLocationStringRepresentation()}"));
                }
            }
            TypePath = BoolDef.TypePath;
            return;
        }

        // Arithmetic operators
        if (!IsArithmeticOperator(op))
        {
            analyzer.AddException(new SemanticException(
                $"Operator '{op.ToSymbol()}' is not supported with binary expressions\n{Token.GetLocationStringRepresentation()}"));
            TypePath = LangPath.VoidBaseLangPath;
            return;
        }

        var arithmeticTraitPath = GetOperatorTraitPath(op);
        if (arithmeticTraitPath == null)
        {
            TypePath = LangPath.VoidBaseLangPath;
            return;
        }

        var arithmeticTraitWithRhs = arithmeticTraitPath.AppendGenerics([Right.TypePath!]);

        if (!analyzer.TypeImplementsTrait(Left.TypePath!, arithmeticTraitWithRhs))
        {
            analyzer.AddException(new SemanticException(
                $"Type '{Left.TypePath}' does not implement '{arithmeticTraitPath.GetLastPathSegment()}({Right.TypePath})'.\n" +
                $"Cannot use operator '{op.ToSymbol()}'\n{Token.GetLocationStringRepresentation()}"));
            TypePath = Left.TypePath;
            _resolvedOutputType = Left.TypePath;
            return;
        }

        var outputType = analyzer.ResolveAssociatedType(Left.TypePath!, arithmeticTraitWithRhs, "Output");
        if (outputType != null)
        {
            TypePath = outputType;
            _resolvedOutputType = outputType;
        }
        else
        {
            bool isGenericParam = Left.TypePath is NormalLangPath nlpLeft
                && nlpLeft.PathSegments.Length == 1
                && analyzer.IsGenericParam(nlpLeft.PathSegments[0].ToString());

            if (isGenericParam)
            {
                var qualifiedOutput = new QualifiedAssocTypePath(Left.TypePath, arithmeticTraitWithRhs, "Output");
                TypePath = qualifiedOutput;
                _resolvedOutputType = qualifiedOutput;
            }
            else
            {
                TypePath = Left.TypePath;
                _resolvedOutputType = Left.TypePath;
            }
        }
    }

    public Token Token => (Token)OperatorToken;
}
