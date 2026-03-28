using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Parse.Statements;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions;

public class FunctionDefinition : IItem, IDefinition, IAnalyzable, IPathResolvable, IMonomorphizable
{
    public readonly ImmutableArray<VariableDefinition> Arguments;

    public FunctionDefinition(string name, IEnumerable<VariableDefinition> variables, LangPath returnTypePath,
        BlockExpression blockExpression, NormalLangPath module, IEnumerable<GenericParameter> genericParameters,
        Token lookUpToken, IEnumerable<string>? lifetimeParameters = null,
        Dictionary<int, string>? argumentLifetimes = null, string? returnLifetime = null)
    {
        Arguments = variables.ToImmutableArray();
        Name = name;
        ReturnTypePath = returnTypePath;
        BlockExpression = blockExpression;
        Token = lookUpToken;
        Module = module;
        GenericParameters = genericParameters.ToImmutableArray();
        LifetimeParameters = lifetimeParameters?.ToImmutableArray() ?? [];
        ArgumentLifetimes = argumentLifetimes ?? new();
        ReturnLifetime = returnLifetime;
    }

    public ImmutableArray<GenericParameter> GenericParameters { get; }
    
    /// <summary>Lifetime parameters declared in the function signature (e.g., 'a, 'b).</summary>
    public ImmutableArray<string> LifetimeParameters { get; }
    /// <summary>Maps argument index to its lifetime annotation name.</summary>
    public Dictionary<int, string> ArgumentLifetimes { get; }
    /// <summary>Lifetime annotation on the return type, if any.</summary>
    public string? ReturnLifetime { get; }


    public int Priority => 3;
    public BlockExpression BlockExpression { get; }


    /// <summary>
    ///     NOTE: This would be the path pre monomorphization. so if the return type is a generic param T, thats exactly what
    ///     it will return.
    /// </summary>
    public LangPath ReturnTypePath { get; protected set; }

    public LangPath TypePath => Module.Append(Name);
    public NormalLangPath Module { get; }
    public bool HasBeenGened { get; set; }
    public string Name { get; }




