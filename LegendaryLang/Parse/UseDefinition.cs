using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;

public class UseDefinition : IItem, IAnalyzable
{


    public UseDefinition(NormalLangPath pathToUse, Token token)
    {
        PathToUse = pathToUse;
        Token = token;
    }

    public NormalLangPath PathToUse { get; }

    /// <summary>
    /// The fully resolved version of PathToUse (after pkg expansion etc.).
    /// Set during RegisterUsings when a PathResolver is available.
    /// </summary>
    public NormalLangPath ResolvedPathToUse { get; private set; }


    public Token Token { get; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        var path = ResolvedPathToUse ?? PathToUse;

        // Register trait imports — if the target is a trait, make it available for method dispatch
        var def = analyzer.GetDefinition(path);
        if (def is TraitDefinition)
            analyzer.ImportTrait(path);

        // Skip validation for auto-generated UseDefinitions (no token = internal registration).
        if (Token == null) return;

        // Check 1: Direct definition match (full path)
        if (def != null) return;

        // Check 2: Enum variant — parent is an enum, last segment is a variant
        if (path.PathSegments.Length >= 2)
        {
            var parentPath = path.Pop();
            if (parentPath != null)
            {
                var parentDef = analyzer.GetDefinition(parentPath);
                if (parentDef is EnumTypeDefinition enumDef)
                {
                    var variantName = path.GetLastPathSegment()?.ToString();
                    if (variantName != null && enumDef.GetVariant(variantName) != null)
                        return; // Valid enum variant import
                }
            }
        }

        // Check 3: Module import — path is a prefix of some registered definition
        if (analyzer.IsModulePath(path))
            return;

        analyzer.AddException(new SemanticException(
            $"Cannot find '{PathToUse}' — the imported path does not exist\n" +
            Token.GetLocationStringRepresentation()));
    }

    public IEnumerable<ISyntaxNode> Children => [];


    Token ISyntaxNode.Token => Token;

    public static UseDefinition Parse(Parser parser)
    {
        var usin = parser.Pop();
        if (usin is not UseToken useToken) throw new ExpectedParserException(parser, [ParseType.Use], usin);
        var path = NormalLangPath.Parse(parser);
        if (path is not NormalLangPath normalPath) throw new Exception("d");

        if (normalPath.PathSegments.Any(i => i is NormalLangPath.NormalPathSegment { HasGenericArgs: true })) throw new Exception("d");

        SemiColon.Parse(parser);
        return new UseDefinition(normalPath, useToken);
    }

    public void RegisterUsings(PathResolver resolver)
    {
        ResolvedPathToUse = (NormalLangPath)PathToUse.Resolve(resolver);
        resolver.AddToDeepestScope(ResolvedPathToUse.PathSegments.Last(), ResolvedPathToUse);
    }

}