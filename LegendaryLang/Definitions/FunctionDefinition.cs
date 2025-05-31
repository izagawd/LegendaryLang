using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Parse.Statements;
using LegendaryLang.Semantics;

namespace LegendaryLang.Definitions;

public class  FunctionDefinition: ITopLevel, IDefinition, IMonomorphizable, IPathHaver
{


    public IEnumerable<ISyntaxNode> Children => [BlockExpression];
    public ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        var fullPath = (this as IDefinition).FullPath;
        if (fullPath == path)
        {
            return [];
        }

        if (path is NormalLangPath normalLangPath)
        {
            var segment = normalLangPath.GetLastPathSegment();
            if (segment is not NormalLangPath.GenericTypesPathSegment genericTypesPathSegment)
            {
                return null;
            }
            
            if (genericTypesPathSegment.TypePaths.Length != GenericParameters.Length)
            {
                return null;
            }

            if (genericTypesPathSegment.TypePaths.Length == GenericParameters.Length)
            {
                var popped =normalLangPath.Pop();
        
                if (popped is not null && popped == (this as IDefinition).FullPath)
                {
                    var last =normalLangPath.GetLastPathSegment() as NormalLangPath.GenericTypesPathSegment;
                    return last.TypePaths;
                }
                return null;
            }
        }
        return null;

    }

    IConcreteDefinition IMonomorphizable. Monomorphize(CodeGenContext context, LangPath langPath)
    {
        return Monomorphize(context, langPath);
    }
    public Function? Monomorphize(CodeGenContext codeGenContext, LangPath ident)
    {
        var genericArguments = GetGenericArguments(ident);
        if (genericArguments is null)
        {
            return null;
        }
        var func =new Function(this,genericArguments.Value);
        return func;

    }
    public ImmutableArray<GenericParameter> GenericParameters {get; }
    public NormalLangPath Module { get; }
    public bool HasBeenGened { get; set; }




    public int Priority => 3;

    public void SetFullPathOfShortCutsDirectly(SemanticAnalyzer analyzer)
    {
        

        ReturnTypePath = ReturnTypePath.GetFromShortCutIfPossible(analyzer);
        foreach (var i in Arguments)
        {
            i.TypePath = i.TypePath?.GetFromShortCutIfPossible(analyzer);
        }

    }


    public Token Token {get; }
    
    
    public void Analyze(SemanticAnalyzer analyzer)
    {

 

        foreach (var i in Arguments)
        {
            analyzer.RegisterVariableType(new NormalLangPath(i.IdentifierToken,[i.Name]), i.TypePath);
        }
        BlockExpression.Analyze(analyzer);
        if (BlockExpression.TypePath != ReturnTypePath)
        {
            IEnumerable<ReturnStatement> GuaranteedReturnStatements(ISyntaxNode node)
            {
                if (node is IfExpression ifExpression && !ifExpression.EndsWithoutIf)
                {
                    yield break;
                }
                if (node is ReturnStatement returnStatement)
                {
                    yield return returnStatement;
                }

                foreach (var i in node.Children)
                {
                    foreach (var j in GuaranteedReturnStatements(i) )
                    {
                        yield return j;
                    }
             
                }
           
            }

            if (GuaranteedReturnStatements(this).Any())
            {
                var statementsThatDontFollow = GuaranteedReturnStatements(this)
                    .Where(i => i.TypePath != ReturnTypePath).ToArray();
                if (statementsThatDontFollow.Length != 0)
                {
                    foreach (var i in statementsThatDontFollow)
                    {
                        analyzer.AddException(new SemanticException(
                            $"Return type of function does not match it's definition\nExpected Type: '{ReturnTypePath}'\nFound: '{i.TypePath}\n{i.Token.GetLocationStringRepresentation()}'"));

                    }

                }
                
            }
            else if(ReturnTypePath != LangPath.VoidBaseLangPath)
            {
    
                    analyzer.AddException(new SemanticException(
                        $"Not all paths return a value of the valid type\n{Token.GetLocationStringRepresentation()}'"));

                
            }


        }
   
    }
    public BlockExpression BlockExpression { get; }
    public readonly ImmutableArray<VariableDefinition> Arguments;
    public string Name { get; }
    
    
    /// <summary>
    /// NOTE: This would be the path pre monomorphization. so if the return type is a generic param T, thats exactly what it will return. 
    /// </summary>
    public LangPath ReturnTypePath { get; protected set; }

    public LangPath? GetMonomorphizedReturnTypePath(NormalLangPath functionLangPath)
    {
        
        if (!(this as IDefinition).FullPath.Contains(functionLangPath.PopGenerics()))
        {
            return null;
        }
        var genericArgs=functionLangPath.GetFrontGenerics();
        if (genericArgs.Length != GenericParameters.Length)
        {
            return null;
        }
        for(int i = 0; i < GenericParameters.Length; i++)
        {
            var genericParam = GenericParameters[i];
            if (new NormalLangPath(null, [genericParam.Name]) == ReturnTypePath)
            {
                return genericArgs[i];
            }
        }

        return ReturnTypePath;
    }
    public FunctionDefinition(string name, IEnumerable<VariableDefinition> variables, LangPath returnTypePath, BlockExpression blockExpression, NormalLangPath module, IEnumerable<GenericParameter> genericParameters, Token lookUpToken)
    {
        Arguments = variables.ToImmutableArray();
        Name = name;
        ReturnTypePath = returnTypePath;
        BlockExpression = blockExpression;
        Token = lookUpToken;
        Module = module;
        GenericParameters = genericParameters.ToImmutableArray();
    }

    public static FunctionDefinition Parse(Parser parser)
    {
        var genericParameters = new List<GenericParameter>();
        var token = parser.Pop();
        var variables = new List<VariableDefinition>();
        if (token is FnToken)
        {
            var nameToken = Identifier.Parse(parser);
            var name = nameToken.Identity;
            var nextToken = parser.Peek();
            if (nextToken is LessThanToken)
            {
                parser.Pop();
                nextToken = parser.Peek();
                while (nextToken is not GreaterThanToken)
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
                {
                    throw new ExpectedParserException(parser,(ParseType.BaseLangPath), parameter.IdentifierToken);
                }
                variables.Add(parameter);
                if (nextToken is CommaToken)
                {
                    parser.Pop();
                }
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
            return new FunctionDefinition(name, variables,returnTyp,BlockExpression.Parse(parser,returnTyp),parser.File.Module, genericParameters,nameToken);
        } else
        {
            throw new ExpectedParserException(parser,(ParseType.Fn), token);
        }
    }

}