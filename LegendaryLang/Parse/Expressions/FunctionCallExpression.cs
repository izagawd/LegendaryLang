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

    public void Analyze(SemanticAnalyzer analyzer)
    {
        var def = analyzer.GetDefinition(FunctionPath);
        var popped = FunctionPath.Pop();
        if (def is null) def = analyzer.GetDefinition(FunctionPath.Pop());
        if (def is FunctionDefinition fd)
        {
            if (fd.GenericParameters.Length != FunctionPath.GetFrontGenerics().Length)
            {
                analyzer.AddException(new GenericParamCountException(
                    fd.GenericParameters.Length, FunctionPath.GetFrontGenerics().Length,
                    Token.GetLocationStringRepresentation()));
                TypePath = fd.ReturnTypePath;
            }
            else
            {
                TypePath = fd.GetMonomorphizedReturnTypePath(FunctionPath);

                // Check trait bounds: each generic arg must satisfy its param's trait bounds
                var genericArgs = FunctionPath.GetFrontGenerics();
                for (int i = 0; i < fd.GenericParameters.Length; i++)
                {
                    var gp = fd.GenericParameters[i];
                    foreach (var bound in gp.TraitBounds)
                    {
                        var argType = genericArgs[i];
                        if (!analyzer.TypeImplementsTrait(argType, bound))
                        {
                            analyzer.AddException(new TraitBoundViolationException(argType, bound));
                        }
                    }
                }
            }
        }
        else
        {
            // Try trait method call: TraitName::method(...)
            var traitMethodReturnType = analyzer.ResolveTraitMethodReturnType(FunctionPath);
            if (traitMethodReturnType != null)
            {
                // For <ConcreteType as Trait>::method() — verify the type implements the trait
                // Skip if QualifiedAsType is a generic parameter (bounds validated at call site)
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

                // If return type is Self, substitute with the qualified concrete type if available
                if (traitMethodReturnType is NormalLangPath nlpRet && nlpRet.PathSegments.Length == 1
                    && nlpRet.PathSegments[0].ToString() == "Self" && QualifiedAsType != null)
                {
                    TypePath = QualifiedAsType;
                }
                else
                {
                    TypePath = traitMethodReturnType;
                }
            }
            else
            {
                TypePath = LangPath.VoidBaseLangPath;
                analyzer.AddException(
                    new FunctionNotFoundException(FunctionPath, Token.GetLocationStringRepresentation()));
            }
        }

        // Analyze each argument and immediately mark as moved if not Copy.
        // This ensures f(w, w) catches use-after-move on the second w.
        foreach (var arg in Arguments)
        {
            arg.Analyze(analyzer);
            analyzer.TryMarkExpressionAsMoved(arg);
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