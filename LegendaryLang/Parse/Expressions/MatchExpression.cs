using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Parse.Expressions;

/// <summary>
/// A single arm in a match expression: Pattern => Expression
/// </summary>
public class MatchArm
{
    public required MatchPattern Pattern { get; init; }
    public required IExpression Body { get; init; }
}

/// <summary>
/// Base class for match patterns
/// </summary>
public abstract class MatchPattern
{
    public abstract Token Token { get; }
}

/// <summary>Wildcard pattern: _</summary>
public class WildcardPattern : MatchPattern
{
    public required Token WildcardToken { get; init; }
    public override Token Token => WildcardToken;
}

/// <summary>Unit variant pattern: Foo::A or A</summary>
public class VariantPattern : MatchPattern
{
    public required NormalLangPath VariantPath { get; init; }
    public override Token Token => VariantPath.FirstIdentifierToken!;
}

/// <summary>Tuple variant pattern: Foo::C(x, y)</summary>
public class TupleVariantPattern : MatchPattern
{
    public required NormalLangPath VariantPath { get; init; }
    public required ImmutableArray<string> Bindings { get; init; }
    public override Token Token => VariantPath.FirstIdentifierToken!;
}

public class MatchExpression : IExpression
{
    public MatchExpression(IExpression scrutinee, IEnumerable<MatchArm> arms, Token matchToken)
    {
        Scrutinee = scrutinee;
        Arms = arms.ToImmutableArray();
        MatchTok = matchToken;
    }

    public IExpression Scrutinee { get; }
    public ImmutableArray<MatchArm> Arms { get; }
    public Token MatchTok { get; }

    public IEnumerable<ISyntaxNode> Children =>
        new ISyntaxNode[] { Scrutinee }.Concat(Arms.Select(a => a.Body));

    public Token Token => MatchTok;
    public LangPath? TypePath { get; private set; }
    public bool HasGuaranteedExplicitReturn => Arms.All(a => a.Body.HasGuaranteedExplicitReturn);

    public void Analyze(SemanticAnalyzer analyzer)
    {
        Scrutinee.Analyze(analyzer);
        var scrutineeType = Scrutinee.TypePath;

        // Look up enum definition
        var enumDef = scrutineeType != null ? analyzer.GetDefinition(scrutineeType) as EnumTypeDefinition : null;
        if (enumDef == null && scrutineeType is NormalLangPath nlpScr && nlpScr.GetFrontGenerics().Length > 0)
            enumDef = analyzer.GetDefinition(nlpScr.PopGenerics()) as EnumTypeDefinition;

        if (enumDef == null && scrutineeType != null)
        {
            analyzer.AddException(new SemanticException(
                $"Cannot match on non-enum type '{scrutineeType}'\n{MatchTok.GetLocationStringRepresentation()}"));
            TypePath = LangPath.VoidBaseLangPath;
            return;
        }

        // Get generic args for substitution
        ImmutableArray<LangPath> genericArgs = [];
        if (scrutineeType is NormalLangPath nlpType)
            genericArgs = nlpType.GetFrontGenerics();

        LangPath? armType = null;
        bool hasWildcard = false;
        var coveredVariants = new HashSet<string>();

        foreach (var arm in Arms)
        {
            analyzer.AddScope();

            if (arm.Pattern is WildcardPattern)
            {
                hasWildcard = true;
            }
            else if (arm.Pattern is VariantPattern vp)
            {
                var variantName = vp.VariantPath.GetLastPathSegment()?.ToString();
                if (variantName != null && enumDef != null)
                {
                    var variant = enumDef.GetVariant(variantName);
                    if (variant == null)
                        analyzer.AddException(new SemanticException(
                            $"Enum '{enumDef.Name}' has no variant '{variantName}'\n{vp.Token.GetLocationStringRepresentation()}"));
                    else if (variant.FieldTypes.Length > 0)
                        analyzer.AddException(new SemanticException(
                            $"Variant '{variantName}' has fields — use '{variantName}(...)' pattern\n{vp.Token.GetLocationStringRepresentation()}"));
                    else
                        coveredVariants.Add(variantName);
                }
            }
            else if (arm.Pattern is TupleVariantPattern tvp)
            {
                var variantName = tvp.VariantPath.GetLastPathSegment()?.ToString();
                if (variantName != null && enumDef != null)
                {
                    var variant = enumDef.GetVariant(variantName);
                    if (variant == null)
                    {
                        analyzer.AddException(new SemanticException(
                            $"Enum '{enumDef.Name}' has no variant '{variantName}'\n{tvp.Token.GetLocationStringRepresentation()}"));
                    }
                    else
                    {
                        if (tvp.Bindings.Length != variant.FieldTypes.Length)
                            analyzer.AddException(new SemanticException(
                                $"Variant '{variantName}' has {variant.FieldTypes.Length} field(s), but pattern has {tvp.Bindings.Length}\n{tvp.Token.GetLocationStringRepresentation()}"));

                        // Register bindings as variables
                        for (int i = 0; i < tvp.Bindings.Length && i < variant.FieldTypes.Length; i++)
                        {
                            var fieldType = variant.FieldTypes[i];
                            if (genericArgs.Length > 0 && enumDef.GenericParameters.Length > 0)
                                fieldType = FieldAccessExpression.SubstituteGenerics(
                                    fieldType, enumDef.GenericParameters, genericArgs);
                            if (tvp.Bindings[i] != "_")
                                analyzer.RegisterVariableType(
                                    new NormalLangPath(null, [tvp.Bindings[i]]), fieldType);
                        }
                        coveredVariants.Add(variantName);
                    }
                }
            }

            arm.Body.Analyze(analyzer);

            if (armType == null)
            {
                // Don't use an arm with guaranteed explicit return as the "expected" type
                if (!arm.Body.HasGuaranteedExplicitReturn)
                    armType = arm.Body.TypePath;
            }
            else if (arm.Body.TypePath != armType && !arm.Body.HasGuaranteedExplicitReturn)
                analyzer.AddException(new SemanticException(
                    $"Match arm type '{arm.Body.TypePath}' does not match previous arm type '{armType}'\n{arm.Body.Token.GetLocationStringRepresentation()}"));

            analyzer.PopScope();
        }

        // Check exhaustiveness
        if (!hasWildcard && enumDef != null)
        {
            foreach (var variant in enumDef.Variants)
            {
                if (!coveredVariants.Contains(variant.Name))
                {
                    analyzer.AddException(new SemanticException(
                        $"Non-exhaustive match: variant '{variant.Name}' not covered\n{MatchTok.GetLocationStringRepresentation()}"));
                }
            }
        }

        TypePath = armType ?? LangPath.VoidBaseLangPath;
    }

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var scrutineeVal = Scrutinee.CodeGen(codeGenContext);
        var enumType = scrutineeVal.Type as EnumType;

