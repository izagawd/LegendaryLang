using System.Collections;
using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public class NormalLangPath : LangPath, IEnumerable<NormalLangPath.PathSegment>
{
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
    ///     converts EG T to std::primitive::i32 if T is i32,
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
            if (i is GenericTypesPathSegment genericTypesPathSegment)
            {
                var typePaths = new List<LangPath>();
                foreach (var j in genericTypesPathSegment.TypePaths)
                    typePaths.Add((codeGen.GetRefItemFor(j) as TypeRefItem)?.Type.TypePath ?? j);
                newSegments.Add(new GenericTypesPathSegment(typePaths));
            }
            else
            {
                newSegments.Add(i);
            }

        return new NormalLangPath(FirstIdentifierToken, newSegments);
    }

    public override LangPath Resolve(PathResolver resolver)
    {
        IEnumerable<PathSegment>? toWorkWith =
            resolver.GetFullPathOfShortcut(PathSegments.First().ToString())?.PathSegments;
        if (toWorkWith is null)
            toWorkWith = PathSegments;
        else
            toWorkWith = toWorkWith.Concat(PathSegments.Skip(1));
        var newSegments = new List<PathSegment>();
        foreach (var i in toWorkWith)
            if (i is GenericTypesPathSegment genericTypesPathSegment)
            {
                var shortcutted = genericTypesPathSegment.TypePaths
                    .Select(j => j.Resolve(resolver));
                newSegments.Add(new GenericTypesPathSegment(shortcutted));
            }
            else
            {
                newSegments.Add(i);
            }
        

        return new NormalLangPath(FirstIdentifierToken, newSegments);
    }

    public PathSegment GetLastPathSegment()
    {
        return PathSegments.Last();
    }

    public override string ToString()
    {
        return string.Join("::", PathSegments);
    }

    public NormalLangPath Append(params IEnumerable<PathSegment> pathSegment)
    {
        return new NormalLangPath(FirstIdentifierToken, [..PathSegments, ..pathSegment]);
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
        if (PathSegments.LastOrDefault() is GenericTypesPathSegment genericTypesPathSegment)
            return genericTypesPathSegment.TypePaths;

        return [];
    }

    public NormalLangPath? PopGenerics()
    {
        if (PathSegments.LastOrDefault() is GenericTypesPathSegment) return Pop();

        return this;
    }

    public override bool Equals(object? obj)
    {
        if (obj is NormalLangPath other)
        {
            var firstIndex = 0;
            var secondIndex = 0;
            while (firstIndex < PathSegments.Length && secondIndex < other.PathSegments.Length)
            {
                var first = PathSegments[firstIndex];
                var second = other.PathSegments[secondIndex];
                if (first is GenericTypesPathSegment firstGen
                    && second is not GenericTypesPathSegment && firstGen.TypePaths.Count() == 0)
                {
                    firstIndex++;
                    continue;
                }

                if (first is not GenericTypesPathSegment
                    && second is GenericTypesPathSegment secondGen && secondGen.TypePaths.Count() == 0)
                {
                    secondIndex++;
                    continue;
                }

                if (first != second) return false;
                secondIndex++;
                firstIndex++;
            }

            if (firstIndex != PathSegments.Length || secondIndex != other.PathSegments.Length) return false;
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

        public static implicit operator PathSegment(ImmutableArray<LangPath> str)
        {
            return new GenericTypesPathSegment(str);
        }

        public abstract override bool Equals(object obj);
    }

    public class GenericTypesPathSegment : PathSegment
    {
        public GenericTypesPathSegment(IEnumerable<LangPath> typePaths)
        {
            TypePaths = typePaths.ToImmutableArray();
        }

        public ImmutableArray<LangPath> TypePaths { get; }

        public override string ToString()
        {
            return $"<{string.Join(',', TypePaths)}>";
        }

        public override bool Equals(object obj)
        {
            if (obj is GenericTypesPathSegment genericTypesPathSegment)
                return TypePaths.SequenceEqual(genericTypesPathSegment.TypePaths);

            return false;
        }
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

        public NormalPathSegment(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return Text;
        }

        public override bool Equals(object? obj)
        {
            return obj is NormalPathSegment segment && Text == segment.Text;
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