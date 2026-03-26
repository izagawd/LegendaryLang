using System.Collections.Immutable;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

/// <summary>
/// Represents a trait bound with optional associated type constraints.
/// E.g., Add&lt;T, Output = T&gt; has TraitPath = Add&lt;T&gt; and AssociatedTypeConstraints = {Output: T}.
/// </summary>
public class TraitBound
{
    public LangPath TraitPath { get; set; }
    public Dictionary<string, LangPath> AssociatedTypeConstraints { get; init; }

    public TraitBound(LangPath traitPath)
    {
        TraitPath = traitPath;
        AssociatedTypeConstraints = new Dictionary<string, LangPath>();
    }

    public TraitBound(LangPath traitPath, Dictionary<string, LangPath> constraints)
    {
        TraitPath = traitPath;
        AssociatedTypeConstraints = constraints;
    }

    public TraitBound Resolve(PathResolver resolver)
    {
        var newPath = TraitPath.Resolve(resolver);
        var newConstraints = new Dictionary<string, LangPath>();
        foreach (var (name, type) in AssociatedTypeConstraints)
            newConstraints[name] = type.Resolve(resolver);
        return new TraitBound(newPath, newConstraints);
    }

    public TraitBound SubstituteGenerics(ImmutableArray<GenericParameter> genericParams, ImmutableArray<LangPath> genericArgs)
    {
        var newPath = FieldAccessExpression.SubstituteGenerics(TraitPath, genericParams, genericArgs);
        var newConstraints = new Dictionary<string, LangPath>();
        foreach (var (name, type) in AssociatedTypeConstraints)
            newConstraints[name] = FieldAccessExpression.SubstituteGenerics(type, genericParams, genericArgs);
        return new TraitBound(newPath, newConstraints);
    }

    /// <summary>
    /// Parses a trait bound, handling both simple (Add&lt;T&gt;) and
    /// constrained (Add&lt;T, Output = T&gt;) syntax.
    /// </summary>
    public static TraitBound Parse(Parser parser)
    {
        // Parse the base trait path using LangPath.Parse but with awareness of associated type constraints
        // We need to handle Add<T, Output = T> where "Output = T" is NOT a type arg
        
        var firstIdent = Identifier.Parse(parser);
        var segments = new List<NormalLangPath.PathSegment> { (NormalLangPath.NormalPathSegment)firstIdent.Identity };
        
        // Handle :: paths like std::core::ops::Add
        while (parser.Peek() is DoubleColonToken)
        {
            parser.Pop();
            segments.Add((NormalLangPath.NormalPathSegment)Identifier.Parse(parser).Identity);
        }
        
        var constraints = new Dictionary<string, LangPath>();
        
        // Check for generic args with potential associated type constraints: <T, Output = T>
        if (parser.Peek() is OperatorToken { OperatorType: Operator.LessThan })
        {
            parser.Pop(); // consume '<'
            var typeArgs = new List<LangPath>();
            
            while (parser.Peek() is not OperatorToken { OperatorType: Operator.GreaterThan })
            {
                // Check for "Name = Type" pattern (associated type constraint)
                // This is a constraint if we see Identifier followed by '='
                if (parser.Peek() is IdentifierToken && parser.PeekAt(1) is EqualityToken)
                {
                    var name = Identifier.Parse(parser);
                    parser.Pop(); // consume '='
                    var type = LangPath.Parse(parser, true);
                    constraints[name.Identity] = type;
                }
                else
                {
                    typeArgs.Add(LangPath.Parse(parser, true));
                }
                
                if (parser.Peek() is CommaToken)
                    parser.Pop();
                else
                    break;
            }
            Comparator.ParseGreater(parser);
            
            if (typeArgs.Count > 0)
                segments.Add(new NormalLangPath.GenericTypesPathSegment(typeArgs));
        }
        
        var traitPath = new NormalLangPath(firstIdent, segments)
        {
            FirstIdentifierToken = firstIdent
        };
        return new TraitBound(traitPath, constraints);
    }
}

public class GenericParameter
{
    public readonly IdentifierToken? Identifier;
    public readonly string Name;
    public List<TraitBound> TraitBounds;

    public GenericParameter(string name)
    {
        Name = name;
        TraitBounds = new List<TraitBound>();
    }

    public GenericParameter(IdentifierToken identifier)
    {
        Identifier = identifier;
        Name = identifier.Identity;
        TraitBounds = new List<TraitBound>();
    }

    public GenericParameter(IdentifierToken identifier, IEnumerable<TraitBound> traitBounds)
    {
        Identifier = identifier;
        Name = identifier.Identity;
        TraitBounds = traitBounds.ToList();
    }
}
