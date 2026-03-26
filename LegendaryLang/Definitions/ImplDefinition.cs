using System.Collections.Immutable;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Definitions;

public class ImplDefinition : IItem, IAnalyzable, IPathResolvable
{
    public ImplDefinition(LangPath traitPath, LangPath forTypePath,
        IEnumerable<FunctionDefinition> methods, Token token,
        IEnumerable<GenericParameter> genericParameters)
    {
        TraitPath = traitPath;
        ForTypePath = forTypePath;
        Methods = methods.ToImmutableArray();
        Token = token;
        GenericParameters = genericParameters.ToImmutableArray();
    }

    public LangPath TraitPath { get; set; }
    public LangPath ForTypePath { get; set; }
    public ImmutableArray<FunctionDefinition> Methods { get; }
    public ImmutableArray<GenericParameter> GenericParameters { get; }

    // IItem
    public bool ImplementsLater => false;
    bool ISyntaxNode.NeedsSemiColonAfterIfNotLastInBlock => false;

    // ISyntaxNode
    public IEnumerable<ISyntaxNode> Children => Methods;
    public Token Token { get; }

    /// <summary>
    /// Returns the set of impl generic param names for pattern matching.
    /// </summary>
    private HashSet<string> GetFreeVariableNames()
    {
        return GenericParameters.Select(gp => gp.Name).ToHashSet();
    }

    /// <summary>
    /// Tries to match a concrete type against this impl's ForTypePath pattern.
    /// Returns the bindings (generic_param_name → concrete_type) or null if no match.
    /// 
    /// For example, impl&lt;T&gt; Foo for Wrapper&lt;T&gt; matching against Wrapper&lt;i32&gt;
    /// returns {T → i32}.
    /// For non-generic impls, returns empty dict on exact match.
    /// </summary>
    public Dictionary<string, LangPath>? TryMatchConcreteType(LangPath concreteType)
    {
        var freeVars = GetFreeVariableNames();
        if (freeVars.Count == 0)
        {
            // Non-generic impl: exact match
            return ForTypePath == concreteType ? new Dictionary<string, LangPath>() : null;
        }

        var bindings = new Dictionary<string, LangPath>();
        if (TryMatch(ForTypePath, concreteType, freeVars, bindings))
        {
            // Verify all free vars are bound
            if (freeVars.All(v => bindings.ContainsKey(v)))
                return bindings;
        }
        return null;
    }

    private static bool TryMatch(LangPath pattern, LangPath concrete, HashSet<string> freeVars,
        Dictionary<string, LangPath> bindings)
    {
        // Pattern is a single-segment name matching a free variable → bind it
        if (pattern is NormalLangPath nlpPat && nlpPat.PathSegments.Length == 1
            && nlpPat.PathSegments[0] is NormalLangPath.NormalPathSegment ns
            && freeVars.Contains(ns.Text))
        {
            if (bindings.TryGetValue(ns.Text, out var existing))
                return existing == concrete; // Same var bound to different types → mismatch
            bindings[ns.Text] = concrete;
            return true;
        }

        // Both must be same type of path
        if (pattern is NormalLangPath nlpPattern && concrete is NormalLangPath nlpConcrete)
        {
            // Compare segment by segment, ignoring empty generic segments
            var patSegs = nlpPattern.PathSegments.ToList();
            var conSegs = nlpConcrete.PathSegments.ToList();

            int pi = 0, ci = 0;
            while (pi < patSegs.Count && ci < conSegs.Count)
            {
                var ps = patSegs[pi];
                var cs = conSegs[ci];

                if (ps is NormalLangPath.GenericTypesPathSegment patGen
                    && cs is NormalLangPath.GenericTypesPathSegment conGen)
                {
                    if (patGen.TypePaths.Length != conGen.TypePaths.Length) return false;
                    for (int i = 0; i < patGen.TypePaths.Length; i++)
                        if (!TryMatch(patGen.TypePaths[i], conGen.TypePaths[i], freeVars, bindings))
                            return false;
                    pi++; ci++;
                }
                else if (ps is NormalLangPath.GenericTypesPathSegment pg && pg.TypePaths.Length == 0)
                { pi++; }
                else if (cs is NormalLangPath.GenericTypesPathSegment cg && cg.TypePaths.Length == 0)
                { ci++; }
                else
                {
                    if (ps != cs) return false;
                    pi++; ci++;
                }
            }
            // Skip trailing empty generic segments
            while (pi < patSegs.Count && patSegs[pi] is NormalLangPath.GenericTypesPathSegment epg && epg.TypePaths.Length == 0) pi++;
            while (ci < conSegs.Count && conSegs[ci] is NormalLangPath.GenericTypesPathSegment ecg && ecg.TypePaths.Length == 0) ci++;
            return pi == patSegs.Count && ci == conSegs.Count;
        }

        if (pattern is TupleLangPath tlpPat && concrete is TupleLangPath tlpCon)
        {
            if (tlpPat.TypePaths.Length != tlpCon.TypePaths.Length) return false;
            for (int i = 0; i < tlpPat.TypePaths.Length; i++)
                if (!TryMatch(tlpPat.TypePaths[i], tlpCon.TypePaths[i], freeVars, bindings))
                    return false;
            return true;
        }

        return pattern == concrete;
    }

