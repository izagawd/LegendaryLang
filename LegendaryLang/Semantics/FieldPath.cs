namespace LegendaryLang.Semantics;

/// <summary>
/// A path identifying a variable or a field within a variable, for move tracking.
/// ["x"] = the variable x.
/// ["x", "a"] = field a of variable x.
/// ["x", "a", "b"] = field b of field a of variable x.
/// Implements structural equality so it can be used as a dictionary key or in a HashSet.
/// </summary>
public sealed class FieldPath : IEquatable<FieldPath>
{
    public string[] Segments { get; }

    public FieldPath(params string[] segments)
    {
        Segments = segments;
    }

    /// <summary>The variable name (first segment).</summary>
    public string Root => Segments[0];

    /// <summary>Number of segments. 1 = variable, 2+ = field access.</summary>
    public int Depth => Segments.Length;

    /// <summary>Whether this is a simple variable (no field access).</summary>
    public bool IsVariable => Segments.Length == 1;

    /// <summary>Returns a new FieldPath with an additional segment appended.</summary>
    public FieldPath Append(string field)
    {
        var newSegments = new string[Segments.Length + 1];
        Segments.CopyTo(newSegments, 0);
        newSegments[^1] = field;
        return new FieldPath(newSegments);
    }

    /// <summary>Returns the parent path (all segments except the last). Null if already a root variable.</summary>
    public FieldPath? Parent()
    {
        if (Segments.Length <= 1) return null;
        var parentSegments = new string[Segments.Length - 1];
        Array.Copy(Segments, parentSegments, parentSegments.Length);
        return new FieldPath(parentSegments);
    }

    /// <summary>Whether this path is an ancestor of (or equal to) the other path.</summary>
    public bool IsAncestorOrEqual(FieldPath other)
    {
        if (Segments.Length > other.Segments.Length) return false;
        for (int i = 0; i < Segments.Length; i++)
            if (Segments[i] != other.Segments[i]) return false;
        return true;
    }

    /// <summary>Whether this path is a strict descendant of the other path.</summary>
    public bool IsDescendantOf(FieldPath other)
    {
        if (Segments.Length <= other.Segments.Length) return false;
        for (int i = 0; i < other.Segments.Length; i++)
            if (Segments[i] != other.Segments[i]) return false;
        return true;
    }

    public bool Equals(FieldPath? other)
    {
        if (other is null) return false;
        if (Segments.Length != other.Segments.Length) return false;
        for (int i = 0; i < Segments.Length; i++)
            if (Segments[i] != other.Segments[i]) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is FieldPath fp && Equals(fp);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var s in Segments) hash.Add(s);
        return hash.ToHashCode();
    }

    public override string ToString() => string.Join(".", Segments);

    public static bool operator ==(FieldPath? a, FieldPath? b) =>
        a is null ? b is null : a.Equals(b);
    public static bool operator !=(FieldPath? a, FieldPath? b) => !(a == b);
}
