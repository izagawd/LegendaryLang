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

    /// <summary>
    /// Optional default method body. If present, implementations may omit this method
    /// and the default body will be used instead.
    /// </summary>
    public BlockExpression? DefaultBody { get; init; }

    /// <summary>Whether this method has a default implementation.</summary>
    public bool HasDefault => DefaultBody != null;

    public static TraitMethodSignature Parse(Parser parser)
    {
        var fnTok = parser.Pop();
        if (fnTok is not FnToken)
            throw new ExpectedParserException(parser, ParseType.Fn, fnTok);

        var nameToken = Identifier.Parse(parser);

        // Parse implicit generic parameters: ['a, T:! type]
        var implicitGenerics = FunctionSignatureParser.ParseImplicitGenericParams(parser);
        var genericParameters = (implicitGenerics?.GenericParameters ?? []).ToList();
        var lifetimeParameters = implicitGenerics?.LifetimeParameters ?? [];

        // Parse parameters: (T:! type, x: i32) — () is required for methods
        var paramsResult = FunctionSignatureParser.ParseParams(parser);
        if (paramsResult is null)
            throw new ExpectedParserException(parser, ParseType.LeftParenthesis, parser.Peek());

        // Merge explicit comptime params from () into the generic params list
        if (paramsResult.CheckedParams.Length > 0)
            genericParameters.AddRange(paramsResult.CheckedParams);

        // Parse return type — shared with FunctionDefinition
        var returnResult = FunctionSignatureParser.ParseReturnType(parser);

        // Parse optional body (for default method implementations)
        BlockExpression? defaultBody = null;
        if (parser.Peek() is LeftCurlyBraceToken)
        {
            defaultBody = BlockExpression.Parse(parser, returnResult.ReturnTypePath);
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
            GenericParameters = genericParameters.ToImmutableArray(),
            LifetimeParameters = lifetimeParameters,
            ArgumentLifetimes = paramsResult.ArgumentLifetimes,
            ReturnLifetime = returnResult.ReturnLifetime,
            DefaultBody = defaultBody
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
        DefaultBody?.ResolvePaths(resolver);
    }
}

public class TraitDefinition : IItem, IDefinition, IAnalyzable, IPathResolvable
{
    public TraitDefinition(string name, NormalLangPath module,
        IEnumerable<TraitMethodSignature> methods, Token token,
        IEnumerable<GenericParameter> genericParameters,
        IEnumerable<TraitAssociatedType> associatedTypes,
        IEnumerable<TraitBound>? supertraits = null)
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
    public ImmutableArray<TraitBound> Supertraits { get; set; }

    // IDefinition
    public string Name { get; }
    public LangPath TypePath => Module.Append(Name);
    public NormalLangPath Module { get; }

    // IItem
    bool ISyntaxNode.NeedsSemiColonAfterIfNotLastInBlock => false;

