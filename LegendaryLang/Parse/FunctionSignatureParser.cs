using System.Collections.Immutable;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using Operator = LegendaryLang.Lex.Operator;

namespace LegendaryLang.Parse;

/// <summary>
/// Shared parsing logic for signatures of functions, traits, structs, enums, and impls.
/// </summary>
public static class FunctionSignatureParser
{
    /// <summary>
    /// Result of parsing implicit generic parameters in [] (lifetimes + comptime deduced params).
    /// </summary>
    public record GenericParamsResult(
        ImmutableArray<string> LifetimeParameters,
        ImmutableArray<GenericParameter> GenericParameters);

    /// <summary>
    /// Result of parsing parameters in (). Contains both comptime (:!) and runtime (:) params.
    /// CallParamLayout records the order: true = comptime, false = runtime.
    /// Semantic analysis validates whether runtime params are allowed in a given context.
    /// </summary>
    public record ParamsResult(
        ImmutableArray<VariableDefinition> Parameters,
        Dictionary<int, string> ArgumentLifetimes,
        ImmutableArray<GenericParameter> CheckedParams,
        ImmutableArray<bool> CallParamLayout);

    /// <summary>
    /// Result of parsing a return type (type path + optional lifetime annotation).
    /// </summary>
    public record ReturnTypeResult(LangPath ReturnTypePath, string? ReturnLifetime);

    /// <summary>
    /// Parse the bounds after :! Sized +— either 'type' (unconstrained) or TraitBound + TraitBound + ...
    /// Shared by generic params, associated type declarations, and impl associated types.
    /// </summary>
    public static List<TraitBound> ParseComptimeBounds(Parser parser)
    {
        if (parser.Peek() is TypeKeywordToken)
        {
            parser.Pop(); // 'type' = unconstrained placeholder
            return new List<TraitBound>();
        }
        var bounds = new List<TraitBound> { TraitBound.Parse(parser) };
        while (parser.Peek() is OperatorToken { OperatorType: Operator.Add })
        {
            parser.Pop();
            bounds.Add(TraitBound.Parse(parser));
        }
        return bounds;
    }

    /// <summary>
    /// Parse implicit/deduced generic parameters in bracket syntax: ['a, T:! Sized].
    /// Lifetimes must come first. Only :! Sized +(comptime) allowed in [].
    /// Returns null if next token is not [.
    /// </summary>
    public static GenericParamsResult? ParseImplicitGenericParams(Parser parser)
    {
        if (parser.Peek() is not LeftBracketToken)
            return null;

        parser.Pop(); // consume [

        var lifetimeParams = new List<string>();
        var genericParams = new List<GenericParameter>();

        // Parse lifetime parameters first: 'a, 'b, ...
        while (parser.Peek() is LifetimeToken ltParam)
        {
            lifetimeParams.Add(ltParam.Name);
            parser.Pop();
            if (parser.Peek() is CommaToken) parser.Pop();
            else break;
        }

        // Parse comptime parameters: T:! Sized, U:! Sized +Trait + Bar
        while (parser.Peek() is not RightBracketToken)
        {
            var paramIdent = Identifier.Parse(parser);

            if (parser.Peek() is not ColonBangToken)
            {
                throw new ParseException(
                    $"Expected ':!' for compile-time parameter in deduced brackets. " +
                    $"Use ':!' for comptime params, or '()' for runtime params.\n" +
                    $"{paramIdent.GetLocationStringRepresentation()}");
            }

            parser.Pop(); // consume :!
            var traitBounds = ParseComptimeBounds(parser);

            genericParams.Add(new GenericParameter(paramIdent, traitBounds));
            if (parser.Peek() is CommaToken) parser.Pop();
            else break;
        }

        Bracket.ParseRight(parser);

        return new GenericParamsResult(
            lifetimeParams.ToImmutableArray(),
            genericParams.ToImmutableArray());
    }

