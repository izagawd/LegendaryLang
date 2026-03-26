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
                TypePath = traitMethodReturnType;
            }
            else
            {
                TypePath = LangPath.VoidBaseLangPath;
                analyzer.AddException(
                    new FunctionNotFoundException(FunctionPath, Token.GetLocationStringRepresentation()));
            }
        }

        foreach (var i in Arguments) i.Analyze(analyzer);
    }

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var zaPath = codeGenContext.GetRefItemFor(FunctionPath) as FunctionRefItem;
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