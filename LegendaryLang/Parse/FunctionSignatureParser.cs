using System.Collections.Immutable;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using Operator = LegendaryLang.Lex.Operator;

namespace LegendaryLang.Parse;

/// <summary>
/// Shared parsing logic for function/method signatures.
/// Used by FunctionDefinition.Parse, TraitMethodSignature.Parse, and TraitDefinition.Parse
/// to avoid duplicating generic param, lifetime, parameter, and return type parsing.
/// </summary>
public static class FunctionSignatureParser
{
    /// <summary>
    /// Result of parsing generic parameters (lifetimes + type params with bounds).
    /// </summary>
    public record GenericParamsResult(
        ImmutableArray<string> LifetimeParameters,
        ImmutableArray<GenericParameter> GenericParameters);

    /// <summary>
    /// Result of parsing function parameters (variable defs + lifetime annotations).
    /// </summary>
    public record FunctionParamsResult(
        ImmutableArray<VariableDefinition> Parameters,
        Dictionary<int, string> ArgumentLifetimes);

    /// <summary>
    /// Result of parsing a return type (type path + optional lifetime annotation).
    /// </summary>
    public record ReturnTypeResult(LangPath ReturnTypePath, string? ReturnLifetime);

    /// <summary>
    /// Parse &lt;'a, 'b, T: Foo + Bar, U&gt; — lifetimes first, then type params with bounds.
    /// Assumes the opening &lt; has NOT been consumed yet.
    /// Returns null if next token is not &lt;.
    /// </summary>
    public static GenericParamsResult? ParseGenericParams(Parser parser)
    {
        if (parser.Peek() is not OperatorToken { OperatorType: Operator.LessThan })
            return null;

        parser.Pop(); // consume <

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

        // Parse type parameters: T, U: Foo + Bar, ...
        while (parser.Peek() is not OperatorToken { OperatorType: Operator.GreaterThan })
        {
            var paramIdent = Identifier.Parse(parser);
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
            genericParams.Add(new GenericParameter(paramIdent, traitBounds));
            if (parser.Peek() is CommaToken) parser.Pop();
            else break;
        }

        Comparator.ParseGreater(parser);

        return new GenericParamsResult(
            lifetimeParams.ToImmutableArray(),
            genericParams.ToImmutableArray());
    }

    /// <summary>
    /// Parse (param1: Type1, param2: Type2, ...) — function parameters with lifetime capture.
    /// Assumes opening ( has NOT been consumed yet.
    /// </summary>
    public static FunctionParamsResult ParseFunctionParams(Parser parser)
    {
        Parenthesis.ParseLeft(parser);

        var parameters = new List<VariableDefinition>();
        var argumentLifetimes = new Dictionary<int, string>();
        int argIndex = 0;

        while (parser.Peek() is not RightParenthesisToken)
        {
            LangPath.LastParsedLifetime = null;
            var param = VariableDefinition.Parse(parser);
            if (param.TypePath is null)
                throw new ExpectedParserException(parser, ParseType.BaseLangPath, param.IdentifierToken);
            parameters.Add(param);

            if (LangPath.LastParsedLifetime != null)
                argumentLifetimes[argIndex] = LangPath.LastParsedLifetime;
            LangPath.LastParsedLifetime = null;

            argIndex++;
            if (parser.Peek() is CommaToken) parser.Pop();
        }

        Parenthesis.ParseRight(parser);

        return new FunctionParamsResult(
            parameters.ToImmutableArray(),
            argumentLifetimes);
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
