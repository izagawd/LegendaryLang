using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions.Types;

public class EnumVariant
{
    public required string Name { get; init; }
    public required Token Token { get; init; }
    public required int Tag { get; init; }
    /// <summary>
    /// Payload field types. Empty for unit variants.
    /// </summary>
    public ImmutableArray<LangPath> FieldTypes { get; set; } = [];
}

public class EnumTypeDefinition : ComposableTypeDefinition
{
    public EnumTypeDefinition(string name, NormalLangPath module, EnumToken token,
        IEnumerable<EnumVariant> variants, IEnumerable<GenericParameter> genericParameters,
        IEnumerable<string>? lifetimeParameters = null)
    {
        EnumToken = token;
        Name = name;
        Module = module;
        Variants = variants.ToImmutableArray();
        GenericParameters = genericParameters.ToImmutableArray();
        LifetimeParameters = lifetimeParameters?.ToImmutableArray() ?? [];
    }

    public override Token Token => EnumToken;
    public EnumToken EnumToken { get; }
    public ImmutableArray<EnumVariant> Variants { get; }
    public ImmutableArray<GenericParameter> GenericParameters { get; }
    public ImmutableArray<string> LifetimeParameters { get; }

    public override string Name { get; }
    public override NormalLangPath Module { get; }

    public override ImmutableArray<LangPath> ComposedTypes =>
        Variants.SelectMany(v => v.FieldTypes).ToImmutableArray();

    public EnumVariant? GetVariant(string name) =>
        Variants.FirstOrDefault(v => v.Name == name);

    public override void ResolvePaths(PathResolver resolver)
    {
        foreach (var variant in Variants)
        {
            var resolved = new List<LangPath>();
            foreach (var ft in variant.FieldTypes)
                resolved.Add(ft.Resolve(resolver));
            variant.FieldTypes = resolved.ToImmutableArray();
        }

        foreach (var gp in GenericParameters)
            for (int i = 0; i < gp.TraitBounds.Count; i++)
                gp.TraitBounds[i] = gp.TraitBounds[i].Resolve(resolver);
    }

    public override void Analyze(SemanticAnalyzer analyzer)
    {
        var seen = new HashSet<string>();
        foreach (var gp in GenericParameters)
            if (!seen.Add(gp.Name))
                analyzer.AddException(new SemanticException(
                    $"Duplicate generic parameter name '{gp.Name}'\n{Token.GetLocationStringRepresentation()}"));

        var seenVariants = new HashSet<string>();
        foreach (var v in Variants)
            if (!seenVariants.Add(v.Name))
                analyzer.AddException(new SemanticException(
                    $"Duplicate variant name '{v.Name}' in enum '{Name}'\n{Token.GetLocationStringRepresentation()}"));
    }

    /// <summary>
    /// Computes the maximum payload size in bytes across all variants for LLVM layout.
    /// </summary>
    private int GetMaxPayloadFieldCount()
    {
        return Variants.Max(v => v.FieldTypes.Length);
    }

    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        context.AddScope();
        for (int i = 0; i < GenericParameters.Length && i < genericArguments.Length; i++)
        {
            var argRefItem = context.GetRefItemFor(genericArguments[i]);
            if (argRefItem != null)
                context.AddToDeepestScope(new NormalLangPath(null, [GenericParameters[i].Name]), argRefItem);
        }

        var monomorphizedPath = genericArguments.Length > 0
            ? (LangPath)((NormalLangPath)TypePath).AppendGenerics(genericArguments)
            : TypePath;

        // Resolve all variant field types while generic scope is active
        var resolvedVariants = new List<(EnumVariant variant, ImmutableArray<Type> fieldTypes)>();
        int maxPayloadBits = 0;
        foreach (var variant in Variants)
        {
            var fieldTypesList = new List<Type>();
            int payloadBits = 0;
            foreach (var ft in variant.FieldTypes)
            {
                var substituted = ft;
                if (genericArguments.Length > 0 && GenericParameters.Length > 0)
                    substituted = LegendaryLang.Parse.Expressions.FieldAccessExpression.SubstituteGenerics(
                        ft, GenericParameters, genericArguments);
                var refItem = context.GetRefItemFor(substituted) as TypeRefItem;
                if (refItem == null)
                {
                    context.PopScope();
                    return new TypeRefItem
                    {
                        Type = new EnumType(this,
                            context.LLVMContext.CreateNamedStruct("__invalid__"),
                            monomorphizedPath)
                    };
                }
                fieldTypesList.Add(refItem.Type);
                payloadBits += (int)refItem.TypeRef.SizeOf.ConstIntZExt;
            }
            resolvedVariants.Add((variant, fieldTypesList.ToImmutableArray()));
            if (payloadBits > maxPayloadBits) maxPayloadBits = payloadBits;
        }