    // ISyntaxNode
    public IEnumerable<ISyntaxNode> Children => [];
    public Token Token { get; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Validate that each supertrait references an existing trait definition
        foreach (var supertrait in Supertraits)
        {
            var lookupPath = LangPath.StripGenerics(supertrait.TraitPath);
            var superDef = analyzer.GetDefinition(lookupPath);
            if (superDef == null)
            {
                analyzer.AddException(new TraitNotFoundException(supertrait.TraitPath,
                    Token.GetLocationStringRepresentation()));
            }
            else if (superDef is not TraitDefinition superTraitDef)
            {
                analyzer.AddException(new SemanticException(
                    $"'{supertrait.TraitPath}' is not a trait and cannot be used as a supertrait\n{Token.GetLocationStringRepresentation()}"));
            }
            else if (superTraitDef.GenericParameters.Length > 0)
            {
                // Supertrait has generic params — verify the right number of args are provided
                var providedArgs = supertrait.TraitPath is NormalLangPath nlpSt ? nlpSt.GetFrontGenerics() : [];
                if (providedArgs.Length != superTraitDef.GenericParameters.Length)
                {
                    analyzer.AddException(new GenericParamCountException(
                        superTraitDef.GenericParameters.Length, providedArgs.Length,
                        Token.GetLocationStringRepresentation()));
                }
            }
        }

        // Validate lifetime annotations on trait method signatures
        foreach (var method in MethodSignatures)
        {
            analyzer.ValidateLifetimeAnnotations(method.Parameters, method.ArgumentLifetimes,
                method.ReturnLifetime, method.ReturnTypePath, method.LifetimeParameters,
                method.Name, method.Token.GetLocationStringRepresentation(), "trait method");

            // Push bounds for Self + trait generics + method generics
            // Used both for Sized param checks and default body analysis
            analyzer.AddScope();
            var traitBounds = new List<(LangPath traitPath, string typeName, Dictionary<string, LangPath>? assocConstraints)>();
            var selfTraitPath = GenericParameters.Length > 0
                ? ((NormalLangPath)TypePath).AppendGenerics(
                    GenericParameters.Select(gp => (LangPath)new NormalLangPath(null, [gp.Name])).ToArray())
                : TypePath;
            traitBounds.Add((selfTraitPath, "Self", null));
            // Self always implements MetaSized (it's universal — even unsized types have it)
            traitBounds.Add(((LangPath)SemanticAnalyzer.MetaSizedTraitPath, "Self", null));
            foreach (var gp in GenericParameters)
            {
                foreach (var tb in gp.TraitBounds)
                    traitBounds.Add((tb.TraitPath, gp.Name, tb.AssociatedTypeConstraints.Count > 0 ? tb.AssociatedTypeConstraints : null));
                bool hasMetaSized = gp.TraitBounds.Any(tb =>
                    LangPath.StripGenerics(tb.TraitPath).Equals(SemanticAnalyzer.MetaSizedTraitPath));
                if (!hasMetaSized)
                    traitBounds.Add(((LangPath)SemanticAnalyzer.SizedTraitPath, gp.Name, null));
            }
            foreach (var mgp in method.GenericParameters)
            {
                foreach (var tb in mgp.TraitBounds)
                    traitBounds.Add((tb.TraitPath, mgp.Name, tb.AssociatedTypeConstraints.Count > 0 ? tb.AssociatedTypeConstraints : null));
                bool hasMetaSized = mgp.TraitBounds.Any(tb =>
                    LangPath.StripGenerics(tb.TraitPath).Equals(SemanticAnalyzer.MetaSizedTraitPath));
                if (!hasMetaSized)
                    traitBounds.Add(((LangPath)SemanticAnalyzer.SizedTraitPath, mgp.Name, null));
            }
            // Associated type bounds (e.g., let Output :! type → Output: implicit Sized)
            foreach (var at in AssociatedTypes)
            {
                var qualifiedName = $"Self.{at.Name}";
                foreach (var atBound in at.TraitBounds)
                    traitBounds.Add((atBound, qualifiedName, null));
                bool hasMetaSized = at.TraitBounds.Any(tb =>
                    LangPath.StripGenerics(tb).Equals(SemanticAnalyzer.MetaSizedTraitPath));
                if (!hasMetaSized)
                    traitBounds.Add(((LangPath)SemanticAnalyzer.SizedTraitPath, qualifiedName, null));
            }
            analyzer.PushTraitBounds(traitBounds);

            // All parameters must be Sized (can't pass unsized types by value)
            analyzer.ValidateParamsSized(method.Parameters, method.Token.GetLocationStringRepresentation(), method.ReturnTypePath);

            // Analyze default method bodies
            if (method.HasDefault)
            {
                foreach (var param in method.Parameters)
                    analyzer.RegisterVariableType(
                        new NormalLangPath(param.IdentifierToken, [param.Name]), param.TypePath);

                analyzer.SetFunctionParameters(method.Parameters.Select(p => p.Name));

                method.DefaultBody!.Analyze(analyzer);
            }

            analyzer.PopTraitBounds();
            analyzer.PopScope();
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
        // Resolve supertrait paths (including associated type constraints)
        var resolved = new List<TraitBound>();
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

        var generics = FunctionSignatureParser.ParseGenericParams(parser);
        var genericParameters = generics.GenericParameters.ToList();

        // Parse optional supertraits: trait Foo: Bar + Baz<Output = i32> { ... }
        var supertraits = new List<TraitBound>();
        if (parser.Peek() is ColonToken)
        {
            parser.Pop();
            supertraits.Add(TraitBound.Parse(parser));
            while (parser.Peek() is OperatorToken { OperatorType: Operator.Add })
            {
                parser.Pop();
                supertraits.Add(TraitBound.Parse(parser));
            }
        }

        CurlyBrace.ParseLeft(parser);

        var methods = new List<TraitMethodSignature>();
        var associatedTypes = new List<TraitAssociatedType>();
        while (parser.Peek() is not RightCurlyBraceToken)
        {
            if (parser.Peek() is LetToken)
            {
                // Parse associated type:
                //   let Output :! type;          — unconstrained
                //   let Output :! Copy;          — single bound
                //   let Output :! Copy + Clone;  — multiple bounds
                parser.Pop(); // consume 'let'
                var atName = Identifier.Parse(parser);
                var colonBang = parser.Pop();
                if (colonBang is not ColonBangToken)
                    throw new ExpectedParserException(parser, ParseType.ColonBang, colonBang);
                var bounds = FunctionSignatureParser.ParseComptimeBounds(parser);
                SemiColon.Parse(parser);
                associatedTypes.Add(new TraitAssociatedType
                {
                    Name = atName.Identity,
                    Token = atName,
                    TraitBounds = bounds.Select(b => b.TraitPath).ToList()
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