    public IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        unsafe
        {
            context.AddScope();
            for (int i = 0; i < genericArguments.Length; i++)
            {
                context.AddToDeepestScope(new NormalLangPath(null,[GenericParameters[i].Name]),
                    context.GetRefItemFor(genericArguments[i])! );
            }

            var returnTypeRefItem = context.GetRefItemFor(ReturnTypePath) as TypeRefItem;

            var ReturnType = returnTypeRefItem.Type;

            var FullPath = Module.Append(Name, new NormalLangPath.GenericTypesPathSegment(
                genericArguments.Select(i => i.Monomorphize(context))));

            // 1. Determine the LLVM return type.
            var llvmReturnType = returnTypeRefItem.TypeRef;
            // 2. Gather LLVM types for each parameter — also capture the resolved Type objects
            var paramTypes = new LLVMTypeRef[Arguments.Length];
            var resolvedArgTypes = new Type[Arguments.Length];
            for (var i = 0; i < Arguments.Length; i++)
            {
                var argTypeRef = context.GetRefItemFor(Arguments[i].TypePath) as TypeRefItem;
                paramTypes[i] = argTypeRef.TypeRef;
                resolvedArgTypes[i] = argTypeRef.Type;
            }

            LLVMTypeRef functionType;
            // 3. Create the function type and add the function to the module.
            fixed (LLVMTypeRef* llvmFunctionType = paramTypes)
            {
                functionType = LLVM.FunctionType(llvmReturnType, (LLVMOpaqueType**)llvmFunctionType,
                    (uint)paramTypes.Length, 0);
            }
            
            LLVMValueRef function = context.Module.AddFunction(FullPath.ToString(), functionType);

            var FunctionValueRef = function;
            context.PopScope();
            return new FunctionRefItem()
            {
                Function = new Function(this, genericArguments,FunctionValueRef,functionType,ReturnType, FullPath)
                {
                    ResolvedArgTypes = resolvedArgTypes.ToImmutableArray()
                }
            };
            
        }
    }


    public void ResolvePaths(PathResolver resolver)
    {
        BlockExpression.ResolvePaths(resolver);
        ReturnTypePath = ReturnTypePath.Resolve(resolver);
        foreach (var i in Arguments)
            i.TypePath = i.TypePath?.Resolve(resolver);
        foreach (var gp in GenericParameters)
            for (int i = 0; i < gp.TraitBounds.Count; i++)
                gp.TraitBounds[i] = gp.TraitBounds[i].Resolve(resolver);
    }


    public IEnumerable<ISyntaxNode> Children => [BlockExpression];


    public Token Token { get; }


    public void Analyze(SemanticAnalyzer analyzer)
    {
        analyzer.AddScope();

        // Check for duplicate generic parameter names
        var seen = new HashSet<string>();
        foreach (var gp in GenericParameters)
        {
            if (!seen.Add(gp.Name))
            {
                analyzer.AddException(new SemanticException(
                    $"Duplicate generic parameter name '{gp.Name}'\n{Token.GetLocationStringRepresentation()}"));
            }
        }

        var bounds = GenericParameters
            .SelectMany(gp => gp.TraitBounds.Select(tb => (tb.TraitPath, gp.Name, (Dictionary<string, LangPath>?)(tb.AssociatedTypeConstraints.Count > 0 ? tb.AssociatedTypeConstraints : null))))
            .ToList();
        analyzer.PushTraitBounds(bounds);

        // Resolve qualified associated type paths in return type (e.g., T::Output, <T as Add<T>>::Output)
        ReturnTypePath = analyzer.ResolveQualifiedTypePath(ReturnTypePath);

        // Also resolve argument types
        foreach (var i in Arguments)
        {
            if (i.TypePath != null)
                i.TypePath = analyzer.ResolveQualifiedTypePath(i.TypePath);
        }

        // Lifetime elision check: if the function returns a reference and has multiple
        // reference parameters, the compiler can't determine which input the output
        // borrows from — require explicit lifetime annotations.
        // Exception 1: if there's a 'self' reference parameter, output borrows from self (Rust rule 2).
        // Exception 2: if explicit lifetime annotations are provided, use those instead.
        if (ReturnTypePath is NormalLangPath nlpRetCheck
            && nlpRetCheck.Contains(RefTypeDefinition.GetRefModule()))
        {
            var refParamCount = Arguments.Count(a =>
                a.TypePath is NormalLangPath nlpA && nlpA.Contains(RefTypeDefinition.GetRefModule()));
            var hasSelfRefParam = Arguments.Any(a =>
                a.Name == "self"
                && a.TypePath is NormalLangPath nlpS
                && nlpS.Contains(RefTypeDefinition.GetRefModule()));

            bool hasExplicitLifetimes = ReturnLifetime != null;

            if (hasExplicitLifetimes)
            {
                // Validate: return lifetime must be declared
                if (!LifetimeParameters.Contains(ReturnLifetime!))
                {
                    analyzer.AddException(new SemanticException(
                        $"Undeclared lifetime '{ReturnLifetime}' in return type of function '{Name}'\n" +
                        Token.GetLocationStringRepresentation()));
                }
                // Validate: return lifetime must appear on at least one parameter
                bool returnLifetimeOnParam = ArgumentLifetimes.Values.Any(lt => lt == ReturnLifetime);
                if (!returnLifetimeOnParam)
                {
                    analyzer.AddException(new SemanticException(
                        $"Return lifetime '{ReturnLifetime}' does not appear on any parameter in function '{Name}'\n" +
                        Token.GetLocationStringRepresentation()));
                }
                // Validate: all argument lifetimes are declared
                foreach (var (_, lt) in ArgumentLifetimes)
                {
                    if (!LifetimeParameters.Contains(lt))
                    {
                        analyzer.AddException(new SemanticException(
                            $"Undeclared lifetime '{lt}' in parameter of function '{Name}'\n" +
                            Token.GetLocationStringRepresentation()));
                    }
                }
            }
            else if (refParamCount > 1 && !hasSelfRefParam)
            {
                analyzer.AddException(new SemanticException(
                    $"Function '{Name}' returns a reference but has {refParamCount} reference parameters. " +
                    $"Cannot determine which input the output borrows from — explicit lifetime annotations are required\n" +
                    Token.GetLocationStringRepresentation()));
            }
        }

        foreach (var i in Arguments)
            analyzer.RegisterVariableType(new NormalLangPath(i.IdentifierToken, [i.Name]), i.TypePath);

        // Register parameter names for lifetime analysis
        analyzer.SetFunctionParameters(Arguments.Select(a => a.Name));

        // Register argument lifetimes so the analyzer can look them up by name
        if (LifetimeParameters.Length > 0)
        {
            var paramLifetimeMap = new Dictionary<string, string>();
            for (int i = 0; i < Arguments.Length; i++)
            {
                if (ArgumentLifetimes.TryGetValue(i, out var lt))
                    paramLifetimeMap[Arguments[i].Name] = lt;
            }
            analyzer.SetParameterLifetimes(paramLifetimeMap);
        }

        BlockExpression.Analyze(analyzer);

        // Validate return lifetime: if the function has explicit lifetime annotations,
        // check that the return expression borrows from a parameter with the matching lifetime.
        if (ReturnLifetime != null && BlockExpression.BlockSyntaxNodeContainers.Length > 0)
        {
            // Check implicit return (last expression in block)
            var lastNode = BlockExpression.BlockSyntaxNodeContainers.Last();
            if (lastNode.Node is IExpression lastExpr && !lastNode.HasSemiColonAfter)
                ValidateReturnLifetime(lastExpr, analyzer);

            // Check explicit return statements
            foreach (var node in BlockExpression.SyntaxNodes)
            {
                if (node is ReturnStatement rs && rs.ToReturn != null)
                    ValidateReturnLifetime(rs.ToReturn, analyzer);
            }
        }

        // Check implicit return for dangling references
        if (BlockExpression.BlockSyntaxNodeContainers.Length > 0)
        {
            var lastNode = BlockExpression.BlockSyntaxNodeContainers.Last();
            if (lastNode.Node is IExpression lastExpr
                && BlockExpression.TypePath is NormalLangPath nlpRet
                && nlpRet.Contains(RefTypeDefinition.GetRefModule())
                && analyzer.IsExpressionLocalBorrow(lastExpr))
            {
                analyzer.AddException(new DanglingReferenceException(
                    Token.GetLocationStringRepresentation()));
            }

            // Validate return value lifetime matches declared return lifetime
            if (ReturnLifetime != null && lastNode.Node is IExpression returnExpr)
            {
                ValidateReturnLifetime(returnExpr, analyzer);
            }
        }

        if (BlockExpression.TypePath != ReturnTypePath)
        {
            IEnumerable<ReturnStatement> GuaranteedReturnStatements(ISyntaxNode node)
            {
                if (node is ReturnStatement returnStatement) yield return returnStatement;

                foreach (var i in node.Children.Where(i => 
                                 i is ICanHaveExplicitReturn canHaveExplicitReturn && canHaveExplicitReturn.HasGuaranteedExplicitReturn)
                             .Where(i => i is not IItem))
                foreach (var j in GuaranteedReturnStatements(i))
                    yield return j;
            }

            if (GuaranteedReturnStatements(this).Any())
            {
                var statementsThatDontFollow = GuaranteedReturnStatements(this)
                    .Where(i => i.TypePath != ReturnTypePath).ToArray();
                if (statementsThatDontFollow.Length != 0)
                    foreach (var i in statementsThatDontFollow)
                        analyzer.AddException(new ReturnTypeMismatchException(
                            ReturnTypePath, i.TypePath, i.Token.GetLocationStringRepresentation()));
            }
            else if (ReturnTypePath != LangPath.VoidBaseLangPath)
            {
                analyzer.AddException(new SemanticException(
                    $"Not all paths return a value of the valid type\n{Token.GetLocationStringRepresentation()}'"));
            }
        }
        analyzer.PopTraitBounds();
        analyzer.PopScope();
    }

    public void ImplementMonomorphized(CodeGenContext codeGenContext, Function function)
    {
        function.CodeGen(codeGenContext);
    }

    public bool ImplementsLater => true;

    public LangPath? GetMonomorphizedReturnTypePath(NormalLangPath functionLangPath)
    {
        if (! ((NormalLangPath) (this as IDefinition).TypePath).Contains(functionLangPath.PopGenerics())) return null;
        var genericArgs = functionLangPath.GetFrontGenerics();
        if (genericArgs.Length != GenericParameters.Length) return null;

        // Substitute all generic params in the return type
        return FieldAccessExpression.SubstituteGenerics(ReturnTypePath, GenericParameters, genericArgs);
    }

    public static FunctionDefinition Parse(Parser parser, NormalLangPath module)
    {
        var token = parser.Pop();
        if (token is not FnToken)
            throw new ExpectedParserException(parser, ParseType.Fn, token);

        var nameToken = Identifier.Parse(parser);

        // Parse generic parameters (lifetimes + type params)
        var generics = FunctionSignatureParser.ParseGenericParams(parser);
        var genericParameters = generics?.GenericParameters ?? [];
        var lifetimeParameters = generics?.LifetimeParameters ?? [];

        // Parse function parameters
        var paramsResult = FunctionSignatureParser.ParseFunctionParams(parser);

        // Parse return type
        var returnResult = FunctionSignatureParser.ParseReturnType(parser);

        return new FunctionDefinition(nameToken.Identity, paramsResult.Parameters,
            returnResult.ReturnTypePath, BlockExpression.Parse(parser, returnResult.ReturnTypePath),
            module, genericParameters, nameToken, lifetimeParameters,
            paramsResult.ArgumentLifetimes, returnResult.ReturnLifetime);
    }

    /// <summary>
    /// Validate that an implicit return value's borrow origin has a lifetime matching the declared return lifetime.
    /// E.g., fn bro&lt;'a, 'b&gt;(dd: &amp;'a i32, kk: &amp;'b i32) -&gt; &amp;'b i32 { dd } — error because dd has 'a not 'b.
    /// </summary>
    private void ValidateReturnLifetime(IExpression returnExpr, SemanticAnalyzer analyzer)
    {
        // Find the origin parameter name of the returned value
        string? originParam = null;

        if (returnExpr is PathExpression pe && pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            var varName = nlp.PathSegments[0].ToString();
            // Is it directly a parameter?
            if (Arguments.Any(a => a.Name == varName))
                originParam = varName;
            else
            {
                // Trace through borrows to find the ultimate source parameter
                var source = analyzer.GetBorrowSource(varName);
                if (source != null && Arguments.Any(a => a.Name == source))
                    originParam = source;
            }
        }

        if (originParam == null) return;

        // Find the parameter's index and its declared lifetime
        for (int i = 0; i < Arguments.Length; i++)
        {
            if (Arguments[i].Name != originParam) continue;
            if (!ArgumentLifetimes.TryGetValue(i, out var paramLifetime)) break;

            if (paramLifetime != ReturnLifetime)
            {
                analyzer.AddException(new SemanticException(
                    $"Function '{Name}' returns a value with lifetime '{paramLifetime}', " +
                    $"but the return type requires lifetime '{ReturnLifetime}'\n" +
                    Token.GetLocationStringRepresentation()));
            }
            break;
        }
    }
}