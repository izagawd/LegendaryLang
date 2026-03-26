using System.Collections.Immutable;
using LegendaryLang.Parse;

namespace LegendaryLang.Semantics;

/// <summary>
/// Shared type inference and unification utilities.
/// Used by struct construction, function calls, and impl pattern matching.
/// </summary>
public static class TypeInference
{
    /// <summary>
    /// Tries to unify a type pattern (which may contain free variables like T)
    /// with a concrete type, producing bindings for the free variables.
    /// </summary>
    public static bool TryUnify(LangPath pattern, LangPath concrete,
        HashSet<string> freeVars, Dictionary<string, LangPath> bindings)
    {
        // Pattern is a free variable → bind it
        if (pattern is NormalLangPath nlpPat && nlpPat.PathSegments.Length == 1
            && nlpPat.PathSegments[0] is NormalLangPath.NormalPathSegment ns
            && freeVars.Contains(ns.Text))
        {
            if (bindings.TryGetValue(ns.Text, out var existing))
                return existing == concrete; // Same var bound to different types → conflict
            bindings[ns.Text] = concrete;
            return true;
        }

        // Both NormalLangPaths — compare segment by segment
        if (pattern is NormalLangPath nlpPattern && concrete is NormalLangPath nlpConcrete)
        {
            var patSegs = nlpPattern.PathSegments.ToList();
            var conSegs = nlpConcrete.PathSegments.ToList();

            int pi = 0, ci = 0;
            while (pi < patSegs.Count && ci < conSegs.Count)
            {
                var ps = patSegs[pi];
                var cs = conSegs[ci];

                if (ps is NormalLangPath.GenericTypesPathSegment patGen
                    && cs is NormalLangPath.GenericTypesPathSegment conGen)
                {
                    if (patGen.TypePaths.Length != conGen.TypePaths.Length) return false;
                    for (int i = 0; i < patGen.TypePaths.Length; i++)
                        if (!TryUnify(patGen.TypePaths[i], conGen.TypePaths[i], freeVars, bindings))
                            return false;
                    pi++; ci++;
                }
                else if (ps is NormalLangPath.GenericTypesPathSegment pg && pg.TypePaths.Length == 0)
                { pi++; }
                else if (cs is NormalLangPath.GenericTypesPathSegment cg && cg.TypePaths.Length == 0)
                { ci++; }
                else
                {
                    if (ps != cs) return false;
                    pi++; ci++;
                }
            }
            while (pi < patSegs.Count && patSegs[pi] is NormalLangPath.GenericTypesPathSegment epg && epg.TypePaths.Length == 0) pi++;
            while (ci < conSegs.Count && conSegs[ci] is NormalLangPath.GenericTypesPathSegment ecg && ecg.TypePaths.Length == 0) ci++;
            return pi == patSegs.Count && ci == conSegs.Count;
        }

        if (pattern is TupleLangPath tlpPat && concrete is TupleLangPath tlpCon)
        {
            if (tlpPat.TypePaths.Length != tlpCon.TypePaths.Length) return false;
            for (int i = 0; i < tlpPat.TypePaths.Length; i++)
                if (!TryUnify(tlpPat.TypePaths[i], tlpCon.TypePaths[i], freeVars, bindings))
                    return false;
            return true;
        }

        return pattern == concrete;
    }

    /// <summary>
    /// Builds an ordered array of generic arguments from bindings and parameter definitions.
    /// Returns null if any parameter is unbound.
    /// </summary>
    public static ImmutableArray<LangPath>? BuildGenericArgs(
        ImmutableArray<GenericParameter> genericParams,
        Dictionary<string, LangPath> bindings)
    {
        var args = new LangPath[genericParams.Length];
        for (int i = 0; i < genericParams.Length; i++)
        {
            if (!bindings.TryGetValue(genericParams[i].Name, out var bound))
                return null;
            args[i] = bound;
        }
        return args.ToImmutableArray();
    }

    /// <summary>
    /// Tries to infer all generic args by unifying parallel lists of patterns and concrete types.
    /// Returns the ordered generic args, or null if inference fails.
    /// </summary>
    public static ImmutableArray<LangPath>? InferFromConstraints(
        ImmutableArray<GenericParameter> genericParams,
        IEnumerable<(LangPath pattern, LangPath concrete)> constraints)
    {
        var freeVars = genericParams.Select(gp => gp.Name).ToHashSet();
        var bindings = new Dictionary<string, LangPath>();

        foreach (var (pattern, concrete) in constraints)
            if (!TryUnify(pattern, concrete, freeVars, bindings))
                return null;

        return BuildGenericArgs(genericParams, bindings);
    }
}
