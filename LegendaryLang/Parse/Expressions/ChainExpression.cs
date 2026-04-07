using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Statements;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

// ─── Chain steps (parse-time, purely syntactic) ──────────────────────

public abstract class ChainStep
{
    public Token Token { get; init; }
}

public class AccessStep : ChainStep
{
    public required string Name { get; init; }
    public IdentifierToken IdentifierToken { get; init; }
    public override string ToString() => $".{Name}";
}

public class CallStep : ChainStep
{
    public required ImmutableArray<IExpression> Arguments { get; init; }
    public override string ToString() => $"({string.Join(", ", Arguments)})";
}

// ─── Resolved kinds (determined during semantic analysis) ────────────
//
// After analysis, a ChainExpression resolves to a tree of IChainKind nodes.
// The tree is recursive: FieldAccessKind and MethodCallKind hold their
// receiver as another IChainKind, so f.get().val becomes:
//
//   FieldAccessKind(
//     receiver: MethodCallKind(
//       receiver: VariableRefKind("f"),
//       method: "get"),
//     field: "val")
//
// Each kind implements its own CodeGen — no delegation to legacy expression types.

/// <summary>
/// A resolved chain node. Carries its type and knows how to codegen.
/// </summary>
public interface IChainKind
{
    LangPath? TypePath { get; }
    ValueRefItem CodeGen(CodeGenContext ctx);
    IEnumerable<ISyntaxNode> KindChildren { get; }
    
    /// <summary>
    /// Extract borrow sources for this kind. Each kind knows where its borrows come from.
    /// Returns empty if this kind doesn't produce borrows.
    /// </summary>
    List<(string sourceName, RefKind refKind)> GetBorrowSources(SemanticAnalyzer analyzer) => [];
}

/// <summary>Simple variable/path reference. Terminal node.</summary>
public class VariableRefKind : IChainKind
{
    public required LangPath Path { get; init; }
    public LangPath? TypePath { get; init; }
    public IEnumerable<ISyntaxNode> KindChildren => [];

    public ValueRefItem CodeGen(CodeGenContext ctx)
    {
        var rawRef = ctx.GetRefItemFor(Path);
        if (rawRef is ValueRefItem refItem)
            return new ValueRefItem { ValueRef = refItem.ValueRef, Type = refItem.Type };
        
        throw new InvalidOperationException(
            $"'{Path}' resolved to a type reference, not a variable. " +
            $"A type is being used where a runtime value is expected.");
    }
}

/// <summary>Wraps an arbitrary expression as a chain root. Terminal node.</summary>
public class ExpressionRefKind : IChainKind
{
    public required IExpression Expression { get; init; }
    public LangPath? TypePath => Expression.TypePath;
    public IEnumerable<ISyntaxNode> KindChildren => [Expression];
    public ValueRefItem CodeGen(CodeGenContext ctx) => Expression.CodeGen(ctx);
}

/// <summary>Struct field access. Recursive — holds its receiver.</summary>
public class FieldAccessKind : IChainKind
{
    public required IChainKind Receiver { get; init; }
    public required string FieldName { get; init; }
    public LangPath? TypePath { get; set; }
    public bool AutoDeref { get; init; }
    public int AutoDerefDepth { get; init; }
    /// <summary>
    /// Maximum ref kind capability for method calls through this field access chain.
    /// Set when accessing fields through a reference — narrows what methods can be called.
    /// e.g., through &amp;shared Wrapper, field &amp;uniq Holder narrows to shared capability.
    /// </summary>
    public RefKind? MaxCapability { get; init; }
    public IEnumerable<ISyntaxNode> KindChildren => Receiver.KindChildren;

    public ValueRefItem CodeGen(CodeGenContext ctx)
    {
        var receiverVal = Receiver.CodeGen(ctx);
        return FieldAccessExpression.EmitFieldAccess(ctx, receiverVal, FieldName, AutoDeref, AutoDerefDepth);
    }
}

/// <summary>Enum unit variant. Terminal node.</summary>
public class EnumVariantKind : IChainKind
{
    public required EnumTypeDefinition EnumDef { get; init; }
    public required EnumVariant Variant { get; init; }
    public required LangPath EnumTypePath { get; init; }
    public LangPath? TypePath { get; set; }
    public IEnumerable<ISyntaxNode> KindChildren => [];

    public ValueRefItem CodeGen(CodeGenContext ctx)
    {
        var typeRef = ctx.GetRefItemFor(EnumTypePath) as TypeRefItem;
        var enumType = typeRef?.Type as EnumType;
        var alloca = ctx.Builder.BuildAlloca(enumType!.TypeRef);
        var tagPtr = ctx.Builder.BuildStructGEP2(enumType.TypeRef, alloca, 0);
        ctx.Builder.BuildStore(
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)Variant.Tag, false),
            tagPtr);
        return new ValueRefItem { Type = enumType, ValueRef = alloca };
    }
}

/// <summary>Enum tuple variant with payload fields (e.g. Foo.C(42)). Terminal node.</summary>
public class EnumVariantCreationKind : IChainKind
{
    public required EnumVariant Variant { get; init; }
    public required LangPath EnumTypePath { get; init; }
    public required ImmutableArray<IExpression> Arguments { get; init; }
    public LangPath? TypePath { get; set; }
    public IEnumerable<ISyntaxNode> KindChildren => Arguments;

    public List<(string sourceName, RefKind refKind)> GetBorrowSources(SemanticAnalyzer analyzer)
    {
        // Propagate borrows from arguments — e.g., Foo.One(make Droppable{reference: &uniq x})
        // borrows x through the struct field, same as struct creation.
        var results = new List<(string, RefKind)>();
        foreach (var arg in Arguments)
            results.AddRange(LetStatement.ExtractBorrowSources(arg, analyzer));
        return results;
    }

    public ValueRefItem CodeGen(CodeGenContext ctx)
    {
        var typeRef = ctx.GetRefItemFor(EnumTypePath) as TypeRefItem;
        var enumType = typeRef?.Type as EnumType;
        if (enumType == null)
            throw new InvalidOperationException(
                $"Cannot resolve enum type '{EnumTypePath}' during codegen.");

        var alloca = ctx.Builder.BuildAlloca(enumType.TypeRef);
        // Store tag
        var tagPtr = ctx.Builder.BuildStructGEP2(enumType.TypeRef, alloca, 0);
        ctx.Builder.BuildStore(
            LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)Variant.Tag, false),
            tagPtr);

        // Store payload fields
        if (Arguments.Length > 0 && enumType.HasPayloads)
        {
            var payloadPtr = ctx.Builder.BuildStructGEP2(enumType.TypeRef, alloca, 1);
            var resolved = enumType.GetResolvedVariant(Variant.Name);
            if (resolved != null)
            {
                ulong offset = 0;
                for (int i = 0; i < Arguments.Length && i < resolved.Value.fieldTypes.Length; i++)
                {
                    var fieldType = resolved.Value.fieldTypes[i];
                    var argVal = Arguments[i].CodeGen(ctx);

                    var fieldPtr = payloadPtr;
                    if (offset > 0)
                    {
                        fieldPtr = ctx.Builder.BuildGEP2(
                            LLVMTypeRef.Int8, payloadPtr,
                            [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, offset, false)]);
                    }

                    fieldType.AssignTo(ctx, argVal,
                        new ValueRefItem { Type = fieldType, ValueRef = fieldPtr });

                    unsafe
                    {
                        var dataLayout = LLVM.GetModuleDataLayout(ctx.Module);
                        offset += LLVM.StoreSizeOfType(dataLayout, fieldType.TypeRef);
                    }
                }
            }
        }

        return new ValueRefItem { Type = enumType, ValueRef = alloca };
    }
}

/// <summary>
/// Function call (standalone or static method). Terminal node.
/// Does its own CodeGen.
/// </summary>
public class FunctionCallKind : IChainKind
{
    public required NormalLangPath FunctionPath { get; set; }
    public required ImmutableArray<IExpression> Arguments { get; init; }
    public NormalLangPath? QualifiedAsType { get; init; }
    public FunctionDefinition? FuncDef { get; set; }
    public LangPath? TypePath { get; set; }
    public IEnumerable<ISyntaxNode> KindChildren => Arguments;

