using System.Collections.Immutable;
using LegendaryLang.Definitions;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public class EmptyPathException(LangPath paths) : ParseException
{
    public LangPath Path => paths;

    public override string Message => "Expected at least one string in the path. None were found";
}

public class TupleLangPath : LangPath
{
    public override bool IsMonomorphizedFrom(LangPath langPath)
    {
        return langPath is TupleLangPath tupleLangPath && tupleLangPath.TypePaths.Length == this.TypePaths.Length;
    }

    public override ImmutableArray<LangPath> GetGenericArguments()
    {
        return TypePaths;
    }

    public TupleLangPath(IEnumerable<LangPath> paths, IdentifierToken? firstIdentifierToken = null)
    {
        FirstIdentifierToken = firstIdentifierToken;
        TypePaths = paths.ToImmutableArray();
    }

    public ImmutableArray<LangPath> TypePaths { get; }

    public override string ToString()
    {
        return $"({string.Join(",", TypePaths)})";
    }

    public override LangPath Monomorphize(CodeGenContext codeGen)
    {
        return new TupleLangPath(TypePaths.Select(i => i.Monomorphize(codeGen)));
    }


    public override bool Equals(object? obj)
    {
        if (obj is TupleLangPath tupleLangPath) return TypePaths.SequenceEqual(tupleLangPath.TypePaths);
        return false;
    }

    
    public override LangPath Resolve(PathResolver resolver)
    {
        return new TupleLangPath(TypePaths.Select(i => i.Resolve(resolver)));
    }
}

/// <summary>
/// Represents a qualified associated type path like &lt;i32 as Add&lt;i32&gt;&gt;::Output.
/// Resolved to a concrete type during semantic analysis.
/// </summary>
public class QualifiedAssocTypePath : LangPath
{
    public LangPath ForType { get; set; }
    public LangPath TraitPath { get; set; }
    public string AssociatedTypeName { get; }

    public QualifiedAssocTypePath(LangPath forType, LangPath traitPath, string assocTypeName,
        IdentifierToken? firstIdentifierToken = null)
    {
        ForType = forType;
        TraitPath = traitPath;
        AssociatedTypeName = assocTypeName;
        FirstIdentifierToken = firstIdentifierToken;
    }

    public override bool IsMonomorphizedFrom(LangPath langPath) => false;
    public override ImmutableArray<LangPath> GetGenericArguments() => [];

    public override LangPath Resolve(PathResolver resolver)
    {
        return new QualifiedAssocTypePath(
            ForType.Resolve(resolver),
            TraitPath.Resolve(resolver),
            AssociatedTypeName,
            FirstIdentifierToken);
    }

    public override LangPath Monomorphize(CodeGenContext codeGen)
    {
        var resolvedFor = ForType.Monomorphize(codeGen);
        var resolvedTrait = TraitPath.Monomorphize(codeGen);
        // Try to resolve to concrete type via codegen context
        var forTypeRef = codeGen.GetRefItemFor(resolvedFor) as TypeRefItem;
        if (forTypeRef != null)
        {
            // Search impls for this type + trait with the associated type
            foreach (var impl in codeGen.ImplDefinitions)
            {
                var implTraitBase = impl.TraitPath;
                if (implTraitBase is NormalLangPath nlpIT && nlpIT.GetFrontGenerics().Length > 0)
                    implTraitBase = nlpIT.PopGenerics();
                var traitBase = resolvedTrait;
                if (traitBase is NormalLangPath nlpTB && nlpTB.GetFrontGenerics().Length > 0)
                    traitBase = nlpTB.PopGenerics();
                if (implTraitBase != traitBase) continue;
                var bindings = impl.TryMatchConcreteType(forTypeRef.Type.TypePath);
                if (bindings == null) continue;
                var at = impl.AssociatedTypeAssignments.FirstOrDefault(a => a.Name == AssociatedTypeName);
                if (at != null)
                {
                    var result = at.ConcreteType;
                    if (impl.GenericParameters.Length > 0)
                    {
                        var args = TypeInference.BuildGenericArgs(impl.GenericParameters, bindings);
                        if (args != null)
                            result = FieldAccessExpression.SubstituteGenerics(
                                result, impl.GenericParameters, args.Value);
                    }
                    return result.Monomorphize(codeGen);
                }
            }
        }
        return this;
    }

    public override bool Equals(object? obj)
    {
        if (obj is QualifiedAssocTypePath other)
            return ForType == other.ForType && TraitPath == other.TraitPath
                   && AssociatedTypeName == other.AssociatedTypeName;
        return false;
    }

    public override string ToString()
    {
        return $"<{ForType} as {TraitPath}>::{AssociatedTypeName}";
    }
}

