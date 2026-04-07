using System.Collections.Immutable;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
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
/// Represents a qualified associated type path like (i32 as Add(i32)).Output.
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
        return $"({ForType} as {TraitPath}).{AssociatedTypeName}";
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
    public static NormalLangPath PrimitivePath = new(null, ["Std", "Primitive"]);
    public static TupleLangPath VoidBaseLangPath { get; } = new([]);

    public IdentifierToken? FirstIdentifierToken { get; init; }

    /// <summary>
    /// Returns the path with trailing generic type arguments removed.
    /// If no generics are present, returns the path unchanged.
    /// E.g., Add&lt;i32&gt; → Add, Foo → Foo.
    /// </summary>
    public static LangPath StripGenerics(LangPath path)
        => path is NormalLangPath nlp && nlp.GetFrontGenerics().Length > 0
            ? nlp.PopGenerics()! : path;

    /// <summary>
    /// Splits a path into its base (without generics) and the generic arguments.
    /// Returns (basePath, genericArgs) where genericArgs is empty if none exist.
    /// E.g., Add&lt;i32&gt; → (Add, [i32]),  Foo → (Foo, []).
    /// </summary>
    public static (LangPath basePath, ImmutableArray<LangPath> genericArgs) SplitGenerics(LangPath path)
        => path is NormalLangPath nlp && nlp.GetFrontGenerics().Length > 0
            ? (nlp.PopGenerics()!, nlp.GetFrontGenerics())
            : (path, ImmutableArray<LangPath>.Empty);

    /// <summary>
    /// Set by Parse when a reference type with a lifetime annotation is parsed (&amp;'a T).
    /// Read and cleared by FunctionDefinition.Parse to capture lifetime annotations.
    /// </summary>
    public static string? LastParsedLifetime { get; set; }

    public static implicit operator LangPath(ImmutableArray<NormalLangPath.PathSegment> segments)
    {
        return new NormalLangPath(null, segments);
    }

    /// <summary>
    ///     MAKES THE COMPILER understand that the i32 is actually 'std.primitive.i32' if
    ///     use std.primitive.i32;
    ///     is declared. provide it with i32, and its should return std.primitive.i32
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
        return ToString().GetHashCode();
    }

    public static LangPath Parse(Parser parser, bool typePosition = false)
    {
        var next = parser.Peek();

        // Reference type: &T, &'a T, &'a const T, &mut T, &'a uniq T in type position
        if (next is AmpersandToken && typePosition)
        {
            parser.Pop(); // consume &

            // Check for optional lifetime annotation: &'a
            string? lifetime = null;
            if (parser.Peek() is LifetimeToken lt)
            {
                lifetime = lt.Name;
                parser.Pop();
            }
            LastParsedLifetime = lifetime;

            var refKind = RefKind.Shared;
            if (parser.Peek() is MutToken)
            {
                refKind = RefKind.Mut;
                parser.Pop();
            }
            else if (parser.Peek() is IdentifierToken { Identity: "const" })
            {
                refKind = RefKind.Const;
                parser.Pop();
            }
            else if (parser.Peek() is IdentifierToken { Identity: "uniq" })
            {
                refKind = RefKind.Uniq;
                parser.Pop();
            }
            var innerType = Parse(parser, true);
            var refModule = RefTypeDefinition.GetRefModule();
            var refName = RefTypeDefinition.GetRefName(refKind);
            return refModule.Append(refName)
                .AppendGenerics([innerType]);
        }

        // Double reference type: &&T, &&mut T, &&const T, &&uniq T in type position
        if (next is OperatorToken { OperatorType: Operator.And } && typePosition)
        {
            parser.Pop(); // consume &&

            // Second & may have a ref kind modifier
            var innerRefKind = RefKind.Shared;
            if (parser.Peek() is MutToken)
            {
                innerRefKind = RefKind.Mut;
                parser.Pop();
            }
            else if (parser.Peek() is IdentifierToken { Identity: "const" })
            {
                innerRefKind = RefKind.Const;
                parser.Pop();
            }
            else if (parser.Peek() is IdentifierToken { Identity: "uniq" })
            {
                innerRefKind = RefKind.Uniq;
                parser.Pop();
            }

            var innerType = Parse(parser, true);
            var refModule = RefTypeDefinition.GetRefModule();

            // Build inner ref: &kind T
            var innerRef = refModule.Append(RefTypeDefinition.GetRefName(innerRefKind))
                .AppendGenerics([innerType]);

            // Wrap in outer &shared
            return refModule.Append(RefTypeDefinition.GetRefName(RefKind.Shared))
                .AppendGenerics([innerRef]);
        }

        // Raw pointer type: *shared T, *const T, *mut T, *uniq T in type position
        if (next is OperatorToken { OperatorType: Operator.Multiply } && typePosition)
        {
            var peeked = parser.PeekAt(1);
            var isRawPtr = peeked is MutToken
                || (peeked is IdentifierToken { Identity: "const" or "uniq" or "shared" });

            if (isRawPtr)
            {
                parser.Pop(); // consume *

                var ptrKind = RefKind.Shared;
                if (parser.Peek() is MutToken)
                {
                    ptrKind = RefKind.Mut;
                    parser.Pop();
                }
                else if (parser.Peek() is IdentifierToken { Identity: "const" })
                {
                    ptrKind = RefKind.Const;
                    parser.Pop();
                }
                else if (parser.Peek() is IdentifierToken { Identity: "uniq" })
                {
                    ptrKind = RefKind.Uniq;
                    parser.Pop();
                }
                else if (parser.Peek() is IdentifierToken { Identity: "shared" })
                {
                    ptrKind = RefKind.Shared;
                    parser.Pop();
                }

                var innerType = Parse(parser, true);
                var ptrModule = RawPtrTypeDefinition.GetRawPtrModule();
                var ptrName = RawPtrTypeDefinition.GetRawPtrName(ptrKind);
                return ptrModule.Append(ptrName)
                    .AppendGenerics([innerType]);
            }
        }

        // Qualified associated type path: (Type as Trait).AssocType
        // Also handles nested: ((i32 as Add(i32)).Output as Add(i32)).Output
        if (next is LeftParenthesisToken)
        {
            Parenthesis.ParseLeft(parser);

            var firstType = Parse(parser, true);
            
            // Check for (Type as Trait).AssocType — qualified associated type path
            if (parser.Peek() is AsToken)
            {
                parser.Pop(); // consume 'as'
                var traitPath = Parse(parser, true);
                Parenthesis.ParseRight(parser);
                // Expect .AssocName
                if (parser.Peek() is DotToken)
                {
                    parser.Pop();
                    var assocName = Identifier.Parse(parser);
                    return new QualifiedAssocTypePath(firstType, traitPath, assocName.Identity, assocName);
                }
                // No dot — shouldn't happen in well-formed code
                throw new ExpectedParserException(parser, ParseType.Dot, parser.Peek());
            }

            var tuplePaths = new List<LangPath> { firstType };

            while (parser.Peek() is CommaToken)
            {
                parser.Pop();
                if (parser.Peek() is not RightParenthesisToken)
                    tuplePaths.Add(Parse(parser, true));
                else
                    break;
            }

            Parenthesis.ParseRight(parser);
            return new TupleLangPath(tuplePaths);
        }

        // Handle 'crate' keyword as first path segment
        IdentifierToken firstIdent;
        if (next is CrateToken crateToken)
        {
            parser.Pop();
            firstIdent = new IdentifierToken(crateToken.File, crateToken.Column, crateToken.Line, "crate");
        }
        else
        {
            firstIdent = Identifier.Parse(parser);
        }
        var segments = new List<NormalLangPath.PathSegment> { firstIdent.Identity };

        // Path separator: . (Carbon-like)
        while (parser.Peek() is DotToken)
        {
            parser.Pop();
            segments.Add(Identifier.Parse(parser).Identity);
        }

        // In type position, accept explicit generics via ()
        if (typePosition && parser.Peek() is LeftParenthesisToken)
        {
            Parenthesis.ParseLeft(parser);
            var arguments = new List<LangPath>();
            while (parser.Peek() is not RightParenthesisToken)
            {
                // Skip lifetime arguments — lifetimes are erased
                if (parser.Peek() is LifetimeToken)
                {
                    parser.Pop();
                    if (parser.Peek() is CommaToken) parser.Pop();
                    continue;
                }
                arguments.Add(Parse(parser, true));
                if (parser.Peek() is CommaToken)
                    parser.Pop();
                else
                    break;
            }
            Parenthesis.ParseRight(parser);
            if (arguments.Count > 0)
                { if (segments[^1] is NormalLangPath.NormalPathSegment lastSeg) segments[^1] = lastSeg.WithGenericArgs(arguments.ToImmutableArray()); }
        }

        // In type position, accept implicit args via [] (lifetimes, deduced params — all erased)
        if (typePosition && parser.Peek() is LeftBracketToken)
        {
            Bracket.ParseLeft(parser);
            while (parser.Peek() is not RightBracketToken)
            {
                // Lifetimes are erased
                if (parser.Peek() is LifetimeToken)
                {
                    parser.Pop();
                    if (parser.Peek() is CommaToken) parser.Pop();
                    continue;
                }
                // Skip any other implicit args
                parser.Pop();
                if (parser.Peek() is CommaToken) parser.Pop();
            }
            Bracket.ParseRight(parser);
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