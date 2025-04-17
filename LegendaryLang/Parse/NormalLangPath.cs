using System.Collections.Immutable;
using System.Net.Mime;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public class NormalLangPath : LangPath
{
    public override void LoadAsShortCutIfPossible(SemanticAnalyzer analyzer)
    {
        
    }

    public override string ToString()
    {
        return string.Join("::", PathSegments);
    }

    public abstract class PathSegment
    {
        public static bool operator ==(PathSegment a, PathSegment b)
        {
            return object.Equals(a, b);
        }

        public static bool operator !=(PathSegment a, PathSegment b)
        {
            return !(a == b);
        }

        public abstract override string ToString();
        public static implicit operator string(PathSegment pathSegment) => pathSegment.ToString();
        public static implicit operator PathSegment(string str) => new NormalPathSegment(str);
        public static implicit operator PathSegment(ImmutableArray<LangPath> str) => new GenericTypesPathSegment(str);
        public abstract override bool Equals(object obj);
    }
    public class GenericTypesPathSegment : PathSegment
    {
        public GenericTypesPathSegment(IEnumerable<LangPath> typePaths)
        {
            TypePaths = typePaths.ToImmutableArray();
        }

        public override string ToString()
        {
            return $"<{string.Join(',', TypePaths)}>";
        }

        public ImmutableArray<LangPath> TypePaths { get; }
        public override bool Equals(object obj)
        {
          
            if (obj is GenericTypesPathSegment genericTypesPathSegment)
            {
                return TypePaths.SequenceEqual(genericTypesPathSegment.TypePaths);
            }

            return false;
        }
    }

    public NormalLangPath Append(params IEnumerable<PathSegment> pathSegment)
    {
        return new NormalLangPath(FirstIdentifierToken, [..PathSegments, ..pathSegment]);
    }
    public NormalLangPath(IdentifierToken? firstIdentifierToken, IEnumerable<PathSegment> path)
    {
        FirstIdentifierToken = firstIdentifierToken;
        PathSegments = path.ToImmutableArray();
    }

    public ImmutableArray<PathSegment> PathSegments { get; }
    public class NormalPathSegment : PathSegment
    {
        public readonly string Text;

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

        public NormalPathSegment(string text)
        {
            Text = text;
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
                    && second is  GenericTypesPathSegment secondGen && secondGen.TypePaths.Count() == 0)
                {
                    secondIndex++;
                    continue;
                }
                
                if (first != second)
                {
                    return false;
                }
                secondIndex++;
                firstIndex++;
            }

            if (firstIndex != PathSegments.Length || secondIndex != other.PathSegments.Length)
            {
                return false;
            }
            return true;
        }
        return false;
    }
}