    /// <summary>
    /// Checks whether all generic parameter bounds are satisfied for the given bindings.
    /// </summary>
    public bool CheckBounds(Dictionary<string, LangPath> bindings, SemanticAnalyzer analyzer)
    {
        foreach (var gp in GenericParameters)
        {
            if (!bindings.TryGetValue(gp.Name, out var boundType)) return false;
            foreach (var bound in gp.TraitBounds)
            {
                if (!analyzer.TypeImplementsTrait(boundType, bound))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Checks whether all generic parameter bounds are satisfied for the given bindings (codegen version).
    /// </summary>
    public bool CheckBoundsCodeGen(Dictionary<string, LangPath> bindings, CodeGenContext context)
    {
        foreach (var gp in GenericParameters)
        {
            if (!bindings.TryGetValue(gp.Name, out var boundType)) return false;
            foreach (var bound in gp.TraitBounds)
            {
                if (!context.ImplDefinitions.Any(i =>
                {
                    var match = i.TryMatchConcreteType(boundType);
                    return match != null && i.TraitPath == bound;
                }))
                    return false;
            }
        }
        return true;
    }

    private static bool GenericParamUsedInType(string paramName, LangPath? typePath)
    {
        if (typePath is NormalLangPath nlp)
        {
            foreach (var seg in nlp.PathSegments)
            {
                if (seg is NormalLangPath.NormalPathSegment ns && ns.Text == paramName) return true;
                if (seg is NormalLangPath.GenericTypesPathSegment gts)
                    foreach (var tp in gts.TypePaths)
                        if (GenericParamUsedInType(paramName, tp)) return true;
            }
        }
        if (typePath is TupleLangPath tlp)
        {
            foreach (var tp in tlp.TypePaths)
                if (GenericParamUsedInType(paramName, tp)) return true;
        }
        return false;
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Validate that the trait exists
        var traitDef = analyzer.GetDefinition(TraitPath) as TraitDefinition;
        if (traitDef == null)
        {
            analyzer.AddException(new TraitNotFoundException(TraitPath, Token.GetLocationStringRepresentation()));
            return;
        }

        // Check for unused impl generic parameters
        foreach (var gp in GenericParameters)
        {
            if (!GenericParamUsedInType(gp.Name, ForTypePath))
                analyzer.AddException(new SemanticException(
                    $"Generic parameter '{gp.Name}' is never used in the implementing type '{ForTypePath}'\n{Token.GetLocationStringRepresentation()}"));
        }

        // Validate that each trait method is implemented
        foreach (var traitMethod in traitDef.MethodSignatures)
        {
            var implMethod = Methods.FirstOrDefault(m => m.Name == traitMethod.Name);
            if (implMethod == null)
            {
                analyzer.AddException(new TraitMethodNotImplementedException(
                    traitMethod.Name, TraitPath, Token.GetLocationStringRepresentation()));
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
                analyzer.AddException(new TraitExtraMethodException(
                    implMethod.Name, TraitPath, implMethod.Token.GetLocationStringRepresentation()));
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

        // Resolve generic param trait bounds
        foreach (var gp in GenericParameters)
            for (int i = 0; i < gp.TraitBounds.Count; i++)
                gp.TraitBounds[i] = gp.TraitBounds[i].Resolve(resolver);

        // Add a scope with Self mapped to the implementing type
        resolver.AddScope();
        if (ForTypePath is NormalLangPath nlp)
            resolver.AddToDeepestScope("Self", nlp);

        // Resolve paths in each method (Self in params/return types becomes the concrete type)
        foreach (var method in Methods)
            method.ResolvePaths(resolver);

        resolver.PopScope();
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

        // Parse optional generic parameters: impl<T: Copy + Foo, U>
        var genericParameters = new List<GenericParameter>();
        if (parser.Peek() is OperatorToken { OperatorType: Operator.LessThan })
        {
            parser.Pop();
            var nextToken = parser.Peek();
            while (nextToken is not OperatorToken { OperatorType: Operator.GreaterThan })
            {
                var paramIdentifier = Identifier.Parse(parser);
                var traitBounds = new List<LangPath>();
                if (parser.Peek() is ColonToken)
                {
                    parser.Pop();
                    if (parser.Peek() is not OperatorToken { OperatorType: Operator.GreaterThan }
                        && parser.Peek() is not CommaToken)
                    {
                        traitBounds.Add(LangPath.Parse(parser));
                        while (parser.Peek() is OperatorToken { OperatorType: Operator.Add })
                        {
                            parser.Pop();
                            traitBounds.Add(LangPath.Parse(parser));
                        }
                    }
                }
                nextToken = parser.Peek();
                genericParameters.Add(new GenericParameter(paramIdentifier, traitBounds));
                if (nextToken is CommaToken)
                {
                    parser.Pop();
                    nextToken = parser.Peek();
                }
                else
                {
                    break;
                }
            }
            Comparator.ParseGreater(parser);
        }

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

        return new ImplDefinition(traitPath, forTypePath, methods, (Token)implToken, genericParameters);
    }
}
