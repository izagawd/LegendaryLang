using System.Collections;
using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public class NormalLangPath : LangPath, IEnumerable<NormalLangPath.PathSegment>
{
    public override bool IsMonomorphizedFrom(LangPath langPath)
    {
        if (PathSegments.Length == 0) return false;
        var last = GetLastPathSegment();
        if (last is NormalPathSegment { HasGenericArgs: true })
        {
            // Strip generics from last segment and compare
            var stripped = WithLastSegmentGenerics(null);
            return stripped == langPath;
        }
        return this == langPath;
    }

    public override ImmutableArray<LangPath> GetGenericArguments()
    {
        if (PathSegments.Length == 0) return [];
        var segment = GetLastPathSegment();
        if (segment is NormalPathSegment { HasGenericArgs: true } nps)
            return nps.GenericArgs!.Value;

        return [];
    }

    public NormalLangPath(IdentifierToken? firstIdentifierToken, IEnumerable<PathSegment> path)
    {
        FirstIdentifierToken = firstIdentifierToken;
        PathSegments = path.ToImmutableArray();
    }

    
    public ImmutableArray<PathSegment> PathSegments { get; }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<PathSegment> GetEnumerator()
    {
        return PathSegments.AsEnumerable().GetEnumerator();
    }

    /// <summary>
    ///     converts EG T to std.primitive.i32 if T is i32,
    ///     and (T, U) to (i32,bool) if U is bool
    /// </summary>
    /// <param name="codeGen"></param>
    /// <returns></returns>
    public override LangPath Monomorphize(CodeGenContext codeGen)
    {
        if (codeGen.HasIdent(this))
            return (NormalLangPath)(codeGen.GetRefItemFor(this, false) as TypeRefItem)?.Type.TypePath;

        var newSegments = new List<PathSegment>();
        foreach (var i in PathSegments)
            if (i is NormalPathSegment { HasGenericArgs: true } nps)
            {
                var typePaths = new List<LangPath>();
                foreach (var j in nps.GenericArgs!.Value)
                    typePaths.Add((codeGen.GetRefItemFor(j) as TypeRefItem)?.Type.TypePath ?? j);
                newSegments.Add(nps.WithGenericArgs(typePaths.ToImmutableArray()));
            }
            else
            {
                newSegments.Add(i);
            }

        return new NormalLangPath(FirstIdentifierToken, newSegments);
    }

    public override LangPath Resolve(PathResolver resolver)
    {
        var firstSeg = PathSegments.First();
        // Use just the text name for shortcut lookup, not the full ToString() which includes generics
        var lookupName = firstSeg is NormalPathSegment npsFirst ? npsFirst.Text : firstSeg.ToString();
        var resolved = resolver.GetFullPathOfShortcut(lookupName);
        
        IEnumerable<PathSegment> toWorkWith;
        if (resolved is null)
        {
            toWorkWith = PathSegments;
        }
        else
        {
            // If the first segment had generic args, transfer them to the last segment of the resolved path
            if (firstSeg is NormalPathSegment { HasGenericArgs: true } withGenerics)
            {
                var resolvedSegs = resolved.PathSegments.ToList();
                if (resolvedSegs.Count > 0 && resolvedSegs[^1] is NormalPathSegment lastResolved)
                    resolvedSegs[^1] = lastResolved.WithGenericArgs(withGenerics.GenericArgs);
                toWorkWith = resolvedSegs.Concat(PathSegments.Skip(1));
            }
            else
            {
                toWorkWith = resolved.PathSegments.Concat(PathSegments.Skip(1));
            }
        }
        var newSegments = new List<PathSegment>();
        foreach (var i in toWorkWith)
            if (i is NormalPathSegment { HasGenericArgs: true } nps)
            {
                var shortcutted = nps.GenericArgs!.Value
                    .Select(j => j.Resolve(resolver)).ToImmutableArray();
                newSegments.Add(nps.WithGenericArgs(shortcutted));
            }
            else
            {
                newSegments.Add(i);
            }
        

        return new NormalLangPath(FirstIdentifierToken, newSegments);
    }

    public PathSegment? GetLastPathSegment()
    {
        if (PathSegments.Length == 0) return null;
        return PathSegments.Last();
    }

    public override string ToString()
    {
        return string.Join(".", PathSegments);
    }

    public NormalLangPath Append(params IEnumerable<PathSegment> pathSegment)
    {
        return new NormalLangPath(FirstIdentifierToken, [..PathSegments, ..pathSegment]);
    }

    /// <summary>
    /// Returns a new path with generic args attached to the last NormalPathSegment.
    /// e.g. std.Box + [i32] -> std.Box(i32)
    /// </summary>
    public NormalLangPath AppendGenerics(IEnumerable<LangPath> genericArgs)
    {
        var args = genericArgs.ToImmutableArray();
        if (PathSegments.Length == 0)
            return this;
        var last = PathSegments.Last();
        if (last is NormalPathSegment nps)
        {
            var newLast = nps.WithGenericArgs(args);
            return new NormalLangPath(FirstIdentifierToken,
                [..PathSegments.SkipLast(1), newLast]);
        }
        return this;
    }

    /// <summary>
    /// Returns a new path with the last segment's generic args replaced (or cleared if null).
    /// </summary>
    public NormalLangPath WithLastSegmentGenerics(ImmutableArray<LangPath>? genericArgs)
    {
        if (PathSegments.Length == 0) return this;
        var last = PathSegments.Last();
        if (last is NormalPathSegment nps)
        {
            var newLast = nps.WithGenericArgs(genericArgs);
            return new NormalLangPath(FirstIdentifierToken,
                [..PathSegments.SkipLast(1), newLast]);
        }
        return this;
    }

    public bool Contains(NormalLangPath? langPath)
    {
        if (langPath is null) return false;
        if (PathSegments.Length < langPath.PathSegments.Length) return false;

        if (this == langPath) return true;
        for (var i = 0; i < langPath.PathSegments.Length; i++)
        {
            var pathSegment = langPath.PathSegments[i];
            var otherPathSegment = PathSegments[i];
            if (pathSegment != otherPathSegment) return false;
        }

        return true;
    }

    public NormalLangPath? Pop()
    {
        return new NormalLangPath(FirstIdentifierToken, PathSegments.SkipLast(1));
    }

    public ImmutableArray<LangPath> GetFrontGenerics()
    {
        if (PathSegments.LastOrDefault() is NormalPathSegment { HasGenericArgs: true } nps)
            return nps.GenericArgs!.Value;

        return [];
    }

    public NormalLangPath? PopGenerics()
    {
        if (PathSegments.LastOrDefault() is NormalPathSegment { HasGenericArgs: true })
            return WithLastSegmentGenerics(null);

        return this;
    }

    public override bool Equals(object? obj)
    {
        if (obj is NormalLangPath other)
        {
            if (PathSegments.Length != other.PathSegments.Length) return false;
            for (int i = 0; i < PathSegments.Length; i++)
            {
                if (PathSegments[i] != other.PathSegments[i]) return false;
            }
            return true;
        }

        return false;
    }


    public abstract class PathSegment
    {
        public static bool operator ==(PathSegment a, PathSegment b)
        {
            return Equals(a, b);
        }

        public static bool operator !=(PathSegment a, PathSegment b)
        {
            return !(a == b);
        }

        public abstract override string ToString();

        public static implicit operator string(PathSegment pathSegment)
        {
            return pathSegment.ToString();
        }

        public static implicit operator PathSegment(string str)
        {
            return new NormalPathSegment(str);
        }

        public abstract override bool Equals(object obj);
    }

    /// <summary>
    /// used for local definitions (eg local structs, local functions and so)
    /// </summary>
    public class UntypableSegment : PathSegment
    {
  

        public override string ToString()
        {
            return $"#UNTYPABLE";
        }
        
        public override bool Equals(object obj)
        {
            return (object) obj == this;
        }
    }
    public class NormalPathSegment : PathSegment
    {
        public readonly string Text;
        public readonly ImmutableArray<LangPath>? GenericArgs;

        public bool HasGenericArgs => GenericArgs is { Length: > 0 };

        public NormalPathSegment(string text, ImmutableArray<LangPath>? genericArgs = null)
        {
            Text = text;
            GenericArgs = genericArgs;
        }

        /// <summary>
        /// Returns a new NormalPathSegment with the same text but different generic args.
        /// </summary>
        public NormalPathSegment WithGenericArgs(ImmutableArray<LangPath>? genericArgs)
        {
            return new NormalPathSegment(Text, genericArgs);
        }

        public override string ToString()
        {
            if (HasGenericArgs)
                return $"{Text}({string.Join(", ", GenericArgs!.Value)})";
            return Text;
        }

        public override bool Equals(object? obj)
        {
            if (obj is NormalPathSegment segment)
            {
                if (Text != segment.Text) return false;
                // Both have no generics
                if (!HasGenericArgs && !segment.HasGenericArgs) return true;
                // One has generics, other doesn't — treat empty generics as equivalent to none
                if (HasGenericArgs != segment.HasGenericArgs)
                {
                    var one = HasGenericArgs ? GenericArgs!.Value : ImmutableArray<LangPath>.Empty;
                    var two = segment.HasGenericArgs ? segment.GenericArgs!.Value : ImmutableArray<LangPath>.Empty;
                    if (one.Length == 0 && two.Length == 0) return true;
                    return false;
                }
                // Both have generics
                return GenericArgs!.Value.SequenceEqual(segment.GenericArgs!.Value);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        // Implicit conversion from string to PathSegment
        public static implicit operator NormalPathSegment(string text)
        {
            return new NormalPathSegment(text);
        }

        // Optional: implicit conversion back to string
        public static implicit operator string(NormalPathSegment segment)
        {
            return segment.Text;
        }
    }
}
