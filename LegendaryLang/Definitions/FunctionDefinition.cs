using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Definitions;

public class  FunctionDefinition: ITopLevel, IDefinition, IMonomorphizable
{


    
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

    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return BlockExpression.GetAllFunctionsUsed();
    }

    public Token? LookUpToken {get; }
    
    
    public void Analyze(SemanticAnalyzer analyzer)
    {

        ReturnTypePath = ReturnTypePath.GetAsShortCutIfPossible(analyzer);
        foreach (var i in Arguments)
        {
           i.TypePath = i.TypePath?.GetAsShortCutIfPossible(analyzer);
        }

 
        foreach (var i in BlockExpression.SyntaxNodes)
        {
            i.Analyze(analyzer);
        }
    }
    public BlockExpression BlockExpression { get; }
    public readonly ImmutableArray<VariableDefinition> Arguments;
    public string Name { get; }
    public LangPath ReturnTypePath { get; protected set; }

    public FunctionDefinition(string name, IEnumerable<VariableDefinition> variables, LangPath returnTypePath, BlockExpression blockExpression, NormalLangPath module, IEnumerable<GenericParameter> genericParameters, Token? lookUpToken = null)
    {
        Arguments = variables.ToImmutableArray();
        Name = name;
        ReturnTypePath = returnTypePath;
        BlockExpression = blockExpression;
        LookUpToken = lookUpToken;
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
            var name = Identifier.Parse(parser).Identity;
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
            return new FunctionDefinition(name, variables,returnTyp,LegendaryLang.Parse.Expressions.BlockExpression.Parse(parser),parser.File.Module, genericParameters);
        } else
        {
            throw new ExpectedParserException(parser,(ParseType.Fn), token);
        }
    }

    public Token Token { get; private set; }
}