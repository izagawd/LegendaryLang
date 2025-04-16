using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;


public class EmptyPathException(LangPath paths) : ParseException
{
    public LangPath Path => paths;

    public override string Message => $"Expected at least one string in the path. None were found";
}




public class TupleLangPath : LangPath
{
    public override string ToString()
    {
        return $"({string.Join(",", TypePaths)})";
    }

    public ImmutableArray<LangPath> TypePaths { get; }

    public override bool Equals(object? obj)
    {
        if (obj is TupleLangPath tupleLangPath)
        {
            return TypePaths.SequenceEqual(tupleLangPath.TypePaths);
        }
        return false;
    }

    public TupleLangPath(IEnumerable<LangPath> paths)
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

    public override bool Equals(object? obj)
    {
        return obj is PathSegment segment && Text == segment.Text;
    }

    public override int GetHashCode()
    {
        return Text.GetHashCode();
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
public class NormalLangPath: LangPath
{
    public ImmutableArray<PathSegment> Path { get; private set; }

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

    public override void LoadAsShortCutIfPossible(SemanticAnalyzer analyzer)
    {

        var full = analyzer.GetFullPathOfShortcut(Path.First());
        if (full is not null)
        {
            foreach (var i in Path.Skip(1))
            {
                full = full.Append(i);
            }

            this.Path = full.Path.ToImmutableArray();
        }

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
public abstract class LangPath
{
    /// <summary>
    /// MAKES THE COMPILER understand that the i32 is actually 'std::primitive::i32' if the using is declared
    /// </summary>
    public virtual void LoadAsShortCutIfPossible(SemanticAnalyzer analyzer){}
    public static NormalLangPath PrimitivePath = new NormalLangPath(null,["std", "primitive"]);
    public static TupleLangPath VoidBaseLangPath { get; } = new TupleLangPath([]);
    public static bool operator ==(LangPath path1, LangPath path2)
    {
        if (path1 is null && path2 is null)
        {
            return true;
        }

        if (path1 is null)
        {
            return false;
        }
        return path1.Equals(path2);
    }

    public static bool operator !=(LangPath path1, LangPath path2)
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