using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Definitions;

public class ImplDefinition : IItem, IAnalyzable, IPathResolvable
{
    public ImplDefinition(LangPath traitPath, LangPath forTypePath,
        IEnumerable<FunctionDefinition> methods, Token token)
    {
        TraitPath = traitPath;
        ForTypePath = forTypePath;
        Methods = methods.ToImmutableArray();
        Token = token;
    }

    public LangPath TraitPath { get; set; }
    public LangPath ForTypePath { get; set; }
    public ImmutableArray<FunctionDefinition> Methods { get; }

    // IItem
    public bool ImplementsLater => false;
    bool ISyntaxNode.NeedsSemiColonAfterIfNotLastInBlock => false;

    // ISyntaxNode
    public IEnumerable<ISyntaxNode> Children => Methods;
    public Token Token { get; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Validate that the trait exists
        var traitDef = analyzer.GetDefinition(TraitPath) as TraitDefinition;
        if (traitDef == null)
        {
            analyzer.AddException(new SemanticException(
                $"Trait '{TraitPath}' not found\n{Token.GetLocationStringRepresentation()}"));
            return;
        }

        // Validate that each trait method is implemented
        foreach (var traitMethod in traitDef.MethodSignatures)
        {
            var implMethod = Methods.FirstOrDefault(m => m.Name == traitMethod.Name);
            if (implMethod == null)
            {
                analyzer.AddException(new SemanticException(
                    $"Method '{traitMethod.Name}' from trait '{TraitPath}' is not implemented\n{Token.GetLocationStringRepresentation()}"));
                continue;
            }

            if (implMethod.Arguments.Length != traitMethod.Parameters.Length)
            {
                analyzer.AddException(new SemanticException(
                    $"Method '{traitMethod.Name}' has {implMethod.Arguments.Length} parameters, " +
                    $"but the trait requires {traitMethod.Parameters.Length}\n{implMethod.Token.GetLocationStringRepresentation()}"));
            }
        }

        // Check for extra methods not in the trait
        foreach (var implMethod in Methods)
        {
            if (!traitDef.MethodSignatures.Any(m => m.Name == implMethod.Name))
            {
                analyzer.AddException(new SemanticException(
                    $"Method '{implMethod.Name}' is not defined in trait '{TraitPath}'\n{implMethod.Token.GetLocationStringRepresentation()}"));
            }
        }

        // Analyze each method body
        foreach (var method in Methods)
            method.Analyze(analyzer);
    }

    public void ResolvePaths(PathResolver resolver)
    {
        // Resolve the trait and type paths first
        TraitPath = TraitPath.Resolve(resolver);
        ForTypePath = ForTypePath.Resolve(resolver);

        // Add a scope with Self mapped to the implementing type
        resolver.AddScope();
        if (ForTypePath is NormalLangPath nlp)
            resolver.AddToDeepestScope("Self", nlp);

        // Resolve paths in each method (Self in params/return types becomes the concrete type)
        foreach (var method in Methods)
            method.ResolvePaths(resolver);

        resolver.PopScope();
    }

    /// <summary>
    /// Gets a synthetic module path for impl methods to ensure unique LLVM names
    /// </summary>
    public NormalLangPath GetImplModulePath()
    {
        return new NormalLangPath(null,
            [new NormalLangPath.NormalPathSegment($"impl_{TraitPath}_for_{ForTypePath}")]);
    }

    public FunctionDefinition? GetMethod(string methodName)
    {
        return Methods.FirstOrDefault(m => m.Name == methodName);
    }

    public static ImplDefinition Parse(Parser parser, NormalLangPath module)
    {
        var implToken = parser.Pop();
        if (implToken is not ImplToken)
            throw new ExpectedParserException(parser, ParseType.Impl, implToken);

        var traitPath = LangPath.Parse(parser);

        var forTok = parser.Pop();
        if (forTok is not ForToken)
            throw new ExpectedParserException(parser, ParseType.For, forTok);

        var forTypePath = LangPath.Parse(parser);

        CurlyBrace.ParseLeft(parser);

        // Create a synthetic module for the impl methods
        var implModule = new NormalLangPath(null,
            [new NormalLangPath.NormalPathSegment($"impl_{traitPath}_for_{forTypePath}")]);

        var methods = new List<FunctionDefinition>();
        while (parser.Peek() is not RightCurlyBraceToken)
        {
            methods.Add(FunctionDefinition.Parse(parser, implModule));
        }

        CurlyBrace.Parseight(parser);

        return new ImplDefinition(traitPath, forTypePath, methods, (Token)implToken);
    }
}
