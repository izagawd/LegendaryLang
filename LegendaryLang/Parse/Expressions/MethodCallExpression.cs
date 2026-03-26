using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

/// <summary>
/// Represents a method call: receiver.method(args)
/// Desugars to TraitOrImpl::method(receiver, args) during analysis.
/// </summary>
public class MethodCallExpression : IExpression
{
    public IExpression Receiver { get; }
    public string MethodName { get; }
    public ImmutableArray<IExpression> Arguments { get; }
    public Token Token { get; }

    /// <summary>Resolved during Analyze: the synthesized function path for codegen.</summary>
    public NormalLangPath? ResolvedFunctionPath { get; private set; }
    /// <summary>Resolved during Analyze: the qualified type for trait dispatch.</summary>
    public LangPath? ResolvedQualifiedType { get; private set; }
    /// <summary>Whether the receiver needs auto-ref wrapping for the self parameter.</summary>
    public RefKind? AutoRefKind { get; private set; }

    public MethodCallExpression(IExpression receiver, string methodName,
        ImmutableArray<IExpression> arguments, Token token)
    {
        Receiver = receiver;
        MethodName = methodName;
        Arguments = arguments;
        Token = token;
    }

    public static MethodCallExpression FromFieldAccess(Parser parser, FieldAccessExpression fieldExpr)
    {
        Parenthesis.ParseLeft(parser);
        var args = new List<IExpression>();
        while (parser.Peek() is not RightParenthesisToken)
        {
            args.Add(IExpression.Parse(parser));
            if (parser.Peek() is CommaToken) parser.Pop();
        }
        Parenthesis.ParseRight(parser);

        return new MethodCallExpression(
            fieldExpr.Caller,
            fieldExpr.Field.Identity,
            args.ToImmutableArray(),
            fieldExpr.Token);
    }

    public IEnumerable<ISyntaxNode> Children
    {
        get
        {
            yield return Receiver;
            foreach (var arg in Arguments) yield return arg;
        }
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        Receiver.Analyze(analyzer);
        foreach (var arg in Arguments)
        {
            arg.Analyze(analyzer);
            analyzer.TryMarkExpressionAsMoved(arg);
        }

        var receiverType = Receiver.TypePath;
        if (receiverType == null)
        {
            analyzer.AddException(new SemanticException(
                $"Cannot call method '{MethodName}' on expression with unknown type\n{Token.GetLocationStringRepresentation()}"));
            return;
        }

        // Search all impl definitions for a method with this name
        // where the first parameter is named "self"
        foreach (var impl in analyzer.ImplDefinitions)
        {
            var method = impl.GetMethod(MethodName);
            if (method == null) continue;
            if (method.Arguments.Length == 0) continue;
            if (method.Arguments[0].Name != "self") continue;

            // Check if the impl applies to this receiver type
            var selfParamType = method.Arguments[0].TypePath;
            var bindings = impl.TryMatchConcreteType(receiverType);

            if (bindings != null && !impl.CheckBounds(bindings, analyzer))
                bindings = null;

            if (bindings == null) continue;

            // Determine if auto-ref is needed: self param is &T but receiver is T
            if (selfParamType is NormalLangPath nlpSelf
                && nlpSelf.Contains(RefTypeDefinition.GetRefModule()))
            {
                // Find which ref kind from the self param path
                foreach (RefKind rk in Enum.GetValues(typeof(RefKind)))
                {
                    var refName = RefTypeDefinition.GetRefName(rk);
                    if (nlpSelf.PathSegments.Any(s => s.ToString() == refName))
                    {
                        AutoRefKind = rk;
                        break;
                    }
                }
            }

            // Found a matching impl + method
            // Build the trait path for dispatch
            var traitLookup = impl.TraitPath;
            if (traitLookup is NormalLangPath nlpTrait)
            {
                ResolvedFunctionPath = nlpTrait.Append(MethodName);
                ResolvedQualifiedType = receiverType;

                // Resolve return type
                var returnType = method.ReturnTypePath;
                if (returnType is NormalLangPath nlpRet && nlpRet.PathSegments.Length == 1
                    && nlpRet.PathSegments[0].ToString() == "Self")
                {
                    TypePath = receiverType;
                }
                else
                {
                    TypePath = returnType;
                    // Substitute generics from the impl bindings
                    if (bindings.Count > 0)
                    {
                        var implGPs = impl.GenericParameters;
                        var implArgs = implGPs.Select(gp =>
                            bindings.TryGetValue(gp.Name, out var bound) ? bound : (LangPath)new NormalLangPath(null, [gp.Name])).ToImmutableArray();
                        TypePath = FieldAccessExpression.SubstituteGenerics(TypePath, implGPs, implArgs);
                    }
                    if (TypePath != null)
                        TypePath = analyzer.ResolveQualifiedTypePath(TypePath);
                }
                return;
            }
        }

        // Also search trait bounds for generic types
        if (receiverType is NormalLangPath nlpReceiver && nlpReceiver.PathSegments.Length == 1)
        {
            var paramName = nlpReceiver.PathSegments[0].ToString();
            foreach (var td in analyzer.GetTraitBoundsFor(paramName))
            {
                var method = td.GetMethod(MethodName);
                if (method == null || method.Parameters.Length == 0 || method.Parameters[0].Name != "self")
                    continue;

                // Determine auto-ref from self param type
                var selfParamType = method.Parameters[0].TypePath;
                if (selfParamType is NormalLangPath nlpSelfP && nlpSelfP.Contains(RefTypeDefinition.GetRefModule()))
                {
                    foreach (RefKind rk in Enum.GetValues(typeof(RefKind)))
                    {
                        if (nlpSelfP.PathSegments.Any(s => s.ToString() == RefTypeDefinition.GetRefName(rk)))
                        {
                            AutoRefKind = rk;
                            break;
                        }
                    }
                }

                var traitPath = (td as IDefinition).TypePath;
                if (traitPath is NormalLangPath nlpTrait)
                {
                    ResolvedFunctionPath = nlpTrait.Append(MethodName);
                    ResolvedQualifiedType = receiverType;
                    TypePath = method.ReturnTypePath;
                    if (TypePath is NormalLangPath nlpRet && nlpRet.PathSegments.Length == 1
                        && nlpRet.PathSegments[0].ToString() == "Self")
                        TypePath = receiverType;
                    if (TypePath != null)
                        TypePath = analyzer.ResolveQualifiedTypePath(TypePath);
                    return;
                }
            }
        }

        analyzer.AddException(new SemanticException(
            $"No method '{MethodName}' found for type '{receiverType}'\n{Token.GetLocationStringRepresentation()}"));
    }