    public List<(string sourceName, RefKind refKind)> GetBorrowSources(SemanticAnalyzer analyzer)
    {
        var results = new List<(string, RefKind)>();
        if (FuncDef == null) return results;
        if (FuncDef.ReturnLifetime == "static") return results;

        // Explicit lifetime annotations: link return lifetime to matching param lifetimes
        if (FuncDef.ReturnLifetime != null)
        {
            for (int i = 0; i < FuncDef.Arguments.Length && i < Arguments.Length; i++)
            {
                if (!FuncDef.ArgumentLifetimes.TryGetValue(i, out var paramLt)) continue;
                if (paramLt != FuncDef.ReturnLifetime) continue;
                var origin = LetStatement.TraceArgToSource(Arguments[i], analyzer);
                if (origin != null)
                {
                    var inputKind = RefTypeDefinition.ExtractRefKindFromPath(FuncDef.Arguments[i].TypePath);
                    results.Add((origin, inputKind));
                }
            }
        }
        else
        {
            // Implicit lifetime elision: only applies when the return type
            // can actually hold a borrow: references (&T) or types with
            // lifetime params (Wrapper['a]). Plain value types (i32) cannot.
            bool returnCanHoldBorrow = RefTypeDefinition.IsReferenceType(TypePath);
            if (!returnCanHoldBorrow && TypePath != null)
            {
                var retDef = analyzer.GetDefinition(LangPath.StripGenerics(TypePath));
                if (retDef is ComposableTypeDefinition ctd && ctd.LifetimeParameters.Length > 0)
                    returnCanHoldBorrow = true;
            }

            if (returnCanHoldBorrow)
            {
                // 1. Reference-type arguments: if exactly one, the return borrows from it
                var refArgSources = new List<(string, RefKind)>();
                for (int i = 0; i < FuncDef.Arguments.Length && i < Arguments.Length; i++)
                {
                    if (!RefTypeDefinition.IsReferenceType(FuncDef.Arguments[i].TypePath)) continue;
                    var origin = LetStatement.TraceArgToSource(Arguments[i], analyzer);
                    if (origin != null)
                    {
                        var inputKind = RefTypeDefinition.ExtractRefKindFromPath(FuncDef.Arguments[i].TypePath);
                        refArgSources.Add((origin, inputKind));
                    }
                }
                if (refArgSources.Count == 1) results.Add(refArgSources[0]);

                // 2. Non-reference arguments whose concrete types carry lifetimes.
                //    e.g., fn Pass[T](x: T) -> T where T = Yo['a] — the argument's type
                //    has lifetime parameters, so it carries borrows through its definition.
                //    The return type inherits those borrows because it's the same lifetime-carrying type.
                if (results.Count == 0)
                {
                    for (int i = 0; i < Arguments.Length; i++)
                    {
                        var argType = Arguments[i].TypePath;
                        if (argType == null || analyzer.IsTypeCopy(argType)) continue;
                        if (RefTypeDefinition.IsReferenceType(argType)) continue;

                        // Check if the argument's concrete type has lifetime parameters
                        var argDef = analyzer.GetDefinition(LangPath.StripGenerics(argType));
                        if (argDef is not ComposableTypeDefinition ctd || ctd.LifetimeParameters.Length == 0)
                            continue;

                        // Type carries lifetimes — propagate borrows from the argument
                        var argVarName = IExpression.TryGetSimpleVariableName(Arguments[i]);
                        if (argVarName != null)
                        {
                            var borrowInfo = analyzer.GetBorrowInfo(argVarName);
                            if (borrowInfo != null)
                            {
                                results.Add(borrowInfo.Value);
                                continue;
                            }
                        }

                        // Handle expressions (chained calls like Pass2(Pass1(h)))
                        var argBorrows = LetStatement.ExtractBorrowSources(Arguments[i], analyzer);
                        results.AddRange(argBorrows);
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Factory: analyze a function call and return a fully resolved FunctionCallKind.
    /// Handles generic inference, trait method resolution, inherent impl resolution.
    /// </summary>
    public static IChainKind AnalyzeCall(
        NormalLangPath functionPath, ImmutableArray<IExpression> arguments,
        NormalLangPath? qualifiedAsType, LangPath? expectedReturnType,
        Token? errorToken, SemanticAnalyzer analyzer)
    {
        var result = new FunctionCallKind
        {
            FunctionPath = functionPath,
            Arguments = arguments,
            QualifiedAsType = qualifiedAsType,
        };

        CallExpressionHelper.AnalyzeArgumentsWithReborrow(arguments, analyzer);
        var tokenLoc = errorToken?.GetLocationStringRepresentation() ?? "";

        // Enum tuple variant check
        if (functionPath.PathSegments.Length >= 2)
        {
            var workingPath = functionPath;
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
                    var enumDefLookup = analyzer.GetDefinition(parentPath);
                    if (enumDefLookup == null && parentPath is NormalLangPath nlpP && nlpP.GetFrontGenerics().Length > 0)
                        enumDefLookup = analyzer.GetDefinition(nlpP.PopGenerics());
                    if (enumDefLookup is EnumTypeDefinition enumDef)
                    {
                        var variant = enumDef.GetVariant(variantName);
                        if (variant != null)
                        {
                            ImmutableArray<LangPath> genericArgs = frontGenerics;
                            if (genericArgs.Length == 0 && parentPath is NormalLangPath nlpParent)
                                genericArgs = nlpParent.GetFrontGenerics();
                            if (genericArgs.Length == 0 && enumDef.GenericParameters.Length > 0)
                            {
                                // Try to infer from variant field types (e.g., Option.Some(42) → T=i32)
                                var constraints = new List<(LangPath, LangPath)>();
                                for (int i = 0; i < variant.FieldTypes.Length && i < arguments.Length; i++)
                                    if (arguments[i].TypePath != null)
                                        constraints.Add((variant.FieldTypes[i], arguments[i].TypePath));
                                var inferred = TypeInference.InferFromConstraints(enumDef.GenericParameters, constraints);
                                if (inferred != null)
                                    genericArgs = inferred.Value;
                                // Fallback: infer from expected return type (e.g., Option.None in context expecting Option(i32))
                                else if (expectedReturnType is NormalLangPath nlpExpected)
                                {
                                    var expectedGenerics = nlpExpected.GetFrontGenerics();
                                    if (expectedGenerics.Length == enumDef.GenericParameters.Length)
                                        genericArgs = expectedGenerics;
                                }
                            }
                            LangPath enumTypePath;
                            if (genericArgs.Length > 0)
                                enumTypePath = ((NormalLangPath)enumDef.TypePath).AppendGenerics(genericArgs);
                            else
                                enumTypePath = enumDef.TypePath;
                            if (variant.FieldTypes.Length != arguments.Length)
                                analyzer.AddException(new SemanticException(
                                    $"Variant '{variantName}' expects {variant.FieldTypes.Length} field(s), got {arguments.Length}\n{tokenLoc}"));
                            else
                                for (int i = 0; i < variant.FieldTypes.Length; i++)
                                {
                                    var expected = variant.FieldTypes[i];
                                    if (genericArgs.Length > 0 && enumDef.GenericParameters.Length > 0)
                                        expected = FieldAccessExpression.SubstituteGenerics(expected, enumDef.GenericParameters, genericArgs);
                                    if (arguments[i].TypePath != expected)
                                        analyzer.AddException(new SemanticException(
                                            $"Variant '{variantName}' field {i} expects type '{expected}', found '{arguments[i].TypePath}'\n{tokenLoc}"));
                                }
                            return new EnumVariantCreationKind
                            {
                                Variant = variant,
                                EnumTypePath = enumTypePath,
                                Arguments = arguments,
                                TypePath = enumTypePath,
                            };
                        }
                    }
                }
            }
        }

        // Look up function definition
        var def = analyzer.GetDefinition(functionPath);
        if (def is null) def = analyzer.GetDefinition(functionPath.Pop());
        if (def is null && functionPath.GetFrontGenerics().Length > 0)
        {
            var stripped = functionPath.PopGenerics();
            if (stripped != null) def = analyzer.GetDefinition(stripped);
            if (def is null && stripped != null) def = analyzer.GetDefinition(stripped.Pop());
        }

        if (def is FunctionDefinition fd)
        {
            result.FuncDef = fd;

            // Separate comptime type args from runtime args based on CallParamLayout.
            // This handles cases where callers pass args directly without going through
            // DoFunctionCall (e.g., qualified trait calls, method calls with comptime params).
            if (fd.CallParamLayout.Length > 0)
            {
                var typeArgs = new List<LangPath>();
                var runtimeArgs = new List<IExpression>();
                for (int argIdx = 0; argIdx < arguments.Length; argIdx++)
                {
                    if (argIdx < fd.CallParamLayout.Length && fd.CallParamLayout[argIdx])
                    {
                        var typePath = ChainExpression.TryResolveAsType(arguments[argIdx], analyzer);
                        if (typePath != null)
                            typeArgs.Add(typePath);
                        else
                            runtimeArgs.Add(arguments[argIdx]);
                    }
                    else
                    {
                        runtimeArgs.Add(arguments[argIdx]);
                    }
                }
                if (typeArgs.Count > 0)
                {
                    arguments = runtimeArgs.ToImmutableArray();
                    result = new FunctionCallKind
                    {
                        FunctionPath = functionPath.AppendGenerics(typeArgs),
                        Arguments = arguments,
                        QualifiedAsType = qualifiedAsType,
                        FuncDef = fd,
                    };
                    functionPath = result.FunctionPath;
                }
            }

            var providedGenerics = functionPath.GetFrontGenerics();

            // Type inference: when no explicit generics provided, infer all
            if (fd.GenericParameters.Length > 0 && providedGenerics.Length == 0)
            {
                var constraints = new List<(LangPath, LangPath)>();
                for (int i = 0; i < fd.Arguments.Length && i < arguments.Length; i++)
                    if (fd.Arguments[i].TypePath != null && arguments[i].TypePath != null)
                        constraints.Add((fd.Arguments[i].TypePath, arguments[i].TypePath));
                // Only add return type constraint if both are concrete (not associated types)
                if (expectedReturnType is NormalLangPath && fd.ReturnTypePath is NormalLangPath)
                    constraints.Add((fd.ReturnTypePath, expectedReturnType));
                var inferred = TypeInference.InferFromConstraints(fd.GenericParameters, constraints);
                if (inferred != null)
                {
                    result.FunctionPath = functionPath.AppendGenerics(inferred.Value);
                    providedGenerics = inferred.Value;
                }
                else
                {
                    analyzer.AddException(new CannotInferGenericArgsException(fd.Name, tokenLoc));
                    result.TypePath = fd.ReturnTypePath;
                    return result;
                }
            }
            // Partial inference: explicit () generics provided, [] generics need inference
            else if (fd.ImplicitGenericCount > 0 && providedGenerics.Length > 0
                && providedGenerics.Length == fd.GenericParameters.Length - fd.ImplicitGenericCount)
            {
                var implicitParams = fd.GenericParameters.Take(fd.ImplicitGenericCount).ToImmutableArray();
                var constraints = new List<(LangPath, LangPath)>();
                for (int i = 0; i < fd.Arguments.Length && i < arguments.Length; i++)
                    if (fd.Arguments[i].TypePath != null && arguments[i].TypePath != null)
                        constraints.Add((fd.Arguments[i].TypePath, arguments[i].TypePath));
                // Only add return type constraint if both sides are concrete (not associated types)
                if (expectedReturnType is NormalLangPath && fd.ReturnTypePath is NormalLangPath)
                    constraints.Add((fd.ReturnTypePath, expectedReturnType));

                // Use ALL generic params as free vars for unification (so To in return type
                // can unify with the concrete type), but only build args for the implicit ones.
                var allFreeVars = fd.GenericParameters.Select(gp => gp.Name).ToHashSet();
                var bindings = new Dictionary<string, LangPath>();
                bool unified = true;
                foreach (var (pattern, concrete) in constraints)
                    if (!TypeInference.TryUnify(pattern, concrete, allFreeVars, bindings))
                    { unified = false; break; }

                var inferred = unified
                    ? TypeInference.BuildGenericArgs(implicitParams, bindings)
                    : null;

                if (inferred != null)
                {
                    var allGenerics = inferred.Value.AddRange(providedGenerics);
                    result.FunctionPath = functionPath.AppendGenerics(allGenerics);
                    providedGenerics = allGenerics;
                }
                else
                {
                    analyzer.AddException(new CannotInferGenericArgsException(fd.Name, tokenLoc));
                    result.TypePath = fd.ReturnTypePath;
                    return result;
                }
            }

            if (fd.GenericParameters.Length != providedGenerics.Length)
            {
                analyzer.AddException(new GenericParamCountException(
                    fd.GenericParameters.Length, providedGenerics.Length, tokenLoc));
                result.TypePath = fd.ReturnTypePath;
            }
            else
            {
                result.TypePath = fd.GetMonomorphizedReturnTypePath(result.FunctionPath);
                if (result.TypePath != null)
                    result.TypePath = analyzer.ResolveQualifiedTypePath(result.TypePath);
                var genericArgs = result.FunctionPath.GetFrontGenerics();
                CallExpressionHelper.ValidateGenericBounds(fd, genericArgs, analyzer, tokenLoc);
                CallExpressionHelper.ValidateCallArguments(fd, arguments, genericArgs, analyzer, tokenLoc);
            }
        }
        else
        {
            // Trait method or inherent impl — also handle comptime args
            var traitMethodSig = analyzer.ResolveTraitMethodSignature(functionPath);
            if (traitMethodSig != null && traitMethodSig.GenericParameters.Length > 0)
            {
                // Trait method has comptime generic params — separate type args from runtime args
                var typeArgs = new List<LangPath>();
                var runtimeArgs = new List<IExpression>();
                int comptimeCount = traitMethodSig.GenericParameters.Length;
                for (int argIdx = 0; argIdx < arguments.Length; argIdx++)
                {
                    if (argIdx < comptimeCount)
                    {
                        var typePath = ChainExpression.TryResolveAsType(arguments[argIdx], analyzer);
                        if (typePath != null)
                            typeArgs.Add(typePath);
                        else
                            runtimeArgs.Add(arguments[argIdx]);
                    }
                    else
                    {
                        runtimeArgs.Add(arguments[argIdx]);
                    }
                }
                if (typeArgs.Count > 0)
                {
                    arguments = runtimeArgs.ToImmutableArray();
                    functionPath = functionPath.AppendGenerics(typeArgs);
                    result = new FunctionCallKind
                    {
                        FunctionPath = functionPath,
                        Arguments = arguments,
                        QualifiedAsType = qualifiedAsType,
                    };
                }
            }

            var traitRet = analyzer.ResolveTraitMethodReturnType(functionPath);
            if (traitRet != null)
            {
                result.TypePath = ResolveTraitReturnType(traitRet, functionPath, qualifiedAsType, analyzer, tokenLoc);

                // Set FuncDef for borrow tracking — find the impl method's FunctionDefinition
                if (result.FuncDef == null)
                {
                    var lookup = analyzer.ResolveTraitMethodLookup(functionPath);
                    if (lookup != null)
                    {
                        var concreteType = qualifiedAsType ?? lookup.Value.parentPath;
                        foreach (var impl in analyzer.ImplDefinitions)
                        {
                            var bindings = impl.TryMatchConcreteType(concreteType);
                            if (bindings == null) continue;
                            var implTraitBase = LangPath.StripGenerics(impl.TraitPath);
                            var traitDefPath = (lookup.Value.traitDef as IDefinition).TypePath;
                            if (implTraitBase != null && implTraitBase.Equals(traitDefPath))
                            {
                                var method = impl.GetMethod(lookup.Value.methodName);
                                if (method != null) { result.FuncDef = method; break; }
                            }
                        }
                    }
                }
            }
            else
            {
                var inherentRet = ResolveInherentReturn(result, analyzer);
                if (inherentRet != null) result.TypePath = inherentRet;
                else
                {
                    result.TypePath = LangPath.VoidBaseLangPath;
                    analyzer.AddException(new FunctionNotFoundException(functionPath, tokenLoc));
                }
            }
        }

        // Check for types used as runtime arguments — a type name like `i32`
        // being passed where a value is expected (e.g., Box.New(i32, 0))
        foreach (var arg in result.Arguments)
        {
            if (arg is ChainExpression chain && chain.ResolvedKind is VariableRefKind vrk)
            {
                var argDef = analyzer.GetDefinition(vrk.Path);
                if (argDef is TypeDefinition)
                {
                    analyzer.AddException(new SemanticException(
                        $"'{vrk.Path}' is a type, not a value. " +
                        $"A type cannot be used where a runtime value is expected.\n" +
                        tokenLoc));
                }
            }
        }

        return result;
    }

    private static LangPath? ResolveTraitReturnType(
        LangPath traitRet, NormalLangPath functionPath,
        NormalLangPath? qualifiedAsType, SemanticAnalyzer analyzer, string tokenLoc)
    {
        if (qualifiedAsType != null)
        {
            bool isGenericParam = qualifiedAsType is NormalLangPath nlpQual
                && nlpQual.PathSegments.Length == 1
                && analyzer.IsGenericParam(nlpQual.PathSegments[0].ToString());
            if (!isGenericParam)
            {
                var stripped = functionPath;
                if (stripped.GetFrontGenerics().Length > 0) stripped = stripped.PopGenerics()!;
                var traitPath = stripped.Pop();
                if (traitPath != null && !analyzer.TypeImplementsTrait(qualifiedAsType, traitPath))
                    analyzer.AddException(new TraitBoundViolationException(qualifiedAsType, traitPath));
            }
        }
        if (traitRet is NormalLangPath nlpRet && nlpRet.PathSegments.Length == 1)
        {
            var retName = nlpRet.PathSegments[0].ToString();
            if (retName == "Self" && qualifiedAsType != null) return qualifiedAsType;
            var stripped2 = functionPath;
            if (stripped2.GetFrontGenerics().Length > 0) stripped2 = stripped2.PopGenerics()!;
            var tp = stripped2.Pop();
            LangPath? resolved = null;
            if (qualifiedAsType != null && tp != null)
                resolved = analyzer.ResolveAssociatedType(qualifiedAsType, tp, retName);
            return resolved ?? traitRet;
        }
        if (traitRet is NormalLangPath nlpRet2 && nlpRet2.PathSegments.Length == 2
            && nlpRet2.PathSegments[0] is NormalLangPath.NormalPathSegment firstSeg
            && nlpRet2.PathSegments[1] is NormalLangPath.NormalPathSegment secondSeg
            && firstSeg.Text == "Self")
        {
            var assocName = secondSeg.Text;
            var stripped2 = functionPath;
            if (stripped2.GetFrontGenerics().Length > 0) stripped2 = stripped2.PopGenerics()!;
            var tp = stripped2.Pop();
            LangPath? resolved = null;
            if (qualifiedAsType != null && tp != null)
                resolved = analyzer.ResolveAssociatedType(qualifiedAsType, tp, assocName);
            if (resolved == null && tp != null)
            {
                var parentDef = analyzer.GetDefinition(LangPath.StripGenerics(tp));
                if (parentDef is TraitDefinition)
                {
                    var lookup = analyzer.ResolveTraitMethodLookup(functionPath);
                    if (lookup != null)
                    {
                        var (_, _, parentPath) = lookup.Value;
                        var concreteType = qualifiedAsType ?? parentPath;
                        resolved = analyzer.ResolveAssociatedType(concreteType, (parentDef as IDefinition).TypePath, assocName);
                    }
                }
            }
            return resolved ?? traitRet;
        }
        if (traitRet is QualifiedAssocTypePath qpRet)
        {
            var resolvedQp = qpRet;
            if (qualifiedAsType != null)
            {
                var forType = qpRet.ForType;
                var traitInQp = qpRet.TraitPath;
                if (forType is NormalLangPath nlpFor && nlpFor.PathSegments.Length == 1
                    && nlpFor.PathSegments[0].ToString() == "Self")
                    forType = qualifiedAsType;
                if (traitInQp is NormalLangPath nlpTrait)
                {
                    var newSegs = new List<NormalLangPath.PathSegment>();
                    foreach (var seg in nlpTrait.PathSegments)
                    {
                        if (seg is NormalLangPath.NormalPathSegment { HasGenericArgs: true } nps)
                        {
                            var newTypes = nps.GenericArgs!.Value.Select(tp =>
                                tp is NormalLangPath nlpTp && nlpTp.PathSegments.Length == 1
                                && nlpTp.PathSegments[0].ToString() == "Self" && qualifiedAsType != null
                                    ? qualifiedAsType : tp).ToImmutableArray();
                            newSegs.Add(nps.WithGenericArgs(newTypes));
                        }
                        else newSegs.Add(seg);
                    }
                    traitInQp = new NormalLangPath(nlpTrait.FirstIdentifierToken, newSegs);
                }
                resolvedQp = new QualifiedAssocTypePath(forType, traitInQp, qpRet.AssociatedTypeName);
            }
            return analyzer.ResolveQualifiedTypePath(resolvedQp);
        }
        return analyzer.ResolveQualifiedTypePath(traitRet);
    }

    private static LangPath? ResolveInherentReturn(FunctionCallKind result, SemanticAnalyzer analyzer)
    {
        var workingPath = result.FunctionPath;
        if (workingPath.GetFrontGenerics().Length > 0) workingPath = workingPath.PopGenerics()!;
        if (workingPath.PathSegments.Length < 2) return null;
        var lastSeg = workingPath.GetLastPathSegment();
        if (lastSeg == null) return null;
        var methodName = lastSeg.ToString();
        var parentPath = workingPath.Pop();
        if (parentPath == null || parentPath.PathSegments.Length == 0) return null;

        foreach (var impl in analyzer.ImplDefinitions)
        {
            if (!impl.IsInherent) continue;
            var method = impl.GetMethod(methodName);
            if (method == null) continue;
            var bindings = impl.TryMatchConcreteType(parentPath);
            if (bindings == null && impl.GenericParameters.Length > 0)
            {
                var implBase = LangPath.StripGenerics(impl.ForTypePath);
                var callBase = LangPath.StripGenerics(parentPath);
                if (implBase != null && callBase != null && implBase.Equals(callBase))
                {
                    var constraints = new List<(LangPath, LangPath)>();
                    for (int i = 0; i < method.Arguments.Length && i < result.Arguments.Length; i++)
                        if (method.Arguments[i].TypePath != null && result.Arguments[i].TypePath != null)
                            constraints.Add((method.Arguments[i].TypePath, result.Arguments[i].TypePath));
                    var inferred = TypeInference.InferFromConstraints(impl.GenericParameters, constraints);
                    if (inferred != null)
                    {
                        bindings = new Dictionary<string, LangPath>();
                        for (int i = 0; i < impl.GenericParameters.Length && i < inferred.Value.Length; i++)
                            bindings[impl.GenericParameters[i].Name] = inferred.Value[i];
                    }
                }
            }
            if (bindings == null) continue;
            if (!impl.CheckBounds(bindings, analyzer)) continue;
            if (impl.GenericParameters.Length > 0 && parentPath is NormalLangPath nlpParent
                && nlpParent.GetFrontGenerics().Length == 0)
            {
                var inferredArgs = impl.GenericParameters
                    .Select(gp => bindings.TryGetValue(gp.Name, out var bt) ? bt : null)
                    .Where(a => a != null).ToList();
                if (inferredArgs.Count == impl.GenericParameters.Length)
                {
                    var newParent = nlpParent.AppendGenerics(inferredArgs!);
                    result.FunctionPath = ((NormalLangPath)newParent).Append(methodName);
                }
            }
            result.FuncDef = method;
            return CallExpressionHelper.ResolveReturnTypeFromImpl(
                method.ReturnTypePath, parentPath, bindings, impl.GenericParameters, analyzer);
        }
        return null;
    }


    public ValueRefItem CodeGen(CodeGenContext ctx)
    {
        FunctionRefItem? funcRef;
        if (QualifiedAsType != null)
        {
            var strippedPath = FunctionPath;
            if (strippedPath.GetFrontGenerics().Length > 0)
                strippedPath = strippedPath.PopGenerics()!;
            var traitPath = strippedPath.Pop();
            if (traitPath != null)
            {
                var resolvedType = QualifiedAsType.Monomorphize(ctx);
                funcRef = CallExpressionHelper.ResolveWithTraitBound(
                    ctx, traitPath, resolvedType, FunctionPath);
            }
            else
                funcRef = ctx.GetRefItemFor(FunctionPath) as FunctionRefItem;
        }
        else
            funcRef = ctx.GetRefItemFor(FunctionPath) as FunctionRefItem;

        foreach (var arg in Arguments)
            ctx.TryMarkExpressionDropMoved(arg);

        if (funcRef == null)
            throw new InvalidOperationException(
                $"Cannot resolve function '{FunctionPath}' during codegen. " +
                $"The function path may not be fully monomorphized.");

        var argVals = Arguments.Select(a => a.CodeGen(ctx)).ToArray();
        return CallExpressionHelper.EmitCall(funcRef, argVals, ctx);
    }
}

/// <summary>
/// Method call on a receiver. Recursive — holds its receiver kind.
/// Does its own CodeGen.
/// </summary>
public class MethodCallKind : IChainKind
{
    public required IChainKind Receiver { get; init; }
    public required string MethodName { get; init; }
    public required ImmutableArray<IExpression> Arguments { get; init; }
    public required NormalLangPath ResolvedFunctionPath { get; init; }
    public NormalLangPath? ResolvedQualifiedType { get; init; }
    public RefKind? AutoRefKind { get; init; }
    public bool NeedsAutoDeref { get; init; }
    public int AutoDerefDepth { get; init; }
    public LangPath? ReceiverTypePath { get; init; }
    public string? RootVarName { get; init; }
    public LangPath? TypePath { get; set; }
    public IEnumerable<ISyntaxNode> KindChildren
    {
        get
        {
            foreach (var c in Receiver.KindChildren) yield return c;
            foreach (var a in Arguments) yield return a;
        }
    }

    /// <summary>
    /// Factory: analyze a method call and return a fully resolved MethodCallKind.
    /// Handles auto-deref, auto-ref, impl dispatch, trait bound dispatch.
    /// </summary>
    public static MethodCallKind? AnalyzeCall(
        IChainKind receiver, LangPath receiverType, string methodName,
        ImmutableArray<IExpression> arguments, Token? errorToken,
        string? rootVarName, SemanticAnalyzer analyzer, RefKind? maxCapability = null,
        LangPath? expectedReturnType = null)
    {
        CallExpressionHelper.AnalyzeArgumentsWithReborrow(arguments, analyzer);
        
        var tokenLoc = errorToken?.GetLocationStringRepresentation() ?? "";

        if (receiverType == null)
        {
            analyzer.AddException(new SemanticException(
                $"Cannot call method '{methodName}' on expression with unknown type\n{tokenLoc}"));
            return null;
        }

        // Collect all matching impls for disambiguation
        var candidates = new List<(ImplDefinition impl, FunctionDefinition method,
            Dictionary<string, LangPath> bindings, RefKind? autoRefKind)>();

        foreach (var impl in analyzer.ImplDefinitions)
        {
            var method = impl.GetMethod(methodName);
            if (method == null || method.Arguments.Length == 0 || method.Arguments[0].Name != "self") continue;
            var bindings = impl.TryMatchConcreteType(receiverType);
            if (bindings != null && !impl.CheckBounds(bindings, analyzer)) bindings = null;
            if (bindings == null) continue;

            var autoRefKind = DetectAutoRefKind(method.Arguments[0].TypePath);

            if (autoRefKind != null && maxCapability != null
                && !DerefExpression.CanProduceRefKind(maxCapability.Value, autoRefKind.Value))
                continue;

            candidates.Add((impl, method, bindings, autoRefKind));
        }

        // Disambiguate: if multiple candidates, prefer one whose return type matches expected
        (ImplDefinition impl, FunctionDefinition method,
            Dictionary<string, LangPath> bindings, RefKind? autoRefKind)? chosen = null;

        if (candidates.Count == 1)
        {
            chosen = candidates[0];
        }
        else if (candidates.Count > 1 && expectedReturnType != null)
        {
            foreach (var c in candidates)
            {
                var resolvedReturn = CallExpressionHelper.ResolveReturnTypeFromImpl(
                    c.method.ReturnTypePath, receiverType, c.bindings, c.impl.GenericParameters, analyzer);
                if (resolvedReturn == expectedReturnType)
                {
                    chosen = c;
                    break;
                }
            }
            // Fallback to first if no return type match
            chosen ??= candidates[0];
        }
        else if (candidates.Count > 1)
        {
            chosen = candidates[0];
        }

        if (chosen != null)
        {
            var (cImpl, cMethod, cBindings, cAutoRefKind) = chosen.Value;
            CheckImplicitBorrow(cAutoRefKind, rootVarName, analyzer);
            CallExpressionHelper.CheckSelfMove(cAutoRefKind, rootVarName, receiverType, analyzer);
            CallExpressionHelper.ValidateCallArguments(cMethod, arguments,
                ImmutableArray<LangPath>.Empty, analyzer, tokenLoc, selfOffset: 1);
            var dispatchPath = cImpl.IsInherent ? cImpl.ForTypePath : cImpl.TraitPath;
            if (dispatchPath is NormalLangPath nlpTrait)
                return new MethodCallKind
                {
                    Receiver = receiver, MethodName = methodName, Arguments = arguments,
                    ResolvedFunctionPath = nlpTrait.Append(methodName),
                    ResolvedQualifiedType = receiverType as NormalLangPath, AutoRefKind = cAutoRefKind,
                    ReceiverTypePath = receiverType, RootVarName = rootVarName,
                    TypePath = CallExpressionHelper.ResolveReturnTypeFromImpl(
                        cMethod.ReturnTypePath, receiverType, cBindings, cImpl.GenericParameters, analyzer),
                };
        }

        // Search trait bounds for generic types
        if (receiverType is NormalLangPath nlpReceiver && nlpReceiver.PathSegments.Length == 1)
        {
            var paramName = nlpReceiver.PathSegments[0].ToString();
            foreach (var td in analyzer.GetTraitBoundsFor(paramName))
            {
                var method = td.GetMethod(methodName);
                if (method == null || method.Parameters.Length == 0 || method.Parameters[0].Name != "self") continue;
                var autoRefKind = DetectAutoRefKind(method.Parameters[0].TypePath);
                CheckImplicitBorrow(autoRefKind, rootVarName, analyzer);
                CallExpressionHelper.CheckSelfMove(autoRefKind, rootVarName, receiverType, analyzer);
                var traitPath = (td as IDefinition).TypePath;
                if (traitPath is NormalLangPath nlpTrait)
                    return new MethodCallKind
                    {
                        Receiver = receiver, MethodName = methodName, Arguments = arguments,
                        ResolvedFunctionPath = nlpTrait.Append(methodName),
                        ResolvedQualifiedType = receiverType as NormalLangPath, AutoRefKind = autoRefKind,
                        ReceiverTypePath = receiverType, RootVarName = rootVarName,
                        TypePath = CallExpressionHelper.IsSelfReturnType(method.ReturnTypePath)
                            ? receiverType : analyzer.ResolveQualifiedTypePath(method.ReturnTypePath),
                    };
            }
        }

        // Auto-deref chain
        if (receiverType != null)
        {
            var receiverTraitPath = SemanticAnalyzer.ReceiverTraitPath;
            var currentType = receiverType;
            var visited = new HashSet<LangPath>();
            int derefDepth = 0;
            // Start with the field access chain's capability if present —
            // accessing through &shared narrows what the deref chain can produce
            RefKind? chainCap = maxCapability;

            while (derefDepth < 10 && analyzer.TypeImplementsTrait(currentType, receiverTraitPath))
            {
                if (!visited.Add(currentType)) break;
                var targetType = analyzer.ResolveAssociatedType(currentType, receiverTraitPath, "Target");
                if (targetType == null) break;
                var levelCap = GetDerefCapability(currentType, analyzer);
                if (levelCap != null)
                    chainCap = chainCap == null ? levelCap.Value : MinRefKindCapability(chainCap.Value, levelCap.Value);
                derefDepth++;

                foreach (var impl in analyzer.ImplDefinitions)
                {
                    var method = impl.GetMethod(methodName);
                    if (method == null || method.Arguments.Length == 0 || method.Arguments[0].Name != "self") continue;
                    var bindings = impl.TryMatchConcreteType(targetType);
                    if (bindings != null && !impl.CheckBounds(bindings, analyzer)) bindings = null;
                    if (bindings == null) continue;

                    var autoRefKind = DetectAutoRefKind(method.Arguments[0].TypePath);
                    if (autoRefKind != null && chainCap != null
                        && !DerefExpression.CanProduceRefKind(chainCap.Value, autoRefKind.Value))
                    {
                        analyzer.AddException(new SemanticException(
                            $"Cannot call method '{methodName}' taking '&{RefTypeDefinition.GetRefName(autoRefKind.Value)} Self' " +
                            $"through receiver of type '{receiverType}' (deref chain supports up to " +
                            $"'&{RefTypeDefinition.GetRefName(chainCap.Value)}')\n{tokenLoc}"));
                        return null;
                    }

                    CallExpressionHelper.CheckSelfMove(autoRefKind, rootVarName, receiverType, analyzer);
                    CallExpressionHelper.ValidateCallArguments(method, arguments,
                        ImmutableArray<LangPath>.Empty, analyzer, tokenLoc, selfOffset: 1);
                    var dispatchPath = impl.IsInherent ? impl.ForTypePath : impl.TraitPath;
                    if (dispatchPath is NormalLangPath nlpTrait)
                        return new MethodCallKind
                        {
                            Receiver = receiver, MethodName = methodName, Arguments = arguments,
                            ResolvedFunctionPath = nlpTrait.Append(methodName),
                            ResolvedQualifiedType = targetType as NormalLangPath, AutoRefKind = autoRefKind,
                            NeedsAutoDeref = true, AutoDerefDepth = derefDepth,
                            ReceiverTypePath = receiverType, RootVarName = rootVarName,
                            TypePath = CallExpressionHelper.ResolveReturnTypeFromImpl(
                                method.ReturnTypePath, targetType, bindings, impl.GenericParameters, analyzer),
                        };
                }
                currentType = targetType;
            }
        }

        analyzer.AddException(new SemanticException(
            $"No method '{methodName}' found for type '{receiverType}'\n{tokenLoc}"));
        return null;
    }

    private static RefKind? DetectAutoRefKind(LangPath? selfParamType)
        => RefTypeDefinition.TryExtractRefKindFromPath(selfParamType);

    private static void CheckImplicitBorrow(RefKind? autoRefKind, string? receiverVarName, SemanticAnalyzer analyzer)
    {
        if (autoRefKind == null || receiverVarName == null) return;
        analyzer.InvalidateConflictingBorrows(receiverVarName, autoRefKind.Value);
    }

    private static RefKind? GetReceiverAccessCapability(IExpression expr)
    {
        RefKind? cap = null;
        var current = expr;
        while (current is FieldAccessExpression fae)
        {
            var callerType = fae.Caller.TypePath;
            var rk = RefTypeDefinition.TryExtractRefKindFromPath(callerType);
            if (rk != null)
            { cap = cap == null ? rk : MinRefKindCapability(cap.Value, rk.Value); }
            current = fae.Caller;
        }
        return cap;
    }

    internal static RefKind MinRefKindCapability(RefKind a, RefKind b)
    {
        if (a == RefKind.Shared || b == RefKind.Shared) return RefKind.Shared;
        if (a == RefKind.Uniq) return b;
        if (b == RefKind.Uniq) return a;
        if (a == b) return a;
        return RefKind.Shared;
    }

    internal static RefKind? GetDerefCapability(LangPath? typePath, SemanticAnalyzer analyzer)
    {
        if (typePath == null) return null;
        var pointee = DerefExpression.TryGetPointeeType(typePath, out var isRef, out var ptrKind);
        if (pointee != null)
        {
            if (isRef)
            {
                return RefTypeDefinition.TryExtractRefKindFromPath(typePath) ?? RefKind.Shared;
            }
            return ptrKind;
        }
        if (analyzer.TypeImplementsTrait(typePath, SemanticAnalyzer.DerefUniqTraitPath)) return RefKind.Uniq;
        if (analyzer.TypeImplementsTrait(typePath, SemanticAnalyzer.DerefMutTraitPath)) return RefKind.Mut;
        if (analyzer.TypeImplementsTrait(typePath, SemanticAnalyzer.DerefConstTraitPath)) return RefKind.Const;
        if (analyzer.TypeImplementsTrait(typePath, SemanticAnalyzer.DerefTraitPath)) return RefKind.Shared;
        return null;
    }


    public ValueRefItem CodeGen(CodeGenContext ctx)
    {
        var receiverVal = Receiver.CodeGen(ctx);

        // Auto-deref chain
        if (NeedsAutoDeref)
            for (int d = 0; d < AutoDerefDepth; d++)
                receiverVal = DerefExpression.EmitDeref(ctx, receiverVal);

        // Auto-ref wrapping
        ValueRefItem selfArg;
        if (AutoRefKind != null)
        {
            var refTypePath = RefTypeDefinition.GetRefModule()
                .Append(RefTypeDefinition.GetRefName(AutoRefKind.Value))
                .AppendGenerics([ReceiverTypePath!]);
            var refTypeRef = ctx.GetRefItemFor(refTypePath) as TypeRefItem;
            if (refTypeRef?.Type is RefType refType)
            {
                var alloca = ctx.Builder.BuildAlloca(refType.TypeRef);
                ctx.Builder.BuildStore(receiverVal.ValueRef, alloca);
                selfArg = new ValueRefItem { Type = refType, ValueRef = alloca };
            }
            else
                selfArg = receiverVal;
        }
        else
            selfArg = receiverVal;

        // Resolve method
        NormalLangPath concreteMethodPath;
        if (ResolvedQualifiedType is NormalLangPath nlpConc)
            concreteMethodPath = nlpConc.Append(MethodName);
        else
            concreteMethodPath = ResolvedFunctionPath;

        var funcRef = ctx.GetRefItemFor(concreteMethodPath) as FunctionRefItem;
        funcRef ??= ctx.ResolveTraitMethodCall(concreteMethodPath) as FunctionRefItem;
        funcRef ??= ctx.ResolveTraitMethodCall(ResolvedFunctionPath) as FunctionRefItem;

        if (funcRef == null)
            throw new InvalidOperationException(
                $"Cannot resolve method '{MethodName}' during codegen");

        // self + explicit args
        var allArgs = new List<ValueRefItem> { selfArg };
        foreach (var arg in Arguments)
            allArgs.Add(arg.CodeGen(ctx));

        return CallExpressionHelper.EmitCall(funcRef, allArgs, ctx);
    }

    public List<(string sourceName, RefKind refKind)> GetBorrowSources(SemanticAnalyzer analyzer)
    {
        var results = new List<(string, RefKind)>();
        if (!RefTypeDefinition.IsReferenceType(TypePath)) return results;
        var refKind = RefTypeDefinition.ExtractRefKindFromPath(TypePath!);
        if (RootVarName != null)
            results.Add((RootVarName, refKind));
        return results;
    }
}

// ─── ChainExpression ─────────────────────────────────────────────────

public class ChainExpression : IExpression
{
    /// <summary>Identifier-rooted chain (most common): foo.bar.baz()</summary>
    public ChainExpression(IdentifierToken rootToken, IEnumerable<ChainStep> steps)
    {
        RootToken = rootToken;
        RootName = rootToken.Identity;
        Steps = steps.ToImmutableArray();
        ResolvedRootPath = new NormalLangPath(rootToken, [RootName]);
    }

    /// <summary>
    /// Expression-rooted chain: (expr).method(args) or (Type as Trait).method(args).
    /// The root expression is already analyzed — chain starts from Value state.
    /// </summary>
    public ChainExpression(IExpression rootExpr, IEnumerable<ChainStep> steps,
        NormalLangPath? qualifiedAsType = null)
    {
        RootExpression = rootExpr;
        QualifiedAsType = qualifiedAsType;
        RootToken = rootExpr.Token as IdentifierToken;
        RootName = "";
        Steps = steps.ToImmutableArray();
        ResolvedRootPath = new NormalLangPath(RootToken, []);
    }

    public IdentifierToken? RootToken { get; }
    public string RootName { get; }
    public ImmutableArray<ChainStep> Steps { get; }
    public IChainKind? ResolvedKind { get; set; }

    /// <summary>Non-identifier root expression (for expr.method() chains).</summary>
    public IExpression? RootExpression { get; }
    
    /// <summary>For (Type as Trait).method() calls — the concrete type.</summary>
    public NormalLangPath? QualifiedAsType { get; set; }

    /// <summary>
    /// Propagated from LetStatement when the declared type is known.
    /// Used for generic inference.
    /// </summary>
    public LangPath? ExpectedReturnType { get; set; }

    /// <summary>
    /// After ResolvePaths, this holds the fully resolved root path
    /// (e.g., "Alloc" → "Std.Alloc.Alloc" via use imports).
    /// Before resolution, it's just the raw root name.
    /// </summary>
    public NormalLangPath ResolvedRootPath { get; private set; }

    /// <summary>
    /// If this chain is just a bare identifier (no steps), returns the root name.
    /// Used as a drop-in for the old PathExpression single-segment check.
    /// </summary>
    public string? SimpleVariableName => Steps.Length == 0 ? RootName : null;

    public LangPath? TypePath => ResolvedKind?.TypePath;
    public Token Token => (Token?)RootToken ?? RootExpression?.Token;
    public bool HasGuaranteedExplicitReturn => false;

    public IEnumerable<ISyntaxNode> Children
    {
        get
        {
            if (RootExpression != null) yield return RootExpression;
            foreach (var step in Steps)
                if (step is CallStep call)
                    foreach (var arg in call.Arguments)
                        yield return arg;
        }
    }

    public void ResolvePaths(PathResolver resolver)
    {
        // For identifier roots, resolve through use imports
        if (RootExpression == null)
        {
            var rawPath = new NormalLangPath(RootToken, [RootName]);
            ResolvedRootPath = (NormalLangPath)rawPath.Resolve(resolver);
        }

        // Resolve qualified-as type path (e.g., i32 → Std.Primitive.i32)
        if (QualifiedAsType != null)
            QualifiedAsType = (NormalLangPath)QualifiedAsType.Resolve(resolver);

        // Resolve sub-expressions (including RootExpression if present)
        foreach (var child in Children.OfType<IPathResolvable>())
            child.ResolvePaths(resolver);
    }

    // ── Parsing ──────────────────────────────────────────────────────

    public static ChainExpression Parse(Parser parser)
    {
        var root = Identifier.Parse(parser);
        var steps = new List<ChainStep>();

        while (true)
        {
            var next = parser.Peek();
            if (next is LeftParenthesisToken callTok)
            {
                Parenthesis.ParseLeft(parser);
                var args = new List<IExpression>();
                while (parser.Peek() is not RightParenthesisToken)
                {
                    args.Add(IExpression.Parse(parser));
                    if (parser.Peek() is CommaToken) parser.Pop();
                }
                Parenthesis.ParseRight(parser);
                steps.Add(new CallStep
                {
                    Arguments = args.ToImmutableArray(),
                    Token = (Token)callTok
                });
            }
            else if (next is DotToken)
            {
                parser.Pop();
                var memberIdent = Identifier.Parse(parser);
                steps.Add(new AccessStep
                {
                    Name = memberIdent.Identity,
                    IdentifierToken = memberIdent,
                    Token = memberIdent
                });
            }
            else
            {
                break;
            }
        }

        return new ChainExpression(root, steps);
    }

    // ── Semantic Analysis (state machine) ────────────────────────────

    private enum ResolveState { Value, Type, Function }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // ── Expression root: (expr).method() or (Type as Trait).method() ──
        if (RootExpression != null)
        {
            AnalyzeExpressionRoot(analyzer);
            return;
        }

        var rootPath = ResolvedRootPath;

        // ── Resolve root ──
        var varType = analyzer.GetVariableTypePath(rootPath);
        var definition = analyzer.GetDefinition(rootPath);
        // Fallback: if root is a bare name (unresolved) and not found, search by name.
        if (definition == null && rootPath is NormalLangPath { PathSegments.Length: 1 } bareRoot)
        {
            definition = analyzer.FindDefinitionByName(bareRoot.PathSegments[0].ToString());
            if (definition != null)
                rootPath = (NormalLangPath)(definition as IDefinition)!.TypePath;
        }
        var isGenericParam = analyzer.IsGenericParam(RootName);

        ResolveState state;
        LangPath currentTypePath;
        IChainKind? currentKind = null;

        if (isGenericParam)
        {
            state = ResolveState.Type;
            currentTypePath = rootPath;
        }
        else if (varType != null)
        {
            state = ResolveState.Value;
            currentTypePath = varType;
            currentKind = new VariableRefKind { Path = rootPath, TypePath = varType };
            CheckVariableUsage(analyzer, RootName);
        }
        else if (definition != null)
        {
            var defPath = (definition as IDefinition)!.TypePath;
            if (definition is FunctionDefinition)
            {
                state = ResolveState.Function;
                currentTypePath = defPath;
            }
            else
            {
                state = ResolveState.Type;
                currentTypePath = defPath;
            }
        }
        else
        {
            if (Steps.Length == 0)
            {
                analyzer.AddException(new UndefinedVariableException(
                    rootPath, Token.GetLocationStringRepresentation()));
                return;
            }
            state = ResolveState.Type;
            currentTypePath = rootPath;
        }

        // ── Walk steps ──
        int i = 0;
        while (i < Steps.Length)
        {
            var step = Steps[i];

            switch (state, step)
            {
                case (ResolveState.Value, AccessStep access):
                {
                    bool isMethodCall = i + 1 < Steps.Length && Steps[i + 1] is CallStep;
                    if (isMethodCall)
                    {
                        var callStep = (CallStep)Steps[i + 1];
                        DoMethodCall(analyzer, currentKind!, currentTypePath, access, callStep, i);
                        return;
                    }

                    currentKind = DoFieldAccess(analyzer, currentKind!, ref currentTypePath, access);
                    i++;
                    break;
                }

                case (ResolveState.Value, CallStep callOnVal):
                    analyzer.AddException(new SemanticException(
                        $"Cannot call value of type '{currentTypePath}'\n" +
                        callOnVal.Token.GetLocationStringRepresentation()));
                    return;

                case (ResolveState.Type, CallStep callOnType):
                {
                    var genericArgs = ExtractTypeArgs(callOnType, analyzer);
                    if (genericArgs.Length > 0 && currentTypePath is NormalLangPath nlp)
                        currentTypePath = nlp.AppendGenerics(genericArgs);
                    i++;
                    break;
                }

                case (ResolveState.Type, AccessStep accessOnType):
                {
                    var typeDef = analyzer.GetDefinition(LangPath.StripGenerics(currentTypePath));

                    if (typeDef is EnumTypeDefinition enumDef)
                    {
                        var variant = enumDef.GetVariant(accessOnType.Name);
                        if (variant != null)
                        {
                            DoEnumVariant(analyzer, enumDef, variant, currentTypePath,
                                accessOnType, i);
                            return;
                        }
                    }

                    if (i + 1 < Steps.Length && Steps[i + 1] is CallStep staticArgs)
                    {
                        DoStaticMethodCall(analyzer, currentTypePath, accessOnType, staticArgs);
                        return;
                    }

                    if (currentTypePath is NormalLangPath nlpType)
                        currentTypePath = nlpType.Append(accessOnType.Name);
                    i++;
                    break;
                }

                case (ResolveState.Function, CallStep callOnFn):
                {
                    DoFunctionCall(analyzer, currentTypePath, callOnFn, i);
                    return;
                }

                case (ResolveState.Function, AccessStep accOnFn):
                    analyzer.AddException(new SemanticException(
                        $"Cannot access member on function '{currentTypePath}'\n" +
                        accOnFn.Token.GetLocationStringRepresentation()));
                    return;

                default:
                    i++;
                    break;
            }
        }

        // ── Reached end of chain — finalize ──
        switch (state)
        {
            case ResolveState.Value:
                ResolvedKind ??= currentKind ?? new VariableRefKind
                {
                    Path = rootPath,
                    TypePath = currentTypePath
                };
                break;

            case ResolveState.Type:
            case ResolveState.Function:
                ResolvedKind ??= new VariableRefKind
                {
                    Path = currentTypePath,
                    TypePath = currentTypePath
                };
                break;
        }
    }

    /// <summary>
    /// Analyze a chain rooted on an arbitrary expression (not an identifier).
    /// Handles (expr).method(args) and (Type as Trait).method(args).
    /// </summary>
    private void AnalyzeExpressionRoot(SemanticAnalyzer analyzer)
    {
        if (QualifiedAsType != null && RootExpression is PathExpression pe
            && pe.Path is NormalLangPath funcPath)
        {
            // (Type as Trait).method(args) — qualified trait call
            // RootExpression holds the synthesized traitPath.Append(method) path
            // Steps[0] is a CallStep with the args
            if (Steps.Length > 0 && Steps[0] is CallStep call)
            {
                var kind = FunctionCallKind.AnalyzeCall(
                    funcPath, call.Arguments, QualifiedAsType,
                    ExpectedReturnType, RootToken, analyzer);
                ResolvedKind = kind;
            }
            else
            {
                // No call — just a qualified path expression
                ResolvedKind = new VariableRefKind { Path = funcPath, TypePath = funcPath };
            }
            return;
        }

        // General expression root: (expr).field or (expr).method(args)
        // Start in Value state with the root expression as the initial kind
        var currentKind = (IChainKind)new ExpressionRefKind { Expression = RootExpression! };
        var currentTypePath = RootExpression!.TypePath;

        for (int i = 0; i < Steps.Length; i++)
        {
            var step = Steps[i];

            if (step is AccessStep access)
            {
                bool isMethodCall = i + 1 < Steps.Length && Steps[i + 1] is CallStep;
                if (isMethodCall)
                {
                    var callStep = (CallStep)Steps[i + 1];
                    var rootVarName = LetStatement.GetVariableOrigin(RootExpression);
                    var kind = MethodCallKind.AnalyzeCall(
                        currentKind, RootExpression!.TypePath!, access.Name, callStep.Arguments,
                        access.Token, rootVarName, analyzer, expectedReturnType: ExpectedReturnType);
                    if (kind != null) ResolvedKind = kind;
                    return;
                }

                // Field access
                var fa = new FieldAccessExpression(access.IdentifierToken, RootExpression!);
                fa.Analyze(analyzer);
                currentKind = new FieldAccessKind
                {
                    Receiver = currentKind,
                    FieldName = access.Name,
                    TypePath = fa.TypePath,
                    AutoDeref = fa.AutoDeref,
                    AutoDerefDepth = fa.AutoDerefDepth,
                };
                currentTypePath = fa.TypePath ?? currentTypePath;
            }
        }

        ResolvedKind = currentKind;
    }

    // ── Resolution helpers ───────────────────────────────────────────

    private void CheckVariableUsage(SemanticAnalyzer analyzer, string varName)
    {
        analyzer.CheckVariableUsage(varName, new NormalLangPath(RootToken, [varName]),
            Token.GetLocationStringRepresentation());
    }

    private static ImmutableArray<LangPath> ExtractTypeArgs(
        CallStep call, SemanticAnalyzer analyzer)
    {
        return call.Arguments
            .Select(arg => ExtractTypePath(arg))
            .Where(p => p != null)
            .Cast<LangPath>()
            .ToImmutableArray();
    }

    private static LangPath? ExtractTypePath(IExpression expr)
    {
        if (expr is ChainExpression chain)
        {
            // Use resolved root path if available (after ResolvePaths)
            var baseSegments = chain.ResolvedRootPath.PathSegments.ToList();
            
            foreach (var step in chain.Steps)
            {
                if (step is AccessStep access)
                    baseSegments.Add((NormalLangPath.PathSegment)access.Name);
                else if (step is CallStep call)
                {
                    var innerArgs = call.Arguments
                        .Select(ExtractTypePath)
                        .Where(p => p != null)
                        .Cast<LangPath>()
                        .ToImmutableArray();
                    if (innerArgs.Length > 0 && baseSegments[^1] is NormalLangPath.NormalPathSegment lastSeg)
                        baseSegments[^1] = lastSeg.WithGenericArgs(innerArgs);
                }
            }
            return new NormalLangPath(chain.RootToken, baseSegments);
        }

        if (expr is PathExpression pe) return pe.Path;

        return null;
    }

    // ── Concrete resolution methods ──────────────────────────────────

    /// <summary>
    /// Ensures a NormalLangPath has a non-null FirstIdentifierToken for error reporting.
    /// Falls back to this chain's RootToken.
    /// </summary>
    private NormalLangPath EnsureToken(NormalLangPath path)
    {
        if (path.FirstIdentifierToken is not null) return path;
        return new NormalLangPath(RootToken, path.PathSegments);
    }

    private FieldAccessKind DoFieldAccess(SemanticAnalyzer analyzer,
        IChainKind receiver, ref LangPath currentTypePath, AccessStep access)
    {
        var workingType = currentTypePath;
        bool autoDeref = false;
        RefKind? outerRefKind = null;

        // Auto-deref: if type is a reference (&T), unwrap to T and track ref kind
        if (workingType is NormalLangPath nlpRef
            && nlpRef.Contains(RefTypeDefinition.GetRefModule()))
        {
            var generics = nlpRef.GetFrontGenerics();
            if (generics.Length == 1)
            {
                outerRefKind = MethodCallKind.GetDerefCapability(workingType, analyzer);
                workingType = generics[0];
                autoDeref = true;
            }
        }

        // Propagate capability from prior field accesses in the chain
        RefKind? maxCap = outerRefKind;
        if (receiver is FieldAccessKind fak && fak.MaxCapability != null)
            maxCap = maxCap == null ? fak.MaxCapability : MethodCallKind.MinRefKindCapability(maxCap.Value, fak.MaxCapability.Value);

        // Use shared field resolution (walks Receiver/Deref chain, substitutes generics)
        var (fieldType, autoDerefDepth) = FieldAccessExpression.ResolveFieldType(access.Name, workingType, analyzer);

        if (fieldType == null)
        {
            analyzer.AddException(new SemanticException(
                $"Field '{access.Name}' not found on type '{currentTypePath}'\n{access.Token.GetLocationStringRepresentation()}"));
        }

        var kind = new FieldAccessKind
        {
            Receiver = receiver,
            FieldName = access.Name,
            TypePath = fieldType,
            AutoDeref = autoDeref,
            AutoDerefDepth = autoDerefDepth,
            MaxCapability = maxCap,
        };
        ResolvedKind = kind;
        currentTypePath = fieldType ?? currentTypePath;
        return kind;
    }

    private void DoMethodCall(SemanticAnalyzer analyzer,
        IChainKind receiver, LangPath receiverType, AccessStep access, CallStep call, int stepIndex)
    {
        // Propagate field access chain's max capability to the method call
        RefKind? maxCap = null;
        if (receiver is FieldAccessKind fak) maxCap = fak.MaxCapability;

        // Pass expected return type for disambiguation when multiple impls match
        // (e.g., i32 implements TryInto(u8) and TryInto(usize) — need return type to pick)
        var kind = MethodCallKind.AnalyzeCall(
            receiver, receiverType, access.Name, call.Arguments,
            access.Token, RootName, analyzer, maxCap, ExpectedReturnType);

        if (kind == null) return;

        if (stepIndex + 2 < Steps.Length)
            ResolvePostCallChain(analyzer, kind, stepIndex + 2);
        else
            ResolvedKind = kind;
    }

    private void DoFunctionCall(SemanticAnalyzer analyzer,
        LangPath functionPath, CallStep call, int stepIndex)
    {
        if (functionPath is not NormalLangPath nlp)
        {
            analyzer.AddException(new SemanticException(
                $"Invalid function path\n{call.Token.GetLocationStringRepresentation()}"));
            return;
        }

        // Use CallParamLayout to separate comptime type args from runtime args.
        // Layout tells us which positions in () are comptime vs runtime,
        // supporting interleaving: fn foo(T:! type, x: i32, U:! type) → [true, false, true]
        var funcDef = analyzer.GetDefinition(nlp) as FunctionDefinition;

        var typeArgs = new List<LangPath>();
        var runtimeArgs = new List<IExpression>();

        var layout = funcDef?.CallParamLayout ?? [];

        if (layout.Length > 0)
        {
            for (int argIdx = 0; argIdx < call.Arguments.Length; argIdx++)
            {
                if (argIdx < layout.Length && layout[argIdx])
                {
                    // Comptime position — resolve as type
                    var typePath = TryResolveAsType(call.Arguments[argIdx], analyzer);
                    if (typePath != null)
                        typeArgs.Add(typePath);
                    else
                        runtimeArgs.Add(call.Arguments[argIdx]);
                }
                else
                {
                    runtimeArgs.Add(call.Arguments[argIdx]);
                }
            }
        }
        else
        {
            runtimeArgs.AddRange(call.Arguments);
        }

        // Build the function path with generic args in the path segment
        var callPath = nlp;
        if (typeArgs.Count > 0)
            callPath = nlp.AppendGenerics(typeArgs);

        if (callPath.FirstIdentifierToken is null)
            callPath = EnsureToken(callPath);

        var kind = AnalyzeFunctionCall(callPath, runtimeArgs, analyzer);

        if (stepIndex + 1 < Steps.Length)
            ResolvePostCallChain(analyzer, kind, stepIndex + 1);
        else
            ResolvedKind = kind;
    }

    private IChainKind AnalyzeFunctionCall(
        NormalLangPath callPath, IEnumerable<IExpression> args, SemanticAnalyzer analyzer)
    {
        return FunctionCallKind.AnalyzeCall(
            callPath, args.ToImmutableArray(), null,
            ExpectedReturnType, RootToken, analyzer);
    }

    /// <summary>
    /// Tries to resolve an expression as a type reference.
    /// Returns the type path if it's a type/generic param, null if it's a runtime value.
    /// </summary>
    internal static LangPath? TryResolveAsType(IExpression expr, SemanticAnalyzer analyzer)
    {
        // Reference type in comptime position: &const Foo → Std.Reference.const_(Foo)
        if (expr is PointerGetterExpression pge)
        {
            var innerType = TryResolveAsType(pge.PointingTo, analyzer);
            if (innerType != null)
            {
                var refModule = RefTypeDefinition.GetRefModule();
                var refName = RefTypeDefinition.GetRefName(pge.RefKind);
                return refModule.Append(refName).AppendGenerics([innerType]);
            }
            return null;
        }

        // PathExpression — simple path like Foo, i32, Std.Alloc.Box
        if (expr is PathExpression pe && pe.Path is NormalLangPath nlpPath)
        {
            if (nlpPath.PathSegments.Length == 1 && analyzer.IsGenericParam(nlpPath.PathSegments[0].ToString()))
                return nlpPath;
            var def = analyzer.GetDefinition(nlpPath);
            if (def is TypeDefinition or TraitDefinition)
                return (def as IDefinition)!.TypePath;
        }

        if (expr is ChainExpression chain && chain.Steps.Length == 0)
        {
            // Simple identifier — check if it's a generic param or a type
            if (analyzer.IsGenericParam(chain.RootName))
                return new NormalLangPath(chain.RootToken, [chain.RootName]);

            // Use resolved path (after ResolvePaths) so i32 → Std.Primitive.i32 etc.
            var def = analyzer.GetDefinition(chain.ResolvedRootPath);
            if (def is TypeDefinition or TraitDefinition)
                return (def as IDefinition)!.TypePath;
        }
        else if (expr is ChainExpression chainWithSteps)
        {
            // Could be a generic type like Box(i32) — try to extract as type path
            var path = ExtractTypePath(chainWithSteps);
            if (path != null)
            {
                // Verify it resolves to a type
                var basePath = LangPath.StripGenerics(path);
                var def = analyzer.GetDefinition(basePath);
                if (def is TypeDefinition or TraitDefinition)
                    return path;
                if (basePath is NormalLangPath nlpBase && nlpBase.PathSegments.Length == 1
                    && analyzer.IsGenericParam(nlpBase.PathSegments[0].ToString()))
                    return path;
            }
        }
        return null;
    }

    private void DoStaticMethodCall(SemanticAnalyzer analyzer,
        LangPath typePath, AccessStep methodAccess, CallStep call)
    {
        // Build a Type.Method path for static method analysis
        // resolve it through its existing impl resolution logic
        // (handles generic inference, trait dispatch, etc.)
        //
        // e.g., Box.New(42) → path = "Std.Alloc.Box.New"
        //        Box(i32).New(42) → path = "Std.Alloc.Box.(i32).New"
        //
        //   1. Split into parent (Box) and method (New)
        //   2. Search inherent impls
        //   3. Infer generic args from arguments if needed

        NormalLangPath funcPath;
        if (typePath is NormalLangPath nlpType)
            funcPath = nlpType.Append(methodAccess.Name);
        else
            funcPath = new NormalLangPath(null,
                [(NormalLangPath.PathSegment)typePath.ToString()!,
                 (NormalLangPath.PathSegment)methodAccess.Name]);

        funcPath = EnsureToken(funcPath);
        ResolvedKind = AnalyzeFunctionCall(funcPath, call.Arguments, analyzer);
    }

    private void DoEnumVariant(SemanticAnalyzer analyzer,
        EnumTypeDefinition enumDef, EnumVariant variant, LangPath enumTypePath,
        AccessStep access, int stepIndex)
    {
        if (variant.FieldTypes.Length > 0)
        {
            // Tuple variant: Color.Variant(args)
            if (stepIndex + 1 < Steps.Length && Steps[stepIndex + 1] is CallStep tupleArgs)
            {
                var variantPath = EnsureToken(((NormalLangPath)enumTypePath).Append(access.Name));
                ResolvedKind = AnalyzeFunctionCall(variantPath, tupleArgs.Arguments, analyzer);
            }
            else
            {
                analyzer.AddException(new SemanticException(
                    $"Variant '{access.Name}' has fields — use '{access.Name}(...)' syntax\n" +
                    access.Token.GetLocationStringRepresentation()));
            }
        }
        else
        {
            // Unit variant — check if next step provides generic type args
            // e.g., Maybe.Nothing(i32) → (i32) specifies the enum's generic param
            var finalEnumTypePath = enumTypePath;
            if (stepIndex + 1 < Steps.Length && Steps[stepIndex + 1] is CallStep genericArgs)
            {
                var typeArgs = ExtractTypeArgs(genericArgs, analyzer);
                if (typeArgs.Length > 0 && finalEnumTypePath is NormalLangPath nlpEnum)
                    finalEnumTypePath = nlpEnum.AppendGenerics(typeArgs);
            }

            // If enum has unresolved generics, infer from expected return type
            // e.g., Option.None in context expecting Option(i32) → T=i32
            if (finalEnumTypePath is NormalLangPath nlpFinal
                && nlpFinal.GetFrontGenerics().Length == 0
                && enumDef.GenericParameters.Length > 0
                && ExpectedReturnType is NormalLangPath nlpExpected)
            {
                var expectedGenerics = nlpExpected.GetFrontGenerics();
                if (expectedGenerics.Length == enumDef.GenericParameters.Length)
                    finalEnumTypePath = nlpFinal.AppendGenerics(expectedGenerics);
            }

            ResolvedKind = new EnumVariantKind
            {
                EnumDef = enumDef,
                Variant = variant,
                EnumTypePath = finalEnumTypePath,
                TypePath = finalEnumTypePath
            };
        }
    }

    private void ResolvePostCallChain(SemanticAnalyzer analyzer,
        IChainKind callResult, int fromStep)
    {
        if (fromStep >= Steps.Length)
        {
            ResolvedKind = callResult;
            return;
        }

        var step = Steps[fromStep];
        if (step is AccessStep access)
        {
            if (fromStep + 1 < Steps.Length && Steps[fromStep + 1] is CallStep methodCall)
            {
                var kind = MethodCallKind.AnalyzeCall(
                    callResult, callResult.TypePath!, access.Name, methodCall.Arguments,
                    access.Token, RootName, analyzer);
                if (kind != null) ResolvedKind = kind;
            }
            else
            {
                // Field access on the result of a previous call
                var receiverExpr = new PathExpression(new NormalLangPath(RootToken, [RootName]));
                var fa = new FieldAccessExpression(access.IdentifierToken, receiverExpr);
                fa.Analyze(analyzer);
                ResolvedKind = new FieldAccessKind
                {
                    Receiver = callResult,
                    FieldName = access.Name,
                    TypePath = fa.TypePath,
                    AutoDeref = fa.AutoDeref,
                    AutoDerefDepth = fa.AutoDerefDepth,
                };
            }
        }
    }

    // ── CodeGen ──────────────────────────────────────────────────────

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        if (ResolvedKind == null)
            throw new System.InvalidOperationException(
                $"ChainExpression '{this}' was not resolved during semantic analysis");
        return ResolvedKind.CodeGen(codeGenContext);
    }

    public override string ToString() => RootName + string.Join("", Steps);
}
