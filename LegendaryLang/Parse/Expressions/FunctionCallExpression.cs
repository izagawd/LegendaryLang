using System.Collections.Immutable;
using LegendaryLang.Definitions;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public class FunctionCallExpression : IExpression
{
    public FunctionCallExpression(NormalLangPath path, IEnumerable<IExpression> arguments)
    {
        Arguments = arguments.ToImmutableArray();
        FunctionPath = path;
    }


    public ImmutableArray<IExpression> Arguments { get; }

    public ImmutableArray<LangPath> GenericArguments =>
        (FunctionPath?.GetLastPathSegment() as NormalLangPath.GenericTypesPathSegment)?.TypePaths ?? [];

    public NormalLangPath FunctionPath { get; set; }

    /// <summary>
    /// Set when parsing &lt;ConcreteType as Trait&gt;::method() — the concrete type (e.g., i32).
    /// Used to substitute Self in trait method return types.
    /// </summary>
    public LangPath? QualifiedAsType { get; set; }

    public IEnumerable<ISyntaxNode> Children => Arguments;

    public Token Token => FunctionPath.FirstIdentifierToken!;

    /// <summary>
    /// When set by LetStatement, the declared type on the LHS.
    /// Used to infer generic return type (e.g., let a: i32 = make()).
    /// </summary>
    public LangPath? ExpectedReturnType { get; set; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Analyze args first — we need their types for inference
        foreach (var arg in Arguments)
        {
            arg.Analyze(analyzer);
            analyzer.TryMarkExpressionAsMoved(arg);
        }

        var def = analyzer.GetDefinition(FunctionPath);
        var popped = FunctionPath.Pop();
        if (def is null) def = analyzer.GetDefinition(FunctionPath.Pop());
        if (def is FunctionDefinition fd)
        {
            var providedGenerics = FunctionPath.GetFrontGenerics();

            // === TYPE INFERENCE ===
            if (fd.GenericParameters.Length > 0 && providedGenerics.Length == 0)
            {
                var constraints = new List<(LangPath, LangPath)>();

                // Infer from argument types
                for (int i = 0; i < fd.Arguments.Length && i < Arguments.Length; i++)
                {
                    if (fd.Arguments[i].TypePath != null && Arguments[i].TypePath != null)
                        constraints.Add((fd.Arguments[i].TypePath, Arguments[i].TypePath));
                }

                // Infer from expected return type (let a: i32 = foo())
                if (ExpectedReturnType != null && fd.ReturnTypePath != null)
                    constraints.Add((fd.ReturnTypePath, ExpectedReturnType));

                var inferred = TypeInference.InferFromConstraints(fd.GenericParameters, constraints);
                if (inferred != null)
                {
                    FunctionPath = FunctionPath.Append(new NormalLangPath.GenericTypesPathSegment(inferred.Value));
                    providedGenerics = inferred.Value;
                }
                else
                {
                    analyzer.AddException(new CannotInferGenericArgsException(
                        fd.Name, Token.GetLocationStringRepresentation()));
                    TypePath = fd.ReturnTypePath;
                    return;
                }
            }

            if (fd.GenericParameters.Length != providedGenerics.Length)
            {
                analyzer.AddException(new GenericParamCountException(
                    fd.GenericParameters.Length, providedGenerics.Length,
                    Token.GetLocationStringRepresentation()));
                TypePath = fd.ReturnTypePath;
            }
            else
            {
                TypePath = fd.GetMonomorphizedReturnTypePath(FunctionPath);

                // Resolve qualified associated type paths in the monomorphized return type
                if (TypePath != null)
                    TypePath = analyzer.ResolveQualifiedTypePath(TypePath);

                var genericArgs = FunctionPath.GetFrontGenerics();
                for (int i = 0; i < fd.GenericParameters.Length; i++)
                {
                    var gp = fd.GenericParameters[i];
                    foreach (var bound in gp.TraitBounds)
                    {
                        var argType = genericArgs[i];
                        // Substitute generic params in the bound (e.g., Add<T> → Add<i32>)
                        var resolvedBound = FieldAccessExpression.SubstituteGenerics(
                            bound.TraitPath, fd.GenericParameters, genericArgs);
                        if (!analyzer.TypeImplementsTrait(argType, resolvedBound))
                        {
                            analyzer.AddException(new TraitBoundViolationException(argType, resolvedBound));
                        }

                        // Validate associated type constraints (e.g., Output = T)
                        foreach (var (atName, atType) in bound.AssociatedTypeConstraints)
                        {
                            var resolvedAtType = FieldAccessExpression.SubstituteGenerics(
                                atType, fd.GenericParameters, genericArgs);
                            var actualAt = analyzer.ResolveAssociatedType(argType, resolvedBound, atName);
                            if (actualAt != null && actualAt != resolvedAtType)
                            {
                                analyzer.AddException(new SemanticException(
                                    $"Associated type constraint '{atName} = {resolvedAtType}' not satisfied: " +
                                    $"actual '{atName}' is '{actualAt}'\n{Token.GetLocationStringRepresentation()}"));
                            }
                        }
                    }
                }
            }
        }
        else
        {
            var traitMethodReturnType = analyzer.ResolveTraitMethodReturnType(FunctionPath);
            if (traitMethodReturnType != null)
            {
                if (QualifiedAsType != null)
                {
                    bool isGenericParam = QualifiedAsType is NormalLangPath nlpQual
                        && nlpQual.PathSegments.Length == 1
                        && analyzer.IsGenericParam(nlpQual.PathSegments[0].ToString());

                    if (!isGenericParam)
                    {
                        var traitPath = FunctionPath.Pop();
                        if (traitPath != null && !analyzer.TypeImplementsTrait(QualifiedAsType, traitPath))
                        {
                            analyzer.AddException(new TraitBoundViolationException(QualifiedAsType, traitPath));
                        }
                    }
                }

                if (traitMethodReturnType is NormalLangPath nlpRet && nlpRet.PathSegments.Length == 1)
                {
                    var retName = nlpRet.PathSegments[0].ToString();

                    if (retName == "Self" && QualifiedAsType != null)
                    {
                        TypePath = QualifiedAsType;
                    }
                    else
                    {
                        // Try to resolve as an associated type
                        var traitPath = FunctionPath.Pop();
                        LangPath? resolved = null;
                        if (QualifiedAsType != null && traitPath != null)
                        {
                            resolved = analyzer.ResolveAssociatedType(QualifiedAsType, traitPath, retName);
                        }
                        TypePath = resolved ?? traitMethodReturnType;
                    }
                }
                else if (traitMethodReturnType is QualifiedAssocTypePath qpRet)
                {
                    // Substitute Self with the concrete qualified-as type
                    var resolvedQp = qpRet;
                    if (QualifiedAsType != null)
                    {
                        var forType = qpRet.ForType;
                        var traitInQp = qpRet.TraitPath;
                        if (forType is NormalLangPath nlpFor && nlpFor.PathSegments.Length == 1
                            && nlpFor.PathSegments[0].ToString() == "Self")
                            forType = QualifiedAsType;
                        if (traitInQp is NormalLangPath nlpTrait)
                        {
                            var newSegs = new List<NormalLangPath.PathSegment>();
                            foreach (var seg in nlpTrait.PathSegments)
                            {
                                if (seg is NormalLangPath.GenericTypesPathSegment gts)
                                {
                                    var newTypes = gts.TypePaths.Select(tp =>
                                        tp is NormalLangPath nlpTp && nlpTp.PathSegments.Length == 1
                                        && nlpTp.PathSegments[0].ToString() == "Self" && QualifiedAsType != null
                                            ? QualifiedAsType : tp).ToList();
                                    newSegs.Add(new NormalLangPath.GenericTypesPathSegment(newTypes));
                                }
                                else newSegs.Add(seg);
                            }
                            traitInQp = new NormalLangPath(nlpTrait.FirstIdentifierToken, newSegs);
                        }
                        resolvedQp = new QualifiedAssocTypePath(forType, traitInQp, qpRet.AssociatedTypeName);
                    }
                    TypePath = analyzer.ResolveQualifiedTypePath(resolvedQp);
                }
                else
                {
                    TypePath = analyzer.ResolveQualifiedTypePath(traitMethodReturnType);
                }
            }
            else
            {
                TypePath = LangPath.VoidBaseLangPath;
                analyzer.AddException(
                    new FunctionNotFoundException(FunctionPath, Token.GetLocationStringRepresentation()));
            }
        }
    }

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        // For <ConcreteType as Trait>::method() calls, push a temporary trait bound
        // so ResolveTraitMethodCall can find the impl
        bool pushedTempBound = false;
        if (QualifiedAsType != null)
        {
            var traitPath = FunctionPath.Pop(); // Trait path (parent of method)
            if (traitPath != null)
            {
                var resolvedType = QualifiedAsType.Monomorphize(codeGenContext);
                codeGenContext.PushTraitBounds([(traitPath, resolvedType)]);
                pushedTempBound = true;
            }
        }

        var zaPath = codeGenContext.GetRefItemFor(FunctionPath) as FunctionRefItem;

        if (pushedTempBound)
            codeGenContext.PopTraitBounds();

        var callResult = codeGenContext.Builder.BuildCall2(zaPath.Function.FunctionType,
            zaPath.Function.FunctionValueRef,
            Arguments.Select(i =>
            {
                var gened = i.CodeGen(codeGenContext);
                return gened.Type.LoadValue(codeGenContext, gened);
            }).ToArray()
        );

        var returnType = zaPath.Function.ReturnType;
        var stackPtr = returnType.AssignToStack(codeGenContext, new ValueRefItem
        {
            Type = returnType,
            ValueRef = callResult
        });


        return new ValueRefItem
        {
            Type = returnType,
            ValueRef = stackPtr
        };
    }

    public LangPath? TypePath { get; set; }

    public void ResolvePaths(PathResolver resolver)
    {
        FunctionPath = (NormalLangPath)FunctionPath.Resolve(resolver);
        if (QualifiedAsType != null)
            QualifiedAsType = QualifiedAsType.Resolve(resolver);
        foreach (var i in Children.OfType<IPathResolvable>())
        {
            i.ResolvePaths(resolver);
        }
    }

    public static FunctionCallExpression ParseFunctionCallExpression(Parser parser,
        NormalLangPath normalLangPath)
    {
        var leftParenth = Parenthesis.ParseLeft(parser);
        var currentToken = parser.Peek();
        var expressions = new List<IExpression>();
        while (currentToken is not RightParenthesisToken)
        {
            var expression = IExpression.Parse(parser);
            expressions.Add(expression);
            currentToken = parser.Peek();
            if (currentToken is CommaToken)
            {
                Comma.Parse(parser);
                currentToken = parser.Peek();
            }
        }

        Parenthesis.ParseRight(parser);
        return new FunctionCallExpression(normalLangPath, expressions);
    }

    public bool HasGuaranteedExplicitReturn => Arguments.Any(i => i.HasGuaranteedExplicitReturn);
}