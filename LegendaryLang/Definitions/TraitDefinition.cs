using System.Collections.Immutable;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
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

    public static TraitMethodSignature Parse(Parser parser)
    {
        var fnTok = parser.Pop();
        if (fnTok is not FnToken)
            throw new ExpectedParserException(parser, ParseType.Fn, fnTok);

        var nameToken = Identifier.Parse(parser);
        Parenthesis.ParseLeft(parser);

        var parameters = new List<VariableDefinition>();
        while (parser.Peek() is not RightParenthesisToken)
        {
            var param = VariableDefinition.Parse(parser);
            if (param.TypePath is null)
                throw new ExpectedParserException(parser, ParseType.BaseLangPath, param.IdentifierToken);
            parameters.Add(param);
            if (parser.Peek() is CommaToken) parser.Pop();
        }

        Parenthesis.ParseRight(parser);

        LangPath returnType = LangPath.VoidBaseLangPath;
        if (parser.Peek() is RightPointToken)
        {
            parser.Pop();
            returnType = LangPath.Parse(parser, true);
        }

        SemiColon.Parse(parser);

        return new TraitMethodSignature
        {
            Name = nameToken.Identity,
            Token = nameToken,
            Parameters = parameters.ToImmutableArray(),
            ReturnTypePath = returnType
        };
    }

    public void ResolvePaths(PathResolver resolver)
    {
        ReturnTypePath = ReturnTypePath.Resolve(resolver);
        for (int i = 0; i < Parameters.Length; i++)
            Parameters[i].TypePath = Parameters[i].TypePath?.Resolve(resolver);
    }
}

public class TraitDefinition : IItem, IDefinition, IAnalyzable, IPathResolvable
{
    public TraitDefinition(string name, NormalLangPath module,
        IEnumerable<TraitMethodSignature> methods, Token token,
        IEnumerable<GenericParameter> genericParameters,
        IEnumerable<TraitAssociatedType> associatedTypes)
    {
        Name = name;
        Module = module;
        MethodSignatures = methods.ToImmutableArray();
        Token = token;
        GenericParameters = genericParameters.ToImmutableArray();
        AssociatedTypes = associatedTypes.ToImmutableArray();
    }

    public ImmutableArray<TraitMethodSignature> MethodSignatures { get; }
    public ImmutableArray<GenericParameter> GenericParameters { get; }
    public ImmutableArray<TraitAssociatedType> AssociatedTypes { get; }

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

    public void Analyze(SemanticAnalyzer analyzer) { }

    public void ResolvePaths(PathResolver resolver)
    {
        foreach (var method in MethodSignatures)
            method.ResolvePaths(resolver);
        foreach (var at in AssociatedTypes)
            at.ResolvePaths(resolver);
        foreach (var gp in GenericParameters)
            for (int i = 0; i < gp.TraitBounds.Count; i++)
                gp.TraitBounds[i] = gp.TraitBounds[i].Resolve(resolver);
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

        // Parse optional generic parameters: trait Add<Rhs> { ... }
        var genericParameters = new List<GenericParameter>();
        if (parser.Peek() is OperatorToken { OperatorType: Operator.LessThan })
        {
            parser.Pop();
            var nextToken = parser.Peek();
            while (nextToken is not OperatorToken { OperatorType: Operator.GreaterThan })
            {
                var paramIdentifier = Identifier.Parse(parser);
                var traitBounds = new List<TraitBound>();
                if (parser.Peek() is ColonToken)
                {
                    parser.Pop();
                    if (parser.Peek() is not OperatorToken { OperatorType: Operator.GreaterThan }
                        && parser.Peek() is not CommaToken)
                    {
                        traitBounds.Add(TraitBound.Parse(parser));
                        while (parser.Peek() is OperatorToken { OperatorType: Operator.Add })
                        {
                            parser.Pop();
                            traitBounds.Add(TraitBound.Parse(parser));
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
            genericParameters, associatedTypes);
    }
}
