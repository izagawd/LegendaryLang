using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Definitions;

public class TraitAssociatedType
{
    public required string Name { get; init; }
    public required Token Token { get; init; }
    public List<LangPath> TraitBounds { get; init; } = new();

    public void ResolvePaths(PathResolver resolver)
    {
        for (int i = 0; i < TraitBounds.Count; i++)
            TraitBounds[i] = TraitBounds[i].Resolve(resolver);
    }
}

public class TraitMethodSignature
{
    public required string Name { get; init; }
    public required Token Token { get; init; }
    public required ImmutableArray<VariableDefinition> Parameters { get; init; }
    public LangPath ReturnTypePath { get; set; }
    public ImmutableArray<GenericParameter> GenericParameters { get; init; } = [];
    public ImmutableArray<string> LifetimeParameters { get; init; } = [];
    public Dictionary<int, string> ArgumentLifetimes { get; init; } = new();
    public string? ReturnLifetime { get; init; }

    public static TraitMethodSignature Parse(Parser parser)
    {
        var fnTok = parser.Pop();
        if (fnTok is not FnToken)
            throw new ExpectedParserException(parser, ParseType.Fn, fnTok);

        var nameToken = Identifier.Parse(parser);

        // Parse generic parameters (lifetimes + type params) — shared with FunctionDefinition
        var generics = FunctionSignatureParser.ParseGenericParams(parser);
        var genericParameters = generics?.GenericParameters ?? [];
        var lifetimeParameters = generics?.LifetimeParameters ?? [];

        // Parse function parameters — shared with FunctionDefinition
        var paramsResult = FunctionSignatureParser.ParseFunctionParams(parser);

        // Parse return type — shared with FunctionDefinition
        var returnResult = FunctionSignatureParser.ParseReturnType(parser);

        // Parse optional body (for default method implementations)
        if (parser.Peek() is LeftCurlyBraceToken)
        {
            BlockExpression.Parse(parser, returnResult.ReturnTypePath);
        }
        else
        {
            SemiColon.Parse(parser);
        }

        return new TraitMethodSignature
        {
            Name = nameToken.Identity,
            Token = nameToken,
            Parameters = paramsResult.Parameters,
            ReturnTypePath = returnResult.ReturnTypePath,
            GenericParameters = genericParameters,
            LifetimeParameters = lifetimeParameters,
            ArgumentLifetimes = paramsResult.ArgumentLifetimes,
            ReturnLifetime = returnResult.ReturnLifetime
        };
    }

    public void ResolvePaths(PathResolver resolver)
    {
        ReturnTypePath = ReturnTypePath.Resolve(resolver);
        for (int i = 0; i < Parameters.Length; i++)
            Parameters[i].TypePath = Parameters[i].TypePath?.Resolve(resolver);
        foreach (var gp in GenericParameters)
            for (int i = 0; i < gp.TraitBounds.Count; i++)
                gp.TraitBounds[i] = gp.TraitBounds[i].Resolve(resolver);
    }
}

public class TraitDefinition : IItem, IDefinition, IAnalyzable, IPathResolvable
{
    public TraitDefinition(string name, NormalLangPath module,
        IEnumerable<TraitMethodSignature> methods, Token token,
        IEnumerable<GenericParameter> genericParameters,
        IEnumerable<TraitAssociatedType> associatedTypes,
        IEnumerable<LangPath>? supertraits = null)
    {
        Name = name;
        Module = module;
        MethodSignatures = methods.ToImmutableArray();
        Token = token;
        GenericParameters = genericParameters.ToImmutableArray();
        AssociatedTypes = associatedTypes.ToImmutableArray();
        Supertraits = supertraits?.ToImmutableArray() ?? [];
    }

    public ImmutableArray<TraitMethodSignature> MethodSignatures { get; }
    public ImmutableArray<GenericParameter> GenericParameters { get; }
    public ImmutableArray<TraitAssociatedType> AssociatedTypes { get; }
    public ImmutableArray<LangPath> Supertraits { get; set; }

    // IDefinition
    public string Name { get; }
    public LangPath TypePath => Module.Append(Name);
    public NormalLangPath Module { get; }
    public bool HasBeenGened { get; set; }

    // IItem
    public bool ImplementsLater => false;
    bool ISyntaxNode.NeedsSemiColonAfterIfNotLastInBlock => false;

