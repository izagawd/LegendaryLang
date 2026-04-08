using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class PathExpression : IExpression
{
    public PathExpression(LangPath path)
    {
        Path = path;
    }

    public LangPath Path { get; set; }
    public bool IsTemporary => false;
    public IEnumerable<ISyntaxNode> Children => [];

    /// <summary>
    /// Set during Analyze if this path refers to an enum unit variant.
    /// </summary>
    public EnumTypeDefinition? EnumDef { get; set; }
    public EnumVariant? EnumVariant { get; set; }
    public LangPath? EnumTypePath { get; set; }

    /// <summary>
    ///     Generates LLVM IR to load the runtime value of the variable
    ///     referenced by the path.
    /// </summary>
    public ValueRefItem CodeGen(CodeGenContext context)
    {
        // Enum unit variant construction
        if (EnumDef != null && EnumVariant != null && EnumTypePath != null)
        {
            var typeRef = context.GetRefItemFor(EnumTypePath) as TypeRefItem;
            var enumType = typeRef?.Type as EnumType;
            if (enumType != null)
                return enumType.EmitUnitVariant(context, EnumVariant);
        }

        if (TypePath is null) TypePath = (context.GetRefItemFor(Path) as IHasType).Type.TypePath;

        var refItem = context.GetRefItemFor(Path) as ValueRefItem;
        var gotten = refItem.ValueRef;

        return new ValueRefItem
        {
            ValueRef = gotten,
            Type = refItem.Type
        };
    }

    public LangPath? TypePath { get; set; }

    public bool HasGuaranteedExplicitReturn => false;

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Check if this is an enum unit variant: EnumName.VariantName or EnumName(Generics).VariantName
        if (Path is NormalLangPath nlp && nlp.PathSegments.Length >= 2)
        {
            // Strip trailing generics if present
            var workingPath = nlp;
            ImmutableArray<LangPath> frontGenerics = [];
            if (workingPath.GetFrontGenerics().Length > 0)
            {
                frontGenerics = workingPath.GetFrontGenerics();
                workingPath = workingPath.PopGenerics()!;
            }

            if (workingPath.PathSegments.Length >= 2)
            {
                var parentPath = workingPath.Pop();
                var variantName = workingPath.GetLastPathSegment()?.ToString();
                if (parentPath != null && variantName != null)
                {
                    var def = analyzer.GetDefinition(parentPath);
                    if (def == null && parentPath is NormalLangPath nlpP && nlpP.GetFrontGenerics().Length > 0)
                        def = analyzer.GetDefinition(nlpP.PopGenerics());
                    if (def is EnumTypeDefinition enumDef)
                    {
                        var variant = enumDef.GetVariant(variantName);
                        if (variant != null)
                        {
                            if (variant.FieldTypes.Length > 0)
                            {
                                analyzer.AddException(new SemanticException(
                                    $"Variant '{variantName}' has fields — use '{variantName}(...)' syntax\n{Token.GetLocationStringRepresentation()}"));
                            }
                            EnumDef = enumDef;
                            EnumVariant = variant;

                            // Use front generics or parent path generics
                            ImmutableArray<LangPath> genericArgs = frontGenerics;
                            if (genericArgs.Length == 0 && parentPath is NormalLangPath nlpParent)
                                genericArgs = nlpParent.GetFrontGenerics();

                            if (genericArgs.Length > 0)
                            {
                                var basePath = (NormalLangPath)enumDef.TypePath;
                                EnumTypePath = basePath.AppendGenerics(genericArgs);
                            }
                            else
                            {
                                EnumTypePath = enumDef.TypePath;
                            }
                            TypePath = EnumTypePath;
                            return;
                        }
                    }
                }
            }
        }

        TypePath = analyzer.GetVariableTypePath(Path);
        if (TypePath is null)
        {
            analyzer.AddException(new UndefinedVariableException(
                Path, Token.GetLocationStringRepresentation()));
            return;
        }

        if (Path is NormalLangPath nlpMove && nlpMove.PathSegments.Length == 1)
        {
            var varName = nlpMove.PathSegments[0].ToString();
            analyzer.CheckVariableUsage(varName, Path, Token.GetLocationStringRepresentation());
        }
    }

    public Token Token => Path.FirstIdentifierToken;

    public void ResolvePaths(PathResolver resolver)
    {
        Path = Path.Resolve(resolver);
    }
}