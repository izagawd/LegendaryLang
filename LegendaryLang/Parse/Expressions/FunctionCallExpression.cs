using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class FunctionCallExpression : IExpression
{
    public FunctionCallExpression(NormalLangPath path, IEnumerable<IExpression> arguments)
    {
        Arguments = arguments.ToImmutableArray();
        FunctionPath = path;
    }


    public ImmutableArray<IExpression> Arguments { get; }

    public ImmutableArray<LangPath> GenericArguments =>
        (FunctionPath?.GetLastPathSegment() as NormalLangPath.GenericTypesPathSegment)?.TypePaths ?? [];

    public NormalLangPath FunctionPath { get; set; }

    /// <summary>
    /// Set when parsing &lt;ConcreteType as Trait&gt;::method() — the concrete type (e.g., i32).
    /// Used to substitute Self in trait method return types.
    /// </summary>
    public LangPath? QualifiedAsType { get; set; }

    public IEnumerable<ISyntaxNode> Children => Arguments;

    public Token Token => FunctionPath.FirstIdentifierToken!;

    /// <summary>
    /// When set by LetStatement, the declared type on the LHS.
    /// Used to infer generic return type (e.g., let a: i32 = make()).
    /// </summary>
    public LangPath? ExpectedReturnType { get; set; }

    /// <summary>
    /// Set during Analyze if this is an enum tuple variant construction.
    /// </summary>
    public EnumTypeDefinition? EnumDef { get; set; }
    public EnumVariant? EnumVariant { get; set; }
    public LangPath? EnumTypePath { get; set; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Analyze args first — we need their types for inference
        foreach (var arg in Arguments)
        {
            arg.Analyze(analyzer);
            analyzer.TryMarkExpressionAsMoved(arg);
        }

        // Check for enum tuple variant construction: EnumName::Variant(args) or EnumName::Variant::<Generics>(args)
        if (FunctionPath.PathSegments.Length >= 2)
        {
            // Strip trailing generics (turbofish) if present
            var workingPath = FunctionPath;
            ImmutableArray<LangPath> turbofishGenerics = [];
            if (workingPath.GetFrontGenerics().Length > 0)
            {
                turbofishGenerics = workingPath.GetFrontGenerics();
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
                            EnumDef = enumDef;
                            EnumVariant = variant;

                            // Determine generic args — from turbofish or parent path generics
                            ImmutableArray<LangPath> genericArgs = turbofishGenerics;
                            if (genericArgs.Length == 0 && parentPath is NormalLangPath nlpParent)
                                genericArgs = nlpParent.GetFrontGenerics();

                        // Try to infer generics from arguments if not provided
                        if (genericArgs.Length == 0 && enumDef.GenericParameters.Length > 0)
                        {
                            var constraints = new List<(LangPath, LangPath)>();
                            for (int i = 0; i < variant.FieldTypes.Length && i < Arguments.Length; i++)
                                if (Arguments[i].TypePath != null)
                                    constraints.Add((variant.FieldTypes[i], Arguments[i].TypePath));
                            var inferred = TypeInference.InferFromConstraints(enumDef.GenericParameters, constraints);
                            if (inferred != null)
                                genericArgs = inferred.Value;
                        }

                        // Build enum type path with generics
                        if (genericArgs.Length > 0)
                        {
                            var basePath = (NormalLangPath)enumDef.TypePath;
                            EnumTypePath = basePath.Append(
                                new NormalLangPath.GenericTypesPathSegment(genericArgs));
                        }
                        else
                        {
                            EnumTypePath = enumDef.TypePath;
                        }
                        TypePath = EnumTypePath;

                        // Type-check arguments against variant field types
                        if (variant.FieldTypes.Length != Arguments.Length)
                        {
                            analyzer.AddException(new SemanticException(
                                $"Variant '{variantName}' expects {variant.FieldTypes.Length} field(s), got {Arguments.Length}\n{Token.GetLocationStringRepresentation()}"));
                        }
                        else
                        {
                            for (int i = 0; i < variant.FieldTypes.Length; i++)
                            {
                                var expectedType = variant.FieldTypes[i];
                                if (genericArgs.Length > 0 && enumDef.GenericParameters.Length > 0)
                                    expectedType = FieldAccessExpression.SubstituteGenerics(
                                        expectedType, enumDef.GenericParameters, genericArgs);
                                if (Arguments[i].TypePath != expectedType)
                                    analyzer.AddException(new SemanticException(
                                        $"Variant '{variantName}' field {i} expects type '{expectedType}', found '{Arguments[i].TypePath}'\n{Token.GetLocationStringRepresentation()}"));
                            }
                        }
                        return;
                    }
                }
            }
            }
        }

        var def = analyzer.GetDefinition(FunctionPath);
        var popped = FunctionPath.Pop();
        if (def is null) def = analyzer.GetDefinition(FunctionPath.Pop());
        if (def is FunctionDefinition fd)
        {
            var providedGenerics = FunctionPath.GetFrontGenerics();

            // === TYPE INFERENCE ===
            if (fd.GenericParameters.Length > 0 && providedGenerics.Length == 0)
            {
                var constraints = new List<(LangPath, LangPath)>();

                // Infer from argument types
                for (int i = 0; i < fd.Arguments.Length && i < Arguments.Length; i++)
                {
                    if (fd.Arguments[i].TypePath != null && Arguments[i].TypePath != null)
                        constraints.Add((fd.Arguments[i].TypePath, Arguments[i].TypePath));
                }

                // Infer from expected return type (let a: i32 = foo())
                if (ExpectedReturnType != null && fd.ReturnTypePath != null)
                    constraints.Add((fd.ReturnTypePath, ExpectedReturnType));

                var inferred = TypeInference.InferFromConstraints(fd.GenericParameters, constraints);
                if (inferred != null)
                {
                    FunctionPath = FunctionPath.Append(new NormalLangPath.GenericTypesPathSegment(inferred.Value));
                    providedGenerics = inferred.Value;
                }
                else
                {
                    analyzer.AddException(new CannotInferGenericArgsException(
                        fd.Name, Token.GetLocationStringRepresentation()));
                    TypePath = fd.ReturnTypePath;
                    return;
                }
            }

            if (fd.GenericParameters.Length != providedGenerics.Length)
            {
                analyzer.AddException(new GenericParamCountException(
                    fd.GenericParameters.Length, providedGenerics.Length,
                    Token.GetLocationStringRepresentation()));
                TypePath = fd.ReturnTypePath;
            }
            else
            {
                TypePath = fd.GetMonomorphizedReturnTypePath(FunctionPath);

                // Resolve qualified associated type paths in the monomorphized return type
                if (TypePath != null)
                    TypePath = analyzer.ResolveQualifiedTypePath(TypePath);

                var genericArgs = FunctionPath.GetFrontGenerics();
                for (int i = 0; i < fd.GenericParameters.Length; i++)
                {
                    var gp = fd.GenericParameters[i];
                    foreach (var bound in gp.TraitBounds)
                    {
                        var argType = genericArgs[i];
                        // Substitute generic params in the bound (e.g., Add<T> → Add<i32>)
                        var resolvedBound = FieldAccessExpression.SubstituteGenerics(
                            bound.TraitPath, fd.GenericParameters, genericArgs);
                        if (!analyzer.TypeImplementsTrait(argType, resolvedBound))
                        {
                            analyzer.AddException(new TraitBoundViolationException(argType, resolvedBound));
                        }

                        // Validate associated type constraints (e.g., Output = T)
                        foreach (var (atName, atType) in bound.AssociatedTypeConstraints)
                        {
                            var resolvedAtType = FieldAccessExpression.SubstituteGenerics(
                                atType, fd.GenericParameters, genericArgs);
                            var actualAt = analyzer.ResolveAssociatedType(argType, resolvedBound, atName);
                            if (actualAt != null && actualAt != resolvedAtType)
                            {
                                analyzer.AddException(new SemanticException(
                                    $"Associated type constraint '{atName} = {resolvedAtType}' not satisfied: " +
                                    $"actual '{atName}' is '{actualAt}'\n{Token.GetLocationStringRepresentation()}"));
                            }
                        }
                    }
                }
            }
        }
        else
        {
            var traitMethodReturnType = analyzer.ResolveTraitMethodReturnType(FunctionPath);
            if (traitMethodReturnType != null)
            {
                if (QualifiedAsType != null)
                {
                    bool isGenericParam = QualifiedAsType is NormalLangPath nlpQual
                        && nlpQual.PathSegments.Length == 1
                        && analyzer.IsGenericParam(nlpQual.PathSegments[0].ToString());

                    if (!isGenericParam)
                    {
                        var traitPath = FunctionPath.Pop();
                        if (traitPath != null && !analyzer.TypeImplementsTrait(QualifiedAsType, traitPath))
                        {
                            analyzer.AddException(new TraitBoundViolationException(QualifiedAsType, traitPath));
                        }
                    }
                }

                if (traitMethodReturnType is NormalLangPath nlpRet && nlpRet.PathSegments.Length == 1)
                {
                    var retName = nlpRet.PathSegments[0].ToString();

                    if (retName == "Self" && QualifiedAsType != null)
                    {
                        TypePath = QualifiedAsType;
                    }
                    else
                    {
                        // Try to resolve as an associated type
                        var traitPath = FunctionPath.Pop();
                        LangPath? resolved = null;
                        if (QualifiedAsType != null && traitPath != null)
                        {
                            resolved = analyzer.ResolveAssociatedType(QualifiedAsType, traitPath, retName);
                        }
                        TypePath = resolved ?? traitMethodReturnType;
                    }
                }
                else if (traitMethodReturnType is QualifiedAssocTypePath qpRet)
                {
                    // Substitute Self with the concrete qualified-as type
                    var resolvedQp = qpRet;
                    if (QualifiedAsType != null)
                    {
                        var forType = qpRet.ForType;
                        var traitInQp = qpRet.TraitPath;
                        if (forType is NormalLangPath nlpFor && nlpFor.PathSegments.Length == 1
                            && nlpFor.PathSegments[0].ToString() == "Self")
                            forType = QualifiedAsType;
                        if (traitInQp is NormalLangPath nlpTrait)
                        {
                            var newSegs = new List<NormalLangPath.PathSegment>();
                            foreach (var seg in nlpTrait.PathSegments)
                            {
                                if (seg is NormalLangPath.GenericTypesPathSegment gts)
                                {
                                    var newTypes = gts.TypePaths.Select(tp =>
                                        tp is NormalLangPath nlpTp && nlpTp.PathSegments.Length == 1
                                        && nlpTp.PathSegments[0].ToString() == "Self" && QualifiedAsType != null
                                            ? QualifiedAsType : tp).ToList();
                                    newSegs.Add(new NormalLangPath.GenericTypesPathSegment(newTypes));
                                }
                                else newSegs.Add(seg);
                            }
                            traitInQp = new NormalLangPath(nlpTrait.FirstIdentifierToken, newSegs);
                        }
                        resolvedQp = new QualifiedAssocTypePath(forType, traitInQp, qpRet.AssociatedTypeName);
                    }
                    TypePath = analyzer.ResolveQualifiedTypePath(resolvedQp);
                }
                else
                {
                    TypePath = analyzer.ResolveQualifiedTypePath(traitMethodReturnType);
                }
            }
            else
            {
                TypePath = LangPath.VoidBaseLangPath;
                analyzer.AddException(
                    new FunctionNotFoundException(FunctionPath, Token.GetLocationStringRepresentation()));
            }
        }
    }

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        // Enum tuple variant construction
        if (EnumDef != null && EnumVariant != null && EnumTypePath != null)
        {
            var typeRef = codeGenContext.GetRefItemFor(EnumTypePath) as TypeRefItem;
            var enumType = typeRef?.Type as EnumType;
            if (enumType != null)
            {
                var alloca = codeGenContext.Builder.BuildAlloca(enumType.TypeRef);

                // Store tag
                var tagPtr = codeGenContext.Builder.BuildStructGEP2(enumType.TypeRef, alloca, 0);
                codeGenContext.Builder.BuildStore(
                    LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, (ulong)EnumVariant.Tag, false),
                    tagPtr);

                // Store payload fields
                if (enumType.HasPayloads && Arguments.Length > 0)
                {
                    var payloadPtr = codeGenContext.Builder.BuildStructGEP2(enumType.TypeRef, alloca, 1);
                    var resolved = enumType.GetResolvedVariant(EnumVariant.Name);
                    if (resolved != null)
                    {
                        ulong offset = 0;
                        for (int i = 0; i < Arguments.Length && i < resolved.Value.fieldTypes.Length; i++)
                        {
                            var argVal = Arguments[i].CodeGen(codeGenContext);
                            var fieldType = resolved.Value.fieldTypes[i];

                            var fieldPtr = payloadPtr;
                            if (offset > 0)
                            {
                                fieldPtr = codeGenContext.Builder.BuildGEP2(
                                    LLVMTypeRef.Int8, payloadPtr,
                                    [LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, offset, false)]);
                            }

                            // Use pointer-to-pointer AssignTo — works for both primitives and structs
                            fieldType.AssignTo(codeGenContext,
                                argVal,
                                new ValueRefItem { Type = fieldType, ValueRef = fieldPtr });

                            unsafe
                            {
                                var dataLayout = LLVM.GetModuleDataLayout(codeGenContext.Module);
                                offset += LLVM.StoreSizeOfType(dataLayout, fieldType.TypeRef);
                            }
                        }
                    }
                }

                return new ValueRefItem { Type = enumType, ValueRef = alloca };
            }
        }

        // For <ConcreteType as Trait>::method() calls, push a temporary trait bound
        // so ResolveTraitMethodCall can find the impl
        bool pushedTempBound = false;
        if (QualifiedAsType != null)
        {
            var traitPath = FunctionPath.Pop(); // Trait path (parent of method)
            if (traitPath != null)
            {
                var resolvedType = QualifiedAsType.Monomorphize(codeGenContext);
                codeGenContext.PushTraitBounds([(traitPath, resolvedType)]);
                pushedTempBound = true;
            }
        }

        var zaPath = codeGenContext.GetRefItemFor(FunctionPath) as FunctionRefItem;

        if (pushedTempBound)
            codeGenContext.PopTraitBounds();

        var callResult = codeGenContext.Builder.BuildCall2(zaPath.Function.FunctionType,
            zaPath.Function.FunctionValueRef,
            Arguments.Select(i =>
            {
                var gened = i.CodeGen(codeGenContext);
                return gened.Type.LoadValue(codeGenContext, gened);
            }).ToArray()
        );

        var returnType = zaPath.Function.ReturnType;

        // For pointer/reference return types, the call result IS the raw pointer value —
        // just store it directly. AssignToStack would incorrectly dereference it.
        LLVMValueRef stackPtr;
        if (returnType is PointerType)
        {
            stackPtr = codeGenContext.Builder.BuildAlloca(returnType.TypeRef);
            codeGenContext.Builder.BuildStore(callResult, stackPtr);
        }
        else
        {
            stackPtr = returnType.AssignToStack(codeGenContext, new ValueRefItem
            {
                Type = returnType,
                ValueRef = callResult
            });
        }


        return new ValueRefItem
        {
            Type = returnType,
            ValueRef = stackPtr
        };
    }

    public LangPath? TypePath { get; set; }

    public void ResolvePaths(PathResolver resolver)
    {
        FunctionPath = (NormalLangPath)FunctionPath.Resolve(resolver);
        if (QualifiedAsType != null)
            QualifiedAsType = QualifiedAsType.Resolve(resolver);
        foreach (var i in Children.OfType<IPathResolvable>())
        {
            i.ResolvePaths(resolver);
        }
    }

    public static FunctionCallExpression ParseFunctionCallExpression(Parser parser,
        NormalLangPath normalLangPath)
    {
        var leftParenth = Parenthesis.ParseLeft(parser);
        var currentToken = parser.Peek();
        var expressions = new List<IExpression>();
        while (currentToken is not RightParenthesisToken)
        {
            var expression = IExpression.Parse(parser);
            expressions.Add(expression);
            currentToken = parser.Peek();
            if (currentToken is CommaToken)
            {
                Comma.Parse(parser);
                currentToken = parser.Peek();
            }
        }

        Parenthesis.ParseRight(parser);
        return new FunctionCallExpression(normalLangPath, expressions);
    }

    public bool HasGuaranteedExplicitReturn => Arguments.Any(i => i.HasGuaranteedExplicitReturn);
}