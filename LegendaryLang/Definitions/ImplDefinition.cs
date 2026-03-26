using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Definitions;

public class ImplAssociatedType
{
    public required string Name { get; init; }
    public required LangPath ConcreteType { get; set; }
    public required Token Token { get; init; }
}

public class ImplDefinition : IItem, IAnalyzable, IPathResolvable
{
    public ImplDefinition(LangPath traitPath, LangPath forTypePath,
        IEnumerable<FunctionDefinition> methods, Token token,
        IEnumerable<GenericParameter> genericParameters,
        IEnumerable<ImplAssociatedType> associatedTypes)
    {
        TraitPath = traitPath;
        ForTypePath = forTypePath;
        Methods = methods.ToImmutableArray();
        Token = token;
        GenericParameters = genericParameters.ToImmutableArray();
        AssociatedTypeAssignments = associatedTypes.ToImmutableArray();
    }

    public LangPath TraitPath { get; set; }
    public LangPath ForTypePath { get; set; }
    public ImmutableArray<FunctionDefinition> Methods { get; }
    public ImmutableArray<GenericParameter> GenericParameters { get; }
    public ImmutableArray<ImplAssociatedType> AssociatedTypeAssignments { get; }

    // IItem
    public bool ImplementsLater => false;
    bool ISyntaxNode.NeedsSemiColonAfterIfNotLastInBlock => false;

    // ISyntaxNode
    public IEnumerable<ISyntaxNode> Children => Methods;
    public Token Token { get; }

    /// <summary>
    /// Returns the set of impl generic param names for pattern matching.
    /// </summary>
    private HashSet<string> GetFreeVariableNames()
    {
        return GenericParameters.Select(gp => gp.Name).ToHashSet();
    }

    /// <summary>
    /// Tries to match a concrete type against this impl's ForTypePath pattern.
    /// Returns the bindings (generic_param_name → concrete_type) or null if no match.
    /// 
    /// For example, impl&lt;T&gt; Foo for Wrapper&lt;T&gt; matching against Wrapper&lt;i32&gt;
    /// returns {T → i32}.
    /// For non-generic impls, returns empty dict on exact match.
    /// </summary>
    public Dictionary<string, LangPath>? TryMatchConcreteType(LangPath concreteType)
    {
        var freeVars = GetFreeVariableNames();
        if (freeVars.Count == 0)
        {
            // Non-generic impl: exact match
            return ForTypePath == concreteType ? new Dictionary<string, LangPath>() : null;
        }

        var bindings = new Dictionary<string, LangPath>();
        if (TypeInference.TryUnify(ForTypePath, concreteType, freeVars, bindings))
        {
            // Verify all free vars are bound
            if (freeVars.All(v => bindings.ContainsKey(v)))
                return bindings;
        }
        return null;
    }