    public LangPath? TypePath { get; private set; }

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        if (ResolvedFunctionPath == null)
            throw new InvalidOperationException($"Method '{MethodName}' was not resolved during analysis");

        // Build receiver argument — auto-ref if needed
        var receiverVal = Receiver.CodeGen(codeGenContext);
        ValueRefItem selfArg;

        if (AutoRefKind != null)
        {
            // Wrap receiver in a reference
            var refTypePath = RefTypeDefinition.GetRefModule()
                .Append(RefTypeDefinition.GetRefName(AutoRefKind.Value))
                .Append(new NormalLangPath.GenericTypesPathSegment([Receiver.TypePath!]));
            var refTypeRef = codeGenContext.GetRefItemFor(refTypePath) as TypeRefItem;
            if (refTypeRef?.Type is RefType refType)
            {
                var alloca = codeGenContext.Builder.BuildAlloca(refType.TypeRef);
                codeGenContext.Builder.BuildStore(receiverVal.ValueRef, alloca);
                selfArg = new ValueRefItem { Type = refType, ValueRef = alloca };
            }
            else
            {
                selfArg = receiverVal;
            }
        }
        else
        {
            selfArg = receiverVal;
        }

        // Synthesize the function call: ConcreteType::method(self, args...)
        // Use the concrete receiver type, not the trait path, for impl resolution
        NormalLangPath concreteMethodPath;
        if (ResolvedQualifiedType is NormalLangPath nlpConc)
            concreteMethodPath = nlpConc.Append(MethodName);
        else
            concreteMethodPath = ResolvedFunctionPath!;

        var funcRefItem = codeGenContext.GetRefItemFor(concreteMethodPath) as FunctionRefItem;

        if (funcRefItem == null)
        {
            // Try trait method resolution with concrete type path
            var traitResult = codeGenContext.ResolveTraitMethodCall(concreteMethodPath);
            funcRefItem = traitResult as FunctionRefItem;
        }

        if (funcRefItem == null)
        {
            // Fallback: try with the trait path directly
            var traitResult = codeGenContext.ResolveTraitMethodCall(ResolvedFunctionPath!);
            funcRefItem = traitResult as FunctionRefItem;
        }

        if (funcRefItem == null)
            throw new InvalidOperationException($"Cannot resolve method '{MethodName}' during codegen");

        // Build argument list: self + explicit args
        var allArgs = new List<LLVMValueRef>();
        allArgs.Add(selfArg.Type.LoadValue(codeGenContext, selfArg));
        foreach (var arg in Arguments)
        {
            var argVal = arg.CodeGen(codeGenContext);
            allArgs.Add(argVal.Type.LoadValue(codeGenContext, argVal));
        }

        var callResult = codeGenContext.Builder.BuildCall2(
            funcRefItem.Function.FunctionType,
            funcRefItem.Function.FunctionValueRef,
            allArgs.ToArray());

        var returnType = funcRefItem.Function.ReturnType;

        LLVMValueRef stackPtr;
        if (returnType is RefType)
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

    public bool HasGuaranteedExplicitReturn => false;

    public void ResolvePaths(PathResolver resolver)
    {
        if (Receiver is IPathResolvable rpr) rpr.ResolvePaths(resolver);
        foreach (var arg in Arguments.OfType<IPathResolvable>())
            arg.ResolvePaths(resolver);
    }
}
