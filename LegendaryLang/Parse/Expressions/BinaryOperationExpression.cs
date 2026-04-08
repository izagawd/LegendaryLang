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
        Operator.LessEqual => "Le",
        Operator.GreaterEqual => "Ge",
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
        Operator.LessEqual => SemanticAnalyzer.PartialOrdTraitPath,
        Operator.GreaterEqual => SemanticAnalyzer.PartialOrdTraitPath,
        _ => null
    };

    private static bool IsComparisonOperator(Operator op) =>
        op is Operator.Equals or Operator.NotEquals or Operator.LessThan or Operator.GreaterThan
            or Operator.LessEqual or Operator.GreaterEqual;

    private static bool IsArithmeticOperator(Operator op) =>
        op is Operator.Add or Operator.Subtract or Operator.Multiply or Operator.Divide;

    private static bool IsLogicalOperator(Operator op) =>
        op is Operator.And or Operator.Or;

    /// <summary>
    /// Spills a value to an anonymous temporary alloca and wraps it into a shared reference.
    /// Used for comparison operators whose trait methods take &Self / &Rhs:
    /// even literal or temporary values need a stable address to take a reference to.
    /// Reuses SpillToAnonymousLocal (same pattern as DerefExpression for temporary derefs).
    /// </summary>
    private static ValueRefItem SpillAndRef(ValueRefItem val, CodeGenContext ctx)
    {
        var rawPtr = ctx.SpillToAnonymousLocal(val);

        var refTypePath = RefTypeDefinition.GetRefModule()
            .Append(RefTypeDefinition.GetRefName(RefKind.Shared))
            .AppendGenerics([val.Type.TypePath]);
        var refTypeRef = ctx.GetRefItemFor(refTypePath) as TypeRefItem;
        if (refTypeRef?.Type is not RefType refType)
            throw new InvalidOperationException($"Cannot build &{val.Type.TypePath} reference");

        var refAlloca = ctx.Builder.BuildAlloca(refType.TypeRef);
        var field0 = ctx.Builder.BuildStructGEP2(refType.TypeRef, refAlloca, 0);
        ctx.Builder.BuildStore(rawPtr, field0);
        return new ValueRefItem { Type = refType, ValueRef = refAlloca };
    }

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
                {
                    if (IsComparisonOperator(op))
                    {
                        // Comparison trait methods take &Self and &Rhs. Spill each operand to
                        // an anonymous temporary (same pattern as DerefExpression for temporary derefs),
                        // then wrap the alloca into a {ptr, {}} ref struct to pass as the reference arg.
                        var leftRef = SpillAndRef(leftVal, codeGenContext);
                        var rightRef = SpillAndRef(rightVal, codeGenContext);
                        return CallExpressionHelper.EmitCall(funcRef, [leftRef, rightRef], codeGenContext);
                    }

                    return CallExpressionHelper.EmitCall(funcRef, [leftVal, rightVal], codeGenContext);
                }
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
            case Operator.LessEqual:
                valueRef = codeGenContext.Builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, leftValRef, rightValRef);
                break;
            case Operator.GreaterEqual:
                valueRef = codeGenContext.Builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, leftValRef, rightValRef);
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
    public bool IsTemporary => true; // operator call produces new value

    public void Analyze(SemanticAnalyzer analyzer)
    {
        var op = OperatorToken.OperatorType;

        if (IsComparisonOperator(op) || IsLogicalOperator(op))
        {
            // Comparison operators take &Self/&Rhs — operands are shared-borrowed.
            // Uses the same borrow analysis as explicit & expressions.
            PointerGetterExpression.AnalyzeBorrow(Left, RefKind.Shared, analyzer);
            PointerGetterExpression.AnalyzeBorrow(Right, RefKind.Shared, analyzer);

            // Check for conflicting borrows across both operands
            CallExpressionHelper.CheckBorrowConflicts(
                [Left, Right], analyzer, Left.Token);
        }
        else
        {
            // Arithmetic operators take values — may move non-Copy operands.
            CallExpressionHelper.AnalyzeExpressionWithReborrow(Left, analyzer);
            CallExpressionHelper.AnalyzeExpressionWithReborrow(Right, analyzer);
        }

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