    /// <summary>
    /// Checks whether all generic parameter bounds are satisfied for the given bindings.
    /// </summary>
    public bool CheckBounds(Dictionary<string, LangPath> bindings, SemanticAnalyzer analyzer)
    {
        // Build generic args array for substitution
        var genericArgs = TypeInference.BuildGenericArgs(GenericParameters, bindings);

        foreach (var gp in GenericParameters)
        {
            if (!bindings.TryGetValue(gp.Name, out var boundType)) return false;
            foreach (var bound in gp.TraitBounds)
            {
                // Substitute generic params in the bound (e.g., Add<T> → Add<i32>)
                var resolvedBound = genericArgs != null
                    ? FieldAccessExpression.SubstituteGenerics(bound.TraitPath, GenericParameters, genericArgs.Value)
                    : bound.TraitPath;
                if (!analyzer.TypeImplementsTrait(boundType, resolvedBound))
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Checks whether all generic parameter bounds are satisfied for the given bindings (codegen version).
    /// </summary>
    public bool CheckBoundsCodeGen(Dictionary<string, LangPath> bindings, CodeGenContext context)
    {
        var genericArgs = TypeInference.BuildGenericArgs(GenericParameters, bindings);

        foreach (var gp in GenericParameters)
        {
            if (!bindings.TryGetValue(gp.Name, out var boundType)) return false;
            foreach (var bound in gp.TraitBounds)
            {
                // Substitute generic params in the bound (e.g., Add<T> → Add<i32>)
                var resolvedBound = genericArgs != null
                    ? FieldAccessExpression.SubstituteGenerics(bound.TraitPath, GenericParameters, genericArgs.Value)
                    : bound.TraitPath;

                // Check if boundType is a generic param with this trait bound in scope
                if (boundType is NormalLangPath nlpBound && nlpBound.PathSegments.Length == 1)
                {
                    bool foundInBounds = false;
                    foreach (var bounds in context.TraitBoundsStack)
                        foreach (var (tp, _) in bounds)
                            if (tp == resolvedBound)
                            { foundInBounds = true; break; }
                    if (foundInBounds) continue;
                }

                // Strip generics for base comparison
                var resolvedBoundBase = resolvedBound;
                ImmutableArray<LangPath> resolvedBoundGenericArgs = [];
                if (resolvedBound is NormalLangPath nlpResBound && nlpResBound.GetFrontGenerics().Length > 0)
                {
                    resolvedBoundGenericArgs = nlpResBound.GetFrontGenerics();
                    resolvedBoundBase = nlpResBound.PopGenerics();
                }

                if (!context.ImplDefinitions.Any(i =>
                {
                    var implTraitBase = i.TraitPath;
                    ImmutableArray<LangPath> implTraitGenericArgs = [];
                    if (i.TraitPath is NormalLangPath nlpIT && nlpIT.GetFrontGenerics().Length > 0)
                    {
                        implTraitGenericArgs = nlpIT.GetFrontGenerics();
                        implTraitBase = nlpIT.PopGenerics();
                    }

                    if (implTraitBase != resolvedBoundBase) return false;

                    var match = i.TryMatchConcreteType(boundType);
                    if (match == null) return false;

                    // Unify trait generic args
                    if (resolvedBoundGenericArgs.Length > 0 || implTraitGenericArgs.Length > 0)
                    {
                        if (resolvedBoundGenericArgs.Length != implTraitGenericArgs.Length) return false;
                        var freeVars = i.GenericParameters.Select(g => g.Name).ToHashSet();
                        for (int idx = 0; idx < resolvedBoundGenericArgs.Length; idx++)
                            if (!TypeInference.TryUnify(implTraitGenericArgs[idx], resolvedBoundGenericArgs[idx], freeVars, match))
                                return false;
                    }

                    return true;
                }))
                    return false;
            }
        }
        return true;
    }

    private static bool GenericParamUsedInType(string paramName, LangPath? typePath)
    {
        if (typePath is NormalLangPath nlp)
        {
            foreach (var seg in nlp.PathSegments)
            {
                if (seg is NormalLangPath.NormalPathSegment ns && ns.Text == paramName) return true;
                if (seg is NormalLangPath.GenericTypesPathSegment gts)
                    foreach (var tp in gts.TypePaths)
                        if (GenericParamUsedInType(paramName, tp)) return true;
            }
        }
        if (typePath is TupleLangPath tlp)
        {
            foreach (var tp in tlp.TypePaths)
                if (GenericParamUsedInType(paramName, tp)) return true;
        }
        return false;
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Validate that the trait exists — strip generics for lookup
        var traitLookupPath = TraitPath;
        if (traitLookupPath is NormalLangPath nlpTrait && nlpTrait.GetFrontGenerics().Length > 0)
            traitLookupPath = nlpTrait.PopGenerics();
        var traitDef = analyzer.GetDefinition(traitLookupPath) as TraitDefinition;
        if (traitDef == null)
        {
            analyzer.AddException(new TraitNotFoundException(TraitPath, Token.GetLocationStringRepresentation()));
            return;
        }

        // Check for unused impl generic parameters
        foreach (var gp in GenericParameters)
        {
            if (!GenericParamUsedInType(gp.Name, ForTypePath))
                analyzer.AddException(new SemanticException(
                    $"Generic parameter '{gp.Name}' is never used in the implementing type '{ForTypePath}'\n{Token.GetLocationStringRepresentation()}"));
        }

        // Validate that each trait method is implemented
        foreach (var traitMethod in traitDef.MethodSignatures)
        {
            var implMethod = Methods.FirstOrDefault(m => m.Name == traitMethod.Name);
            if (implMethod == null)
            {
                analyzer.AddException(new TraitMethodNotImplementedException(
                    traitMethod.Name, TraitPath, Token.GetLocationStringRepresentation()));
                continue;
            }

            if (implMethod.Arguments.Length != traitMethod.Parameters.Length)
            {
                analyzer.AddException(new SemanticException(
                    $"Method '{traitMethod.Name}' has {implMethod.Arguments.Length} parameters, " +
                    $"but the trait requires {traitMethod.Parameters.Length}\n{implMethod.Token.GetLocationStringRepresentation()}"));
            }

            // Validate generic parameter count matches
            if (implMethod.GenericParameters.Length != traitMethod.GenericParameters.Length)
            {
                analyzer.AddException(new TraitImplBoundsMismatchException(
                    $"Method '{traitMethod.Name}' has {implMethod.GenericParameters.Length} generic parameter(s), " +
                    $"but the trait requires {traitMethod.GenericParameters.Length}",
                    implMethod.Token.GetLocationStringRepresentation()));
            }
            else
            {
                // Validate that generic bounds match
                for (int i = 0; i < traitMethod.GenericParameters.Length; i++)
                {
                    var traitGp = traitMethod.GenericParameters[i];
                    var implGp = implMethod.GenericParameters[i];

                    // Impl must not add bounds that the trait didn't require
                    foreach (var implBound in implGp.TraitBounds)
                    {
                        if (!traitGp.TraitBounds.Any(tb => tb.TraitPath == implBound.TraitPath))
                        {
                            analyzer.AddException(new TraitImplBoundsMismatchException(
                                $"Method '{traitMethod.Name}': impl adds bound '{implBound.TraitPath}' on generic parameter '{implGp.Name}' " +
                                $"which is not present in the trait definition",
                                implMethod.Token.GetLocationStringRepresentation()));
                        }
                    }

                    // Trait bounds must be present in the impl
                    foreach (var traitBound in traitGp.TraitBounds)
                    {
                        if (!implGp.TraitBounds.Any(tb => tb.TraitPath == traitBound.TraitPath))
                        {
                            analyzer.AddException(new TraitImplBoundsMismatchException(
                                $"Method '{traitMethod.Name}': impl is missing bound '{traitBound.TraitPath}' on generic parameter '{implGp.Name}' " +
                                $"required by the trait definition",
                                implMethod.Token.GetLocationStringRepresentation()));
                        }
                    }
                }
            }
        }

        // Check for extra methods not in the trait
        foreach (var implMethod in Methods)
        {
            if (!traitDef.MethodSignatures.Any(m => m.Name == implMethod.Name))
            {
                analyzer.AddException(new TraitExtraMethodException(
                    implMethod.Name, TraitPath, implMethod.Token.GetLocationStringRepresentation()));
            }
        }

        // Analyze each method body — push impl generic bounds so T: Copy is known inside methods
        var implBounds = GenericParameters
            .SelectMany(gp => gp.TraitBounds.Select(tb => (tb.TraitPath, gp.Name, (Dictionary<string, LangPath>?)(tb.AssociatedTypeConstraints.Count > 0 ? tb.AssociatedTypeConstraints : null))))
            .ToList();
        analyzer.PushTraitBounds(implBounds);

        foreach (var method in Methods)
            method.Analyze(analyzer);

        analyzer.PopTraitBounds();

        // Validate associated types
        foreach (var traitAT in traitDef.AssociatedTypes)
        {
            var implAT = AssociatedTypeAssignments.FirstOrDefault(a => a.Name == traitAT.Name);
            if (implAT == null)
            {
                analyzer.AddException(new SemanticException(
                    $"Missing associated type '{traitAT.Name}' in impl of '{TraitPath}'\n{Token.GetLocationStringRepresentation()}"));
            }
            else
            {
                // Validate associated type bounds
                foreach (var bound in traitAT.TraitBounds)
                {
                    if (!analyzer.TypeImplementsTrait(implAT.ConcreteType, bound))
                    {
                        analyzer.AddException(new SemanticException(
                            $"Associated type '{traitAT.Name} = {implAT.ConcreteType}' does not satisfy bound '{bound}'\n{Token.GetLocationStringRepresentation()}"));
                    }
                }
            }
        }

        // Check for extra associated types not in the trait
        foreach (var implAT in AssociatedTypeAssignments)
        {
            if (!traitDef.AssociatedTypes.Any(a => a.Name == implAT.Name))
            {
                analyzer.AddException(new SemanticException(
                    $"Associated type '{implAT.Name}' is not defined in trait '{TraitPath}'\n{Token.GetLocationStringRepresentation()}"));
            }
        }

        // If implementing Copy, validate that all fields of the implementing type also implement Copy
        if (TraitPath == SemanticAnalyzer.CopyTraitPath)
        {
            var typeDef = analyzer.GetDefinition(ForTypePath);
            // Strip generics for definition lookup
            if (typeDef == null && ForTypePath is NormalLangPath nlpFor && nlpFor.GetFrontGenerics().Length > 0)
                typeDef = analyzer.GetDefinition(nlpFor.PopGenerics());

            if (typeDef is StructTypeDefinition structDef)
            {
                // Build a set of impl generic param names that have Copy bounds
                var copyBoundParams = GenericParameters
                    .Where(gp => gp.TraitBounds.Any(b => b.TraitPath == SemanticAnalyzer.CopyTraitPath))
                    .Select(gp => gp.Name)
                    .ToHashSet();

                foreach (var field in structDef.Fields)
                {
                    var fieldType = field.TypePath;

                    // Resolve qualified associated type paths (e.g., <i32 as Add<i32>>::Output → i32)
                    fieldType = analyzer.ResolveQualifiedTypePath(fieldType);

                    // If the field type is a generic param of the struct...
                    if (fieldType is NormalLangPath nlpField && nlpField.PathSegments.Length == 1)
                    {
                        var paramName = nlpField.PathSegments[0].ToString();

                        // Check if it's one of the impl's generic params
                        if (GenericParameters.Any(gp => gp.Name == paramName))
                        {
                            // It must have a Copy bound on this impl
                            if (!copyBoundParams.Contains(paramName))
                            {
                                analyzer.AddException(new SemanticException(
                                    $"Cannot implement Copy for '{ForTypePath}': field '{field.Name}' has type '{paramName}' " +
                                    $"which does not have a Copy bound\n{Token.GetLocationStringRepresentation()}"));
                            }
                            continue;
                        }

                        // Check if it's one of the struct's own generic params
                        if (structDef.GenericParameters.Any(gp => gp.Name == paramName))
                        {
                            // For non-generic impls like `impl Copy for Wrapper<i32>`,
                            // substitute the generic arg and check
                            if (ForTypePath is NormalLangPath nlpForType)
                            {
                                var genericArgs = nlpForType.GetFrontGenerics();
                                for (int i = 0; i < structDef.GenericParameters.Length && i < genericArgs.Length; i++)
                                {
                                    if (structDef.GenericParameters[i].Name == paramName)
                                    {
                                        fieldType = genericArgs[i];
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    // Check that the resolved field type implements Copy
                    if (!analyzer.IsTypeCopy(fieldType))
                    {
                        analyzer.AddException(new SemanticException(
                            $"Cannot implement Copy for '{ForTypePath}': field '{field.Name}' has type '{fieldType}' " +
                            $"which does not implement Copy\n{Token.GetLocationStringRepresentation()}"));
                    }
                }
            }

            // Enum Copy validation: all variant field types must implement Copy
            if (typeDef is EnumTypeDefinition enumDef)
            {
                var copyBoundParams = GenericParameters
                    .Where(gp => gp.TraitBounds.Any(b => b.TraitPath == SemanticAnalyzer.CopyTraitPath))
                    .Select(gp => gp.Name)
                    .ToHashSet();

                foreach (var variant in enumDef.Variants)
                {
                    foreach (var fieldType in variant.FieldTypes)
                    {
                        var resolved = analyzer.ResolveQualifiedTypePath(fieldType);

                        // Check generic params
                        if (resolved is NormalLangPath nlpField && nlpField.PathSegments.Length == 1)
                        {
                            var paramName = nlpField.PathSegments[0].ToString();
                            if (GenericParameters.Any(gp => gp.Name == paramName))
                            {
                                if (!copyBoundParams.Contains(paramName))
                                {
                                    analyzer.AddException(new SemanticException(
                                        $"Cannot implement Copy for '{ForTypePath}': variant '{variant.Name}' contains type '{paramName}' " +
                                        $"which does not have a Copy bound\n{Token.GetLocationStringRepresentation()}"));
                                }
                                continue;
                            }
                        }

                        if (!analyzer.IsTypeCopy(resolved))
                        {
                            analyzer.AddException(new SemanticException(
                                $"Cannot implement Copy for '{ForTypePath}': variant '{variant.Name}' contains type '{resolved}' " +
                                $"which does not implement Copy\n{Token.GetLocationStringRepresentation()}"));
                        }
                    }
                }
            }
        }
    }

    public void ResolvePaths(PathResolver resolver)
    {
        // Resolve the trait and type paths first
        TraitPath = TraitPath.Resolve(resolver);
        ForTypePath = ForTypePath.Resolve(resolver);

        // Resolve generic param trait bounds
        foreach (var gp in GenericParameters)
            for (int i = 0; i < gp.TraitBounds.Count; i++)
                gp.TraitBounds[i] = gp.TraitBounds[i].Resolve(resolver);

        // Resolve associated type concrete types
        foreach (var at in AssociatedTypeAssignments)
            at.ConcreteType = at.ConcreteType.Resolve(resolver);

        // Add a scope with Self mapped to the implementing type
        resolver.AddScope();
        if (ForTypePath is NormalLangPath nlp)
            resolver.AddToDeepestScope("Self", nlp);

        // Register associated type assignments as path shortcuts (Output → i32)
        foreach (var at in AssociatedTypeAssignments)
            if (at.ConcreteType is NormalLangPath nlpAt)
                resolver.AddToDeepestScope(at.Name, nlpAt);

        // Resolve paths in each method (Self in params/return types becomes the concrete type)
        foreach (var method in Methods)
            method.ResolvePaths(resolver);

        resolver.PopScope();
    }

    public FunctionDefinition? GetMethod(string methodName)
    {
        return Methods.FirstOrDefault(m => m.Name == methodName);
    }

    public static ImplDefinition Parse(Parser parser, NormalLangPath module)
    {
        var implToken = parser.Pop();
        if (implToken is not ImplToken)
            throw new ExpectedParserException(parser, ParseType.Impl, implToken);

        // Parse optional generic parameters: impl<T: Copy + Foo, U>
        var genericParameters = new List<GenericParameter>();
        if (parser.Peek() is OperatorToken { OperatorType: Operator.LessThan })
        {
            parser.Pop();
            var nextToken = parser.Peek();
            while (nextToken is not OperatorToken { OperatorType: Operator.GreaterThan })
            {
                var paramIdentifier = Identifier.Parse(parser);
                var traitBounds = new List<TraitBound>();
                if (parser.Peek() is ColonToken)
                {
                    parser.Pop();
                    if (parser.Peek() is not OperatorToken { OperatorType: Operator.GreaterThan }
                        && parser.Peek() is not CommaToken)
                    {
                        traitBounds.Add(TraitBound.Parse(parser));
                        while (parser.Peek() is OperatorToken { OperatorType: Operator.Add })
                        {
                            parser.Pop();
                            traitBounds.Add(TraitBound.Parse(parser));
                        }
                    }
                }
                nextToken = parser.Peek();
                genericParameters.Add(new GenericParameter(paramIdentifier, traitBounds));
                if (nextToken is CommaToken)
                {
                    parser.Pop();
                    nextToken = parser.Peek();
                }
                else
                {
                    break;
                }
            }
            Comparator.ParseGreater(parser);
        }

        var traitPath = LangPath.Parse(parser, true);

        var forTok = parser.Pop();
        if (forTok is not ForToken)
            throw new ExpectedParserException(parser, ParseType.For, forTok);

        var forTypePath = LangPath.Parse(parser, true);

        CurlyBrace.ParseLeft(parser);

        // Create a synthetic module for the impl methods
        var implModule = new NormalLangPath(null,
            [new NormalLangPath.NormalPathSegment($"impl_{traitPath}_for_{forTypePath}")]);

        var methods = new List<FunctionDefinition>();
        var associatedTypes = new List<ImplAssociatedType>();
        while (parser.Peek() is not RightCurlyBraceToken)
        {
            if (parser.Peek() is TypeKeywordToken)
            {
                // Parse: type Output = i32;
                var typeTok = parser.Pop();
                var atName = Identifier.Parse(parser);
                var eqTok = parser.Pop();
                if (eqTok is not EqualityToken)
                    throw new ExpectedParserException(parser, ParseType.Equality, eqTok);
                var concreteType = LangPath.Parse(parser, true);
                SemiColon.Parse(parser);
                associatedTypes.Add(new ImplAssociatedType
                {
                    Name = atName.Identity,
                    ConcreteType = concreteType,
                    Token = atName
                });
            }
            else
            {
                methods.Add(FunctionDefinition.Parse(parser, implModule));
            }
        }

        CurlyBrace.Parseight(parser);

        return new ImplDefinition(traitPath, forTypePath, methods, (Token)implToken, genericParameters, associatedTypes);
    }
}