        // Load the tag (field 0)
        var tagPtr = codeGenContext.Builder.BuildStructGEP2(enumType!.TypeRef, scrutineeVal.ValueRef, 0);
        var tagVal = codeGenContext.Builder.BuildLoad2(LLVMTypeRef.Int32, tagPtr);

        // Get the current function for creating basic blocks
        var currentBlock = codeGenContext.Builder.InsertBlock;
        var function = currentBlock.Parent;

        // Create merge block
        var mergeBlock = function.AppendBasicBlock("match.merge");

        // Determine result type
        var resultTypeRef = codeGenContext.GetRefItemFor(TypePath) as TypeRefItem;
        LLVMValueRef? resultAlloca = null;
        if (resultTypeRef != null && TypePath != LangPath.VoidBaseLangPath)
            resultAlloca = codeGenContext.Builder.BuildAlloca(resultTypeRef.TypeRef, "match.result");

        // Create switch with default block
        var defaultBlock = function.AppendBasicBlock("match.default");
        var switchInst = codeGenContext.Builder.BuildSwitch(tagVal, defaultBlock, (uint)Arms.Length);

        // Track if we have a wildcard arm
        MatchArm? wildcardArm = null;

        foreach (var arm in Arms)
        {
            if (arm.Pattern is WildcardPattern)
            {
                wildcardArm = arm;
                continue;
            }

            string? variantName = null;
            if (arm.Pattern is VariantPattern vp)
                variantName = vp.VariantPath.GetLastPathSegment()?.ToString();
            else if (arm.Pattern is TupleVariantPattern tvp)
                variantName = tvp.VariantPath.GetLastPathSegment()?.ToString();

            if (variantName == null) continue;

            var variant = enumType.GetVariant(variantName);
            if (variant == null) continue;

            var armBlock = function.AppendBasicBlock($"match.arm.{variantName}");
            switchInst.AddCase(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)variant.Tag, false), armBlock);

            codeGenContext.Builder.PositionAtEnd(armBlock);
            codeGenContext.AddScope();

            // For tuple variant patterns, extract payload and bind variables
            if (arm.Pattern is TupleVariantPattern tvpArm && enumType.HasPayloads)
            {
                var resolved = enumType.GetResolvedVariant(variantName);
                if (resolved != null)
                {
                    var payloadPtr = codeGenContext.Builder.BuildStructGEP2(
                        enumType.TypeRef, scrutineeVal.ValueRef, 1);

                    // Build a struct type for this variant's payload
                    var fieldLlvmTypes = resolved.Value.fieldTypes.Select(ft => ft.TypeRef).ToArray();

                    ulong offset = 0;
                    for (int i = 0; i < tvpArm.Bindings.Length && i < resolved.Value.fieldTypes.Length; i++)
                    {
                        var fieldType = resolved.Value.fieldTypes[i];
                        var bindingName = tvpArm.Bindings[i];

                        // Calculate field pointer using byte offset from payload start
                        var fieldPtr = payloadPtr;
                        if (offset > 0)
                        {
                            fieldPtr = codeGenContext.Builder.BuildGEP2(
                                LLVMTypeRef.Int8, payloadPtr,
                                [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, offset, false)]);
                        }

                        var fieldVal = codeGenContext.Builder.BuildLoad2(fieldType.TypeRef, fieldPtr);
                        var fieldAlloca = codeGenContext.Builder.BuildAlloca(fieldType.TypeRef, bindingName);
                        fieldType.AssignTo(codeGenContext,
                            new ValueRefItem { Type = fieldType, ValueRef = fieldVal },
                            new ValueRefItem { Type = fieldType, ValueRef = fieldAlloca });

                        if (bindingName != "_")
                        {
                            codeGenContext.AddToDeepestScope(
                                new NormalLangPath(null, [bindingName]),
                                new ValueRefItem { Type = fieldType, ValueRef = fieldAlloca });
                        }

                        unsafe
                        {
                            var dataLayout = LLVM.GetModuleDataLayout(codeGenContext.Module);
                            offset += LLVM.StoreSizeOfType(dataLayout, fieldType.TypeRef);
                        }
                    }
                }
            }

            var armVal = arm.Body.CodeGen(codeGenContext);
            if (resultAlloca != null && resultTypeRef != null)
            {
                resultTypeRef.Type.AssignTo(codeGenContext, armVal,
                    new ValueRefItem { Type = resultTypeRef.Type, ValueRef = resultAlloca.Value });
            }

            codeGenContext.PopScope();
            codeGenContext.Builder.BuildBr(mergeBlock);
        }

        // Handle default/wildcard block
        codeGenContext.Builder.PositionAtEnd(defaultBlock);
        if (wildcardArm != null)
        {
            codeGenContext.AddScope();
            var armVal = wildcardArm.Body.CodeGen(codeGenContext);
            if (resultAlloca != null && resultTypeRef != null)
            {
                resultTypeRef.Type.AssignTo(codeGenContext, armVal,
                    new ValueRefItem { Type = resultTypeRef.Type, ValueRef = resultAlloca.Value });
            }
            codeGenContext.PopScope();
        }
        codeGenContext.Builder.BuildBr(mergeBlock);

        // Position at merge block
        codeGenContext.Builder.PositionAtEnd(mergeBlock);

        if (resultAlloca != null && resultTypeRef != null)
        {
            return new ValueRefItem
            {
                Type = resultTypeRef.Type,
                ValueRef = resultAlloca.Value
            };
        }

        return codeGenContext.GetVoid();
    }

    public void ResolvePaths(PathResolver resolver)
    {
        if (Scrutinee is IPathResolvable pr) pr.ResolvePaths(resolver);
        foreach (var arm in Arms)
        {
            // Resolve variant paths in patterns
            if (arm.Pattern is VariantPattern vp)
                vp.VariantPath.Resolve(resolver); // NormalLangPath is immutable so this doesn't help...
            // For bodies
            if (arm.Body is IPathResolvable bodyPr) bodyPr.ResolvePaths(resolver);
        }
    }

    public static MatchExpression Parse(Parser parser)
    {
        var matchTok = parser.Pop();
        if (matchTok is not MatchToken)
            throw new ExpectedParserException(parser, ParseType.Match, matchTok);

        // Suppress struct literal so `match x { ... }` doesn't parse x { as struct creation
        IExpression.SuppressStructLiteral = true;
        var scrutinee = IExpression.Parse(parser);
        IExpression.SuppressStructLiteral = false;
        CurlyBrace.ParseLeft(parser);

        var arms = new List<MatchArm>();
        while (parser.Peek() is not RightCurlyBraceToken)
        {
            var pattern = ParsePattern(parser);

            // Expect =>
            var arrow = parser.Pop();
            if (arrow is not FatArrowToken)
                throw new ExpectedParserException(parser, ParseType.FatArrow, arrow);

            var body = IExpression.Parse(parser);
            arms.Add(new MatchArm { Pattern = pattern, Body = body });

            // Optional comma
            if (parser.Peek() is CommaToken) parser.Pop();
        }

        CurlyBrace.Parseight(parser);
        return new MatchExpression(scrutinee, arms, (Token)matchTok);
    }

    private static MatchPattern ParsePattern(Parser parser)
    {
        // Wildcard: _
        if (parser.Peek() is IdentifierToken ident && ident.Identity == "_")
        {
            parser.Pop();
            return new WildcardPattern { WildcardToken = ident };
        }

        // Variant pattern: Path::Variant or Path::Variant(bindings)
        var path = (NormalLangPath)LangPath.Parse(parser);

        if (parser.Peek() is LeftParenthesisToken)
        {
            Parenthesis.ParseLeft(parser);
            var bindings = new List<string>();
            while (parser.Peek() is not RightParenthesisToken)
            {
                var binding = Identifier.Parse(parser);
                bindings.Add(binding.Identity);
                if (parser.Peek() is CommaToken) parser.Pop();
                else break;
            }
            Parenthesis.ParseRight(parser);
            return new TupleVariantPattern
            {
                VariantPath = path,
                Bindings = bindings.ToImmutableArray()
            };
        }

        return new VariantPattern { VariantPath = path };
    }
}
