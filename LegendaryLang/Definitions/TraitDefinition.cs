using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;

namespace LegendaryLang.Definitions;

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
        // Resolve types in method signature (Self will remain as "Self" since it's
        // not registered in the PathResolver for trait definitions)
        ReturnTypePath = ReturnTypePath.Resolve(resolver);
        for (int i = 0; i < Parameters.Length; i++)
            Parameters[i].TypePath = Parameters[i].TypePath?.Resolve(resolver);
    }
}

public class TraitDefinition : IItem, IDefinition, IAnalyzable, IPathResolvable
{
    public TraitDefinition(string name, NormalLangPath module,
        IEnumerable<TraitMethodSignature> methods, Token token)
    {
        Name = name;
        Module = module;
        MethodSignatures = methods.ToImmutableArray();
        Token = token;
    }

    public ImmutableArray<TraitMethodSignature> MethodSignatures { get; }

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
    }

    public TraitMethodSignature? GetMethod(string name)
    {
        return MethodSignatures.FirstOrDefault(m => m.Name == name);
    }

    public static TraitDefinition Parse(Parser parser, NormalLangPath module)
    {
        var traitToken = parser.Pop();
        if (traitToken is not TraitToken)
            throw new ExpectedParserException(parser, ParseType.Trait, traitToken);

        var nameToken = Identifier.Parse(parser);
        CurlyBrace.ParseLeft(parser);

        var methods = new List<TraitMethodSignature>();
        while (parser.Peek() is not RightCurlyBraceToken)
        {
            methods.Add(TraitMethodSignature.Parse(parser));
        }

        CurlyBrace.Parseight(parser);

        return new TraitDefinition(nameToken.Identity, module, methods, nameToken);
    }
}