/// <summary>
///     Used to represent a path. could be a path to a variable, function or type
/// Premonomorphized paths will not contain generic arguments
/// </summary>
public abstract class LangPath
{
    /// <summary>
    /// Useful when checking if a function/type was monomorphized from a definition
    /// </summary>
    /// <returns></returns>
    public abstract bool IsMonomorphizedFrom(LangPath definitionLangPath);
    public abstract ImmutableArray<LangPath> GetGenericArguments();
    public static NormalLangPath PrimitivePath = new(null, ["std", "primitive"]);
    public static TupleLangPath VoidBaseLangPath { get; } = new([]);

    public IdentifierToken? FirstIdentifierToken { get; init; }

    public static implicit operator LangPath(ImmutableArray<NormalLangPath.PathSegment> segments)
    {
        return new NormalLangPath(null, segments);
    }

    /// <summary>
    ///     MAKES THE COMPILER understand that the i32 is actually 'std::primitive::i32' if
    ///     use std::primitive::i32;
    ///     is declared. provide it with i32, and its should return std::primitive::i32
    /// </summary>
    public abstract LangPath Resolve(PathResolver resolver);

    public static bool operator ==(LangPath? path1, LangPath? path2)
    {
        if (path1 is null && path2 is null) return true;

        if (path1 is null) return false;
        return path1.Equals(path2);
    }

    public static bool operator !=(LangPath? path1, LangPath? path2)
    {
        return !(path1 == path2);
    }

    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }

    public static LangPath Parse(Parser parser, bool typePosition = false)
    {
        var next = parser.Peek();

        // Qualified associated type path: <Type as Trait>::AssocType
        // Also handles nested: <<i32 as Add<i32>>::Output as Add<i32>>::Output
        if (next is OperatorToken { OperatorType: Operator.LessThan } && typePosition)
        {
            // Lookahead: is this <Type as Trait>::Name or a generic arg list?
            // If the token after '<' is a type-starting token (identifier, '(', or another '<'),
            // AND eventually we see 'as', it's a qualified path.
            // We use a simple heuristic: parse speculatively.
            // Actually, we check if after parsing a type we see 'as'.
            // Since '<' in type position at the START (no preceding identifier) is unambiguous,
            // we can safely parse it as a qualified path.
            parser.Pop(); // consume '<'
            var forType = Parse(parser, true);
            if (parser.Peek() is AsToken)
            {
                parser.Pop(); // consume 'as'
                var traitPath = Parse(parser, true);
                Comparator.ParseGreater(parser);
                DoubleColon.Parse(parser);
                var assocName = Identifier.Parse(parser);
                return new QualifiedAssocTypePath(forType, traitPath, assocName.Identity, assocName);
            }
            else
            {
                // Not a qualified path — this shouldn't normally happen in well-formed code
                // since '<' at the start of a type position without a preceding identifier
                // only makes sense for qualified paths
                throw new ExpectedParserException(parser, ParseType.As, parser.Peek());
            }
        }

        if (next is LeftParenthesisToken)
        {
            Parenthesis.ParseLeft(parser);

            var tuplePaths = new List<LangPath>();

            while (parser.Peek() is not RightParenthesisToken)
            {
                tuplePaths.Add(Parse(parser, true));
                if (parser.Peek() is CommaToken)
                    parser.Pop();
                else
                    break;
            }

            Parenthesis.ParseRight(parser);
            return new TupleLangPath(tuplePaths);
        }

        var firstIdent = Identifier.Parse(parser);
        var segments = new List<NormalLangPath.PathSegment> { firstIdent.Identity };

        while (parser.Peek() is DoubleColonToken)
        {
            parser.Pop();
            if (parser.Peek() is OperatorToken{OperatorType: Operator.LessThan})
            {
                parser.Pop();
                var arguments = new List<LangPath>();
                while (parser.Peek() is not OperatorToken{OperatorType: Operator.GreaterThan})
                {
                    arguments.Add(Parse(parser, true));
                    if (parser.Peek() is CommaToken)
                        parser.Pop();
                    else
                        break;
                }

                Comparator.ParseGreater(parser);
                var genericsSegment = new NormalLangPath.GenericTypesPathSegment(arguments);
                segments.Add(genericsSegment);
            }
            else
            {
                segments.Add(Identifier.Parse(parser).Identity);
            }
        }

        // In type position, accept <T, U> directly without :: (like Rust's Wrapper<i32>)
        if (typePosition && parser.Peek() is OperatorToken{OperatorType: Operator.LessThan})
        {
            parser.Pop();
            var arguments = new List<LangPath>();
            while (parser.Peek() is not OperatorToken{OperatorType: Operator.GreaterThan})
            {
                arguments.Add(Parse(parser, true));
                if (parser.Peek() is CommaToken)
                    parser.Pop();
                else
                    break;
            }

            Comparator.ParseGreater(parser);
            segments.Add(new NormalLangPath.GenericTypesPathSegment(arguments));
        }

        return new NormalLangPath(firstIdent, segments.Select(i => i))
        {
            FirstIdentifierToken = firstIdent
        };
    }

    public abstract override bool Equals(object? obj);
    public abstract override string ToString();


    public abstract LangPath Monomorphize(CodeGenContext codeGen);
}