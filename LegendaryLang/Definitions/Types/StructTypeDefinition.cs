using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions.Types;

public class StructTypeDefinition : ComposableTypeDefinition
{
    public StructTypeDefinition(string name, NormalLangPath module, StructToken token,
        IEnumerable<VariableDefinition> fields, IEnumerable<GenericParameter> genericParameters,
        IEnumerable<string>? lifetimeParameters = null)
    {
        StructToken = token;
        Name = name;
        Module = module;
        Fields = fields.ToImmutableArray();
        GenericParameters = genericParameters.ToImmutableArray();
        LifetimeParameters = lifetimeParameters?.ToImmutableArray() ?? [];
    }

    public override Token Token => StructToken;

    public StructToken StructToken { get; }
    public ImmutableArray<VariableDefinition> Fields { get; protected set; }

    public override string Name { get; }
    public override NormalLangPath Module { get; }

    public override ImmutableArray<LangPath> ComposedTypes => Fields.Select(i => i.TypePath).ToImmutableArray();

    public ImmutableArray<GenericParameter> GenericParameters { get; }
    public override ImmutableArray<string> LifetimeParameters { get; }

    public override void ResolvePaths(PathResolver resolver)
    {
        var list = new List<VariableDefinition>();
        foreach (var i in Fields)
            list.Add(new VariableDefinition(i.IdentifierToken, i.TypePath.Resolve(resolver), i.Lifetime));

        Fields = list.ToImmutableArray();

        // Resolve trait bounds on generic params
        foreach (var gp in GenericParameters)
            for (int i = 0; i < gp.TraitBounds.Count; i++)
                gp.TraitBounds[i] = gp.TraitBounds[i].Resolve(resolver);
    }

    public override void Analyze(SemanticAnalyzer analyzer)
    {
        // Check for duplicate generic parameter names
        var seen = new HashSet<string>();
        foreach (var gp in GenericParameters)
        {
            if (!seen.Add(gp.Name))
                analyzer.AddException(new SemanticException(
                    $"Duplicate generic parameter name '{gp.Name}'\n{Token.GetLocationStringRepresentation()}"));
        }

        // Check that every generic parameter is used in at least one field type
        foreach (var gp in GenericParameters)
        {
            var used = Fields.Any(f => GenericParamUsedInType(gp.Name, f.TypePath));
            if (!used)
                analyzer.AddException(new SemanticException(
                    $"Generic parameter '{gp.Name}' is never used in struct '{Name}'\n{Token.GetLocationStringRepresentation()}"));
        }

        // Lifetime validation: reference fields require lifetime parameters
        ValidateFieldLifetimes(Fields.Select(f => f.TypePath), Fields.Select(f => f.Lifetime),
            LifetimeParameters, Name, Token, analyzer);
    }

    /// <summary>
    /// Validates lifetime usage in struct/enum fields.
    /// Shared by StructTypeDefinition and EnumTypeDefinition — no duplication.
    /// </summary>
    internal static void ValidateFieldLifetimes(IEnumerable<LangPath?> fieldTypes,
        IEnumerable<string?> fieldLifetimes, ImmutableArray<string> lifetimeParams,
        string typeName, Token token, SemanticAnalyzer analyzer)
    {
        var fieldList = fieldTypes.ToList();
        var fieldLtList = fieldLifetimes.ToList();

        // Check if any field is a non-static reference
        bool hasRefField = false;
        for (int i = 0; i < fieldList.Count; i++)
        {
            if (fieldList[i] is NormalLangPath nlp && nlp.Contains(RefTypeDefinition.GetRefModule())
                && (i >= fieldLtList.Count || fieldLtList[i] != "static"))
            {
                hasRefField = true;
                break;
            }
        }
        bool hasLifetimeDependentField = hasRefField || fieldList.Any(ft =>
            ft is NormalLangPath nlp && nlp.LifetimeArgs.Length > 0);

        // Reference fields require lifetime parameters on the type
        if (hasRefField && lifetimeParams.Length == 0)
        {
            analyzer.AddException(new SemanticException(
                $"'{typeName}' has reference fields but no lifetime parameters. " +
                $"Add lifetime parameters: {typeName}['a]\n{token.GetLocationStringRepresentation()}"));
        }

        // Collect all used lifetimes (from field ref annotations + nested type lifetime args)
        var usedLifetimes = new HashSet<string>();
        foreach (var lt in fieldLtList)
            if (lt != null) usedLifetimes.Add(lt);
        foreach (var ft in fieldList)
            if (ft != null) CollectUsedLifetimes(ft, usedLifetimes);

        // Validate: lifetime args on nested types must reference declared lifetimes
        var declaredSet = lifetimeParams.ToHashSet();
        foreach (var ft in fieldList)
            if (ft is NormalLangPath nlp)
                ValidateLifetimeArgsDeclared(nlp, declaredSet, typeName, token, analyzer);

        // Validate: field types that require lifetime args must provide them
        foreach (var ft in fieldList)
        {
            if (ft is NormalLangPath nlp && nlp.LifetimeArgs.Length == 0)
            {
                var basePath = LangPath.StripGenerics(nlp);
                var def = analyzer.GetDefinition(basePath);
                if (def is StructTypeDefinition std && std.LifetimeParameters.Length > 0)
                    analyzer.AddException(new SemanticException(
                        $"Type '{std.Name}' requires {std.LifetimeParameters.Length} lifetime parameter(s) " +
                        $"but none were provided in field of '{typeName}'\n" +
                        token.GetLocationStringRepresentation()));
                else if (def is EnumTypeDefinition etd && etd.LifetimeParameters.Length > 0)
                    analyzer.AddException(new SemanticException(
                        $"Type '{etd.Name}' requires {etd.LifetimeParameters.Length} lifetime parameter(s) " +
                        $"but none were provided in field of '{typeName}'\n" +
                        token.GetLocationStringRepresentation()));
            }
        }

        // Validate: ref field lifetimes must be declared ('static is built-in)
        foreach (var lt in fieldLtList)
            if (lt != null && lt != "static" && !declaredSet.Contains(lt))
                analyzer.AddException(new SemanticException(
                    $"Undeclared lifetime '{lt}' in field of '{typeName}'\n" +
                    token.GetLocationStringRepresentation()));

        // Validate: all declared lifetimes must be used
        foreach (var lt in lifetimeParams)
            if (!usedLifetimes.Contains(lt))
                analyzer.AddException(new SemanticException(
                    $"Lifetime parameter '{lt}' is never used in '{typeName}'. " +
                    $"Every lifetime must be linked to a reference or nested lifetime-bounded type\n" +
                    token.GetLocationStringRepresentation()));
    }