    /// <summary>
    /// Parse all generic params for a type definition (struct/enum/trait/impl).
    /// Calls ParseImplicitGenericParams for [], then ParseParams for ().
    /// Validates that all () params are comptime (:!) — runtime params are rejected.
    /// </summary>
    public static GenericParamsResult ParseGenericParams(Parser parser)
    {
        var implicit_ = ParseImplicitGenericParams(parser);
        var explicit_ = ParseParams(parser);

        // Validate: all () params must be comptime in a generic param context
        if (explicit_ != null && explicit_.Parameters.Length > 0)
        {
            var firstRuntime = explicit_.Parameters[0];
            throw new ParseException(
                $"Runtime parameters are not allowed in type/trait definitions. " +
                $"Use ':!' for compile-time parameters.\n" +
                $"{firstRuntime.IdentifierToken.GetLocationStringRepresentation()}");
        }

        var genericParams = new List<GenericParameter>();
        if (implicit_ != null) genericParams.AddRange(implicit_.GenericParameters);
        if (explicit_ != null) genericParams.AddRange(explicit_.CheckedParams);

        var lifetimes = new List<string>();
        if (implicit_?.LifetimeParameters.Length > 0) lifetimes.AddRange(implicit_.LifetimeParameters);

        return new GenericParamsResult(
            lifetimes.ToImmutableArray(),
            genericParams.ToImmutableArray());
    }

    /// <summary>
    /// Parse parameters in parentheses: (T:! Sized, x: i32, U:! Sized +Trait).
    /// Handles both comptime (:!) and runtime (:) params uniformly.
    /// Shared by functions, traits, structs, enums — semantic analysis
    /// validates whether runtime params are allowed in a given context.
    /// Returns null if next token is not (.
    /// </summary>
    public static ParamsResult? ParseParams(Parser parser)
    {
        if (parser.Peek() is not LeftParenthesisToken)
            return null;

        Parenthesis.ParseLeft(parser);

        var parameters = new List<VariableDefinition>();
        var argumentLifetimes = new Dictionary<int, string>();
        var checkedParams = new List<GenericParameter>();
        var layout = new List<bool>(); // true = comptime, false = runtime
        int argIndex = 0;

        while (parser.Peek() is not RightParenthesisToken)
        {
            // Check for comptime param: T:! Sized or T:! Sized +Trait
            if (parser.Peek() is IdentifierToken && parser.PeekAt(1) is ColonBangToken)
            {
                var paramIdent = Identifier.Parse(parser);
                parser.Pop(); // consume :!
                var traitBounds = ParseComptimeBounds(parser);
                checkedParams.Add(new GenericParameter(paramIdent, traitBounds));
                layout.Add(true);
                if (parser.Peek() is CommaToken) parser.Pop();
                continue;
            }

            var param = VariableDefinition.Parse(parser);
            if (param.TypePath is null)
                throw new ExpectedParserException(parser, ParseType.BaseLangPath, param.IdentifierToken);
            parameters.Add(param);
            layout.Add(false);

            if (param.Lifetime != null)
                argumentLifetimes[argIndex] = param.Lifetime;

            argIndex++;
            if (parser.Peek() is CommaToken) parser.Pop();
        }

        Parenthesis.ParseRight(parser);

        return new ParamsResult(
            parameters.ToImmutableArray(),
            argumentLifetimes,
            checkedParams.ToImmutableArray(),
            layout.ToImmutableArray());
    }

    /// <summary>
    /// Parse -> ReturnType with lifetime capture.
    /// Returns void type if no -> is present.
    /// </summary>
    public static ReturnTypeResult ParseReturnType(Parser parser)
    {
        if (parser.Peek() is not RightPointToken)
            return new ReturnTypeResult(LangPath.VoidBaseLangPath, null);

        LangPath.LastParsedLifetime = null;
        parser.Pop(); // consume ->
        var returnType = LangPath.Parse(parser, true);
        var returnLifetime = LangPath.LastParsedLifetime;
        LangPath.LastParsedLifetime = null;

        return new ReturnTypeResult(returnType, returnLifetime);
    }
}
