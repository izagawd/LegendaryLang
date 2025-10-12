using System.Collections.Immutable;
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
        Token lookUpToken)
    {
        Arguments = variables.ToImmutableArray();
        Name = name;
        ReturnTypePath = returnTypePath;
        BlockExpression = blockExpression;
        Token = lookUpToken;
        Module = module;
        GenericParameters = genericParameters.ToImmutableArray();
    }

    public ImmutableArray<GenericParameter> GenericParameters { get; }


    public int Priority => 3;
    public BlockExpression BlockExpression { get; }


    /// <summary>
    ///     NOTE: This would be the path pre monomorphization. so if the return type is a generic param T, thats exactly what
    ///     it will return.
    /// </summary>
    public LangPath ReturnTypePath { get; protected set; }

    public LangPath FullPath => Module.Append(Name);
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
            // 2. Gather LLVM types for each parameter.
            var paramTypes = new LLVMTypeRef[Arguments.Length];
            for (var i = 0; i < Arguments.Length; i++)
                paramTypes[i] = (context.GetRefItemFor(Arguments[i].TypePath) as TypeRefItem).TypeRef;

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
            };
            
        }
    }


    public void ResolvePaths(PathResolver resolver)
    {
        BlockExpression.ResolvePaths(resolver);
        ReturnTypePath = ReturnTypePath.Resolve(resolver);
        foreach (var i in Arguments)
            i.TypePath = i.TypePath?.Resolve(resolver);
    }


    public IEnumerable<ISyntaxNode> Children => [BlockExpression];


    public Token Token { get; }


    public void Analyze(SemanticAnalyzer analyzer)
    {
        analyzer.AddScope();
        foreach (var i in Arguments)
            analyzer.RegisterVariableType(new NormalLangPath(i.IdentifierToken, [i.Name]), i.TypePath);
        BlockExpression.Analyze(analyzer);
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
                        analyzer.AddException(new SemanticException(
                            $"Return type of function does not match it's definition\nExpected Type: '{ReturnTypePath}'\nFound: '{i.TypePath}\n{i.Token.GetLocationStringRepresentation()}'"));
            }
            else if (ReturnTypePath != LangPath.VoidBaseLangPath)
            {
                analyzer.AddException(new SemanticException(
                    $"Not all paths return a value of the valid type\n{Token.GetLocationStringRepresentation()}'"));
            }
        }
        analyzer.PopScope();
    }

    public void ImplementMonomorphized(CodeGenContext codeGenContext, Function function)
    {
        function.CodeGen(codeGenContext);
    }

    public bool ImplementsLater => true;

    public LangPath? GetMonomorphizedReturnTypePath(NormalLangPath functionLangPath)
    {
        if (! ((NormalLangPath) (this as IDefinition).FullPath).Contains(functionLangPath.PopGenerics())) return null;
        var genericArgs = functionLangPath.GetFrontGenerics();
        if (genericArgs.Length != GenericParameters.Length) return null;
        for (var i = 0; i < GenericParameters.Length; i++)
        {
            var genericParam = GenericParameters[i];
            if (new NormalLangPath(null, [genericParam.Name]) == ReturnTypePath) return genericArgs[i];
        }

        return ReturnTypePath;
    }

    public static FunctionDefinition Parse(Parser parser, NormalLangPath module)
    {
        var genericParameters = new List<GenericParameter>();
        var token = parser.Pop();
        var variables = new List<VariableDefinition>();
        if (token is FnToken)
        {
            var nameToken = Identifier.Parse(parser);
            var name = nameToken.Identity;
            var nextToken = parser.Peek();
            if (nextToken is OperatorToken {OperatorType: Operator.LessThan})
            {
                parser.Pop();
                nextToken = parser.Peek();
                while (nextToken is not OperatorToken {OperatorType: Operator.GreaterThan})
                {
                    var paramIdentifier = Identifier.Parse(parser);
                    nextToken = parser.Peek();
                    genericParameters.Add(new GenericParameter(paramIdentifier));
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

            Parenthesis.ParseLeft(parser);
            nextToken = parser.Peek();
            while (nextToken is not RightParenthesisToken)
            {
                var parameter = VariableDefinition.Parse(parser);
                nextToken = parser.Peek();
                if (parameter.TypePath is null)
                    throw new ExpectedParserException(parser, ParseType.BaseLangPath, parameter.IdentifierToken);
                variables.Add(parameter);
                if (nextToken is CommaToken) parser.Pop();
                nextToken = parser.Peek();
            }

            parser.Pop();
            nextToken = parser.Peek();
            LangPath returnTyp = LangPath.VoidBaseLangPath;
            if (nextToken is RightPointToken)
            {
                parser.Pop();
                returnTyp = LangPath.Parse(parser);
            }

            return new FunctionDefinition(name, variables, returnTyp, BlockExpression.Parse(parser, returnTyp),
                module, genericParameters, nameToken);
        }

        throw new ExpectedParserException(parser, ParseType.Fn, token);
    }
}