    /// <summary>
    /// Checks that lifetime args on a type path are declared on the containing type.
    /// </summary>
    private static void ValidateLifetimeArgsDeclared(NormalLangPath path,
        HashSet<string> declaredLifetimes, string typeName, Token token, SemanticAnalyzer analyzer)
    {
        foreach (var lt in path.LifetimeArgs)
        {
            if (!declaredLifetimes.Contains(lt))
                analyzer.AddException(new SemanticException(
                    $"Undeclared lifetime '{lt}' in field type '{path}' of '{typeName}'\n" +
                    token.GetLocationStringRepresentation()));
        }
        // Recurse into generic args
        foreach (var ga in path.GetFrontGenerics())
            if (ga is NormalLangPath gaNlp)
                ValidateLifetimeArgsDeclared(gaNlp, declaredLifetimes, typeName, token, analyzer);
    }

    /// <summary>
    /// Collects all lifetime names used in a type path (from LifetimeArgs and reference annotations).
    /// </summary>
    private static void CollectUsedLifetimes(LangPath path, HashSet<string> result)
    {
        if (path is NormalLangPath nlp)
        {
            foreach (var lt in nlp.LifetimeArgs)
                result.Add(lt);
            foreach (var ga in nlp.GetFrontGenerics())
                CollectUsedLifetimes(ga, result);
        }
        if (path is TupleLangPath tlp)
            foreach (var tp in tlp.TypePaths)
                CollectUsedLifetimes(tp, result);
    }

    /// <summary>
    /// Checks if a type path has lifetime arguments (e.g., Bar['a] has lifetime args).
    /// </summary>
    private static bool TypeHasLifetimeArgs(LangPath typePath)
    {
        if (typePath is NormalLangPath nlp && nlp.LifetimeArgs.Length > 0)
            return true;
        return false;
    }

    private static bool GenericParamUsedInType(string paramName, LangPath? typePath)
    {
        if (typePath is NormalLangPath nlp)
        {
            foreach (var seg in nlp.PathSegments)
            {
                if (seg is NormalLangPath.NormalPathSegment ns)
                {
                    if (ns.Text == paramName) return true;
                    if (ns.HasGenericArgs)
                        foreach (var tp in ns.GenericArgs!.Value)
                            if (GenericParamUsedInType(paramName, tp)) return true;
                }
            }
        }
        if (typePath is TupleLangPath tlp)
        {
            foreach (var tp in tlp.TypePaths)
                if (GenericParamUsedInType(paramName, tp)) return true;
        }
        if (typePath is QualifiedAssocTypePath qap)
        {
            if (GenericParamUsedInType(paramName, qap.ForType)) return true;
            if (GenericParamUsedInType(paramName, qap.TraitPath)) return true;
        }
        return false;
    }