    // ISyntaxNode
    public IEnumerable<ISyntaxNode> Children => [];
    public Token Token { get; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Validate lifetime annotations on trait method signatures
        foreach (var method in MethodSignatures)
        {
            // Check all argument lifetimes are declared
            foreach (var (_, lt) in method.ArgumentLifetimes)
            {
                if (!method.LifetimeParameters.Contains(lt))
                {
                    analyzer.AddException(new SemanticException(
                        $"Undeclared lifetime '{lt}' in parameter of trait method '{method.Name}'\n" +
                        method.Token.GetLocationStringRepresentation()));
                }
            }

            // Check return lifetime is declared
            if (method.ReturnLifetime != null && !method.LifetimeParameters.Contains(method.ReturnLifetime))
            {
                analyzer.AddException(new SemanticException(
                    $"Undeclared lifetime '{method.ReturnLifetime}' in return type of trait method '{method.Name}'\n" +
                    method.Token.GetLocationStringRepresentation()));
            }

            // Elision ambiguity check — same rules as FunctionDefinition
            if (method.ReturnTypePath is NormalLangPath nlpRet
                && nlpRet.Contains(RefTypeDefinition.GetRefModule()))
            {
                var refParamCount = method.Parameters.Count(p =>
                    p.TypePath is NormalLangPath nlpP && nlpP.Contains(RefTypeDefinition.GetRefModule()));
                var hasSelfRefParam = method.Parameters.Any(p =>
                    p.Name == "self"
                    && p.TypePath is NormalLangPath nlpS
                    && nlpS.Contains(RefTypeDefinition.GetRefModule()));
                bool hasExplicitLifetimes = method.ReturnLifetime != null;

                if (!hasExplicitLifetimes && refParamCount > 1 && !hasSelfRefParam)
                {
                    analyzer.AddException(new SemanticException(
                        $"Trait method '{method.Name}' returns a reference but has {refParamCount} reference parameters. " +
                        $"Cannot determine which input the output borrows from — explicit lifetime annotations are required\n" +
                        method.Token.GetLocationStringRepresentation()));
                }

                // If explicit return lifetime, check it appears on at least one param
                if (hasExplicitLifetimes)
                {
                    bool returnLifetimeOnParam = method.ArgumentLifetimes.Values.Any(lt => lt == method.ReturnLifetime);
                    if (!returnLifetimeOnParam)
                    {
                        analyzer.AddException(new SemanticException(
                            $"Return lifetime '{method.ReturnLifetime}' does not appear on any parameter in trait method '{method.Name}'\n" +
                            method.Token.GetLocationStringRepresentation()));
                    }
                }
            }
        }
    }

    public void ResolvePaths(PathResolver resolver)
    {
        foreach (var method in MethodSignatures)
            method.ResolvePaths(resolver);
        foreach (var at in AssociatedTypes)
            at.ResolvePaths(resolver);
        foreach (var gp in GenericParameters)
            for (int i = 0; i < gp.TraitBounds.Count; i++)
                gp.TraitBounds[i] = gp.TraitBounds[i].Resolve(resolver);
        // Resolve supertrait paths
        var resolved = new List<LangPath>();
        foreach (var st in Supertraits)
            resolved.Add(st.Resolve(resolver));
        Supertraits = resolved.ToImmutableArray();
    }

    public TraitMethodSignature? GetMethod(string name)
    {
        return MethodSignatures.FirstOrDefault(m => m.Name == name);
    }

    public TraitAssociatedType? GetAssociatedType(string name)
    {
        return AssociatedTypes.FirstOrDefault(a => a.Name == name);
    }

    public static TraitDefinition Parse(Parser parser, NormalLangPath module)
    {
        var traitToken = parser.Pop();
        if (traitToken is not TraitToken)
            throw new ExpectedParserException(parser, ParseType.Trait, traitToken);

        var nameToken = Identifier.Parse(parser);

        // Parse optional generic parameters — shared with FunctionDefinition
        var generics = FunctionSignatureParser.ParseGenericParams(parser);
        var genericParameters = generics?.GenericParameters.ToList() ?? new List<GenericParameter>();

        // Parse optional supertraits: trait Foo: Bar + Baz { ... }
        var supertraits = new List<LangPath>();
        if (parser.Peek() is ColonToken)
        {
            parser.Pop();
            supertraits.Add(LangPath.Parse(parser, true));
            while (parser.Peek() is OperatorToken { OperatorType: Operator.Add })
            {
                parser.Pop();
                supertraits.Add(LangPath.Parse(parser, true));
            }
        }

        CurlyBrace.ParseLeft(parser);

        var methods = new List<TraitMethodSignature>();
        var associatedTypes = new List<TraitAssociatedType>();
        while (parser.Peek() is not RightCurlyBraceToken)
        {
            if (parser.Peek() is TypeKeywordToken)
            {
                // Parse associated type: type Output; or type Output: Bound1 + Bound2;
                parser.Pop();
                var atName = Identifier.Parse(parser);
                var atBounds = new List<LangPath>();
                if (parser.Peek() is ColonToken)
                {
                    parser.Pop();
                    atBounds.Add(LangPath.Parse(parser, true));
                    while (parser.Peek() is OperatorToken { OperatorType: Operator.Add })
                    {
                        parser.Pop();
                        atBounds.Add(LangPath.Parse(parser, true));
                    }
                }
                SemiColon.Parse(parser);
                associatedTypes.Add(new TraitAssociatedType
                {
                    Name = atName.Identity,
                    Token = atName,
                    TraitBounds = atBounds
                });
            }
            else
            {
                methods.Add(TraitMethodSignature.Parse(parser));
            }
        }

        CurlyBrace.Parseight(parser);

        return new TraitDefinition(nameToken.Identity, module, methods, nameToken,
            genericParameters, associatedTypes, supertraits);
    }
}