        // Build the LLVM type: { i32 tag, [maxPayloadSize x i8] }
        // If no variants have payloads, just { i32 }
        bool hasPayloads = Variants.Any(v => v.FieldTypes.Length > 0);

        // Calculate max payload size using actual LLVM type sizes
        ulong maxPayloadBytes = 0;
        foreach (var (variant, fieldTypes) in resolvedVariants)
        {
            if (fieldTypes.Length == 0) continue;
            ulong variantSize = 0;
            foreach (var ft in fieldTypes)
            {
                unsafe
                {
                    var dataLayout = LLVM.GetModuleDataLayout(context.Module);
                    variantSize += LLVM.StoreSizeOfType(dataLayout, ft.TypeRef);
                }
            }
            if (variantSize > maxPayloadBytes) maxPayloadBytes = variantSize;
        }

        var enumStructResult = context.GetOrCreateNamedStruct(monomorphizedPath);
        var enumStruct = enumStructResult.typeRef;
        if (enumStructResult.isNew)
        {
            if (hasPayloads && maxPayloadBytes > 0)
            {
                var payloadArrayType = LLVMTypeRef.CreateArray(LLVMTypeRef.Int8, (uint)maxPayloadBytes);
                enumStruct.StructSetBody([LLVMTypeRef.Int32, payloadArrayType], false);
            }
            else
            {
                enumStruct.StructSetBody([LLVMTypeRef.Int32], false);
            }
        }

        context.PopScope();

        return new TypeRefItem
        {
            Type = new EnumType(this, enumStruct, monomorphizedPath)
            {
                ResolvedVariants = resolvedVariants.ToImmutableArray(),
                HasPayloads = hasPayloads,
                MaxPayloadBytes = maxPayloadBytes
            }
        };
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path != (this as IDefinition).TypePath) return null;
        return [];
    }

    public static EnumTypeDefinition Parse(Parser parser, NormalLangPath module)
    {
        var token = parser.Pop();
        if (token is not EnumToken enumToken)
            throw new ExpectedParserException(parser, ParseType.Enum, token);

        var nameToken = Identifier.Parse(parser);

        // Parse generic parameters: (T:! type) explicit params, or [T]/&lt;T&gt; legacy
        var generics = FunctionSignatureParser.ParseGenericParams(parser);
        var genericParameters = generics.GenericParameters.ToList();
        IEnumerable<string> lifetimeParameters = generics.LifetimeParameters;

        CurlyBrace.ParseLeft(parser);

        var variants = new List<EnumVariant>();
        int tag = 0;
        while (parser.Peek() is not RightCurlyBraceToken)
        {
            var variantName = Identifier.Parse(parser);
            var fieldTypes = new List<LangPath>();

            // Check for tuple variant: VariantName(Type1, Type2, ...)
            if (parser.Peek() is LeftParenthesisToken)
            {
                Parenthesis.ParseLeft(parser);
                while (parser.Peek() is not RightParenthesisToken)
                {
                    fieldTypes.Add(LangPath.Parse(parser, true));
                    if (parser.Peek() is CommaToken) parser.Pop();
                    else break;
                }
                Parenthesis.ParseRight(parser);
            }

            variants.Add(new EnumVariant
            {
                Name = variantName.Identity,
                Token = variantName,
                Tag = tag++,
                FieldTypes = fieldTypes.ToImmutableArray()
            });

            // Optional comma between variants
            if (parser.Peek() is CommaToken) parser.Pop();
        }

        CurlyBrace.Parseight(parser);

        return new EnumTypeDefinition(nameToken.Identity, module, enumToken, variants, genericParameters, lifetimeParameters);
    }
}