    public static StructTypeDefinition Parse(Parser parser, NormalLangPath module)
    {
        var token = parser.Pop();
        if (token is StructToken structToken)
        {
            var structIdentifier = Identifier.Parse(parser);

            // Parse [] (lifetimes only) and () (generic params) separately
            // [] in structs must only contain lifetimes — generic params go in ()
            var implicit_ = FunctionSignatureParser.ParseImplicitGenericParams(parser);
            if (implicit_ != null && implicit_.GenericParameters.Length > 0)
            {
                throw new ParseException(
                    $"Generic type parameters in struct definitions must use '()' not '[]'. " +
                    $"'[]' is reserved for lifetime parameters.\n" +
                    $"{structIdentifier.GetLocationStringRepresentation()}");
            }
            var explicit_ = FunctionSignatureParser.ParseParams(parser);

            // Validate: () params in structs must be comptime
            if (explicit_ != null && explicit_.Parameters.Length > 0)
            {
                var firstRuntime = explicit_.Parameters[0];
                throw new ParseException(
                    $"Runtime parameters are not allowed in struct definitions. " +
                    $"Use ':!' for compile-time parameters.\n" +
                    $"{firstRuntime.IdentifierToken.GetLocationStringRepresentation()}");
            }

            var genericParameters = new List<GenericParameter>();
            if (explicit_ != null) genericParameters.AddRange(explicit_.CheckedParams);

            var lifetimeParameters = new List<string>();
            if (implicit_?.LifetimeParameters.Length > 0) lifetimeParameters.AddRange(implicit_.LifetimeParameters);

            CurlyBrace.ParseLeft(parser);
            var next = parser.Peek();
            var fields = new List<VariableDefinition>();
            while (next is not RightCurlyBraceToken)
            {
                var field = VariableDefinition.Parse(parser);
                if (field.TypePath is null)
                    throw new ExpectedParserException(parser, ParseType.BaseLangPath, field.IdentifierToken);
                fields.Add(field);
                next = parser.Peek();
                if (next is not RightCurlyBraceToken)
                {
                    Comma.Parse(parser);
                    next = parser.Peek();
                }
                else
                {
                    break;
                }
            }

            CurlyBrace.Parseight(parser);

            return new StructTypeDefinition(structIdentifier.Identity, module, structToken, fields, genericParameters, lifetimeParameters);
        }

        throw new ExpectedParserException(parser, ParseType.Struct, token);
    }

    public VariableDefinition? GetField(string fieldName)
    {
        return Fields.FirstOrDefault(f => f.Name == fieldName);
    }

    public uint GetIndexOfField(string fieldName)
    {
        for (var i = 0; i < Fields.Length; i++)
        {
            var field = Fields[i];
            if (field.Name == fieldName) return (uint)i;
        }

        throw new FieldNotFoundException(fieldName, this);
    }


    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        // Push scope with generic param → concrete type mappings
        context.AddScope();
        for (int i = 0; i < GenericParameters.Length && i < genericArguments.Length; i++)
        {
            var argRefItem = context.GetRefItemFor(genericArguments[i]);
            if (argRefItem != null)
            {
                context.AddToDeepestScope(new NormalLangPath(null, [GenericParameters[i].Name]),
                    argRefItem);
            }
        }

        // Resolve field types while generic scope is active
        var resolvedFieldTypesList = new List<ConcreteDefinition.Type>();
        foreach (var field in Fields)
        {
            var refItem = context.GetRefItemFor(field.TypePath) as TypeRefItem;
            if (refItem == null)
            {
                // Field type unresolvable — can happen when generic args are invalid types
                context.PopScope();
                return new TypeRefItem()
                {
                    Type = new StructType(this, context.LLVMContext.CreateNamedStruct("__invalid__"))
                    {
                        TypeDefinition = { }
                    }
                };
            }
            resolvedFieldTypesList.Add(refItem.Type);
        }
        var resolvedFieldTypes = resolvedFieldTypesList.ToImmutableArray();

        var monomorphizedPath = genericArguments.Length > 0
            ? (LangPath)((NormalLangPath) TypePath).AppendGenerics(genericArguments)
            : TypePath;

        var structt = context.GetOrCreateNamedStruct(monomorphizedPath);
        if (structt.isNew)
            structt.typeRef.StructSetBody(
                resolvedFieldTypes.Select(t => t.TypeRef).ToArray(),
                false);

        context.PopScope();

        return new TypeRefItem()
        {
            Type = new StructType(this, structt.typeRef, monomorphizedPath)
            {
                TypeDefinition = { },
                ResolvedFieldTypes = resolvedFieldTypes
            }
        };
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path != (this as IDefinition).TypePath) return null;

        return [];
    }



    public class FieldNotFoundException : Exception
    {
        public FieldNotFoundException(string fieldName, StructTypeDefinition struc)
        {
            FieldName = fieldName;
            Struc = struc;
        }

        public string FieldName { get; }
        public StructTypeDefinition Struc { get; }

        public override string Message =>
            $"Field {FieldName} doesn't exist in struct '{(Struc as IDefinition).TypePath}'\n{Struc.StructToken?.GetLocationStringRepresentation()}";
    }
}