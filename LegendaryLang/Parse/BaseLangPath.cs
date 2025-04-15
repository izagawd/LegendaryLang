using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Parse;


public class EmptyPathException(BaseLangPath paths) : ParseException
{
    public BaseLangPath Path => paths;

    public override string Message => $"Expected at least one string in the path. None were found";
}




public class TupleLangPath : BaseLangPath
{
    public override string ToString()
    {
        return $"({string.Join(",", TypePaths)})";
    }

    public ImmutableArray<BaseLangPath> TypePaths { get; }

    public override bool Equals(object? obj)
    {
        if (obj is TupleLangPath tupleLangPath)
        {
            return TypePaths.SequenceEqual(tupleLangPath.TypePaths);
        }
        return false;
    }

    public TupleLangPath(IEnumerable<BaseLangPath> paths)
    {
        TypePaths = paths.ToImmutableArray();
    }
}

public struct PathSegment
{
    public string Text;

    public override string ToString()
    {
        return Text;
    }

    public PathSegment(string text)
    {
        Text = text;
    }
    // Implicit conversion from string to PathSegment
    public static implicit operator PathSegment(string text)
    {
        return new PathSegment(text);
    }

    // Optional: implicit conversion back to string
    public static implicit operator string(PathSegment segment)
    {
        return segment.Text;
    }
}
public class NormalLangPath: BaseLangPath
{
    public ImmutableArray<PathSegment> Path { get; }

    public NormalLangPath Append(params PathSegment[] paths)
    {
        var toList = Path.ToList();
        toList.AddRange(paths);
        return new NormalLangPath(FirstIdentifierToken, toList);
    }

    public override string ToString()
    {
        return string.Join("::", Path);
    }




    public NormalLangPath(IdentifierToken? firstIdentToken, IEnumerable<PathSegment> ident)
    {
        Path = ident.ToImmutableArray();
        FirstIdentifierToken = firstIdentToken;
    }

    public override bool Equals(object? obj)
    {
        if (obj is NormalLangPath path)
        {
            return Path.SequenceEqual(path.Path);
        }
        return false;
    }
}
public abstract class BaseLangPath
{
    public static NormalLangPath PrimitivePath = new NormalLangPath(null,["std", "primitive"]);
    public static TupleLangPath VoidBaseLangPath { get; } = new TupleLangPath([]);
    public static bool operator ==(BaseLangPath path1, BaseLangPath path2)
    {
        return path1.Equals(path2);
    }

    public static bool operator !=(BaseLangPath path1, BaseLangPath path2)
    {
        return !(path1 == path2);
    }

    public IdentifierToken? FirstIdentifierToken { get; init; }
    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }

    public static  NormalLangPath  Parse(Parser parser)
    {
        var firstIdent = Identifier.Parse(parser);
        var idents = new List<IdentifierToken>() { firstIdent };
        while (parser.Peek() is DoubleColonToken)
        {
            parser.Pop();
            idents.Add(Identifier.Parse(parser));
        }

        return new NormalLangPath( firstIdent ,idents.Select(i =>(PathSegment) i.Identity))
        {
            FirstIdentifierToken = firstIdent
        };
    }
    public  override abstract bool Equals(object? obj);

}