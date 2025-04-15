using System.Text;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;


public interface IExpression : ISyntaxNode
{
    /// <summary>
    /// SHOULD ONLY BE CALLED ONCE PER OBJECT
    /// </summary>
    /// <param name="codeGenContext"></param>
    /// <returns></returns>
    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext);
 
    /// <summary>
    /// Parses field access, struct cration, variable assignment, etc. pretty much anything after a
    /// path (NOTE: a path can be a local or global var. a::b::c is considered a path)
    /// </summary>
    /// <param name="parser"></param>
    /// <returns></returns>
    public static IExpression ParsePossibleIdentPossibilities(Parser parser)
    {
        LangPath parsed = NormalLangPath.Parse(parser);
        var parsedPath = new PathExpression(parsed);
        var next = parser.Peek();
        if (next is EqualityToken)
        {
            return AssignVariableExpression.Parse(parser, parsedPath);
        }

        if (next is LeftCurlyBraceToken)
        {
            if(parsed is NormalLangPath normalLangPath)
                return StructCreationExpression.Parse(parser, normalLangPath);
            throw new Exception("Expected struct creation expression, found something else");
        }


        if (next is LeftParenthesisToken)
        {
            if(parsed is NormalLangPath normalLangPath)
                return  FunctionCallExpression.ParseFunctionCallExpression(parser, normalLangPath);
            throw new Exception("Expected functionc call expression, found something else");
        }
        return parsedPath;
    }

    public LangPath? BaseLangPath { get; }
    public LangPath SetTypePath(SemanticAnalyzer semanticAnalyzer);

    public static IExpression ParsePrimary(Parser parser)
    {
        var token = parser.Peek();
        IExpression expression;
        
        switch (token)
        {
            // Block and grouping
            case LeftCurlyBraceToken:
                expression =  BlockExpression.Parse(parser);
                break;
            // Literals and identifiers
            case ExclamationMarkToken:
                expression = UnaryOperationExpression.Parse(parser); 
                break;
            case NumberToken:
                expression = NumberExpression.Parse(parser);
                break;
            case LeftParenthesisToken:
                expression =  BracketExpression.Parse(parser);
                break;
            case IdentifierToken:
                expression = ParsePossibleIdentPossibilities(parser);
                break;
            case IBoolToken:
                expression = BoolExpression.Parse(parser);
                break;
            // Add more cases as needed,
            default:
                throw new ExpectedParserException(parser,[ParseType.Fn, ParseType.Let, ParseType.LeftParenthesis, ParseType.Identifier],token);
        }
        token = parser.Peek();
        
        while (token is DotToken)
        {
    
            expression = FieldAccessExpression.Parse(parser,expression);
            token = parser.Peek();
            
        

        }

        if (token is EqualityToken)
        {
            expression = AssignVariableExpression.Parse(parser, expression);
        }
        return expression;
    }
    public static IExpression Parse(Parser parser, int minPrec= 0)
    {
        IExpression lhs = ParsePrimary(parser);

        while (true)
        {
            // Peek the next token; if it is an operator, get its precedence.
            var nextToken = parser.Peek();

            // Check if the token is an operator token.
            if (!(nextToken is IOperatorToken opToken))
                break;

            int prec = opToken.Operator.GetPrecedence();
            if (prec < minPrec)
                break;

            // Consume the operator token.
            parser.Pop();

            // For left-associative operators, the next operator must have a precedence greater than the current one.
            // (For right-associative operators – such as exponentiation – you might not add 1.)
            int nextMinPrec = prec + 1;

            // Recursively parse the right-hand side expression.
            IExpression rhs = Parse(parser, nextMinPrec);

            // Build the binary operation node.
            lhs = new BinaryOperationExpression(lhs, opToken, rhs);
        }

        return lhs;
    }
}

public class BracketExpression : IExpression
{
    
    public static BracketExpression Parse(Parser parser)
    {
        var left = Parenthesis.ParseLeft(parser);
        var expr = IExpression.Parse(parser);
        Parenthesis.ParseRight(parser);
        return new BracketExpression(left, expr);
    }
    public IExpression Expression { get; }
    public LeftParenthesisToken LeftParenthesisToken { get; }
    public BracketExpression(LeftParenthesisToken token, IExpression expression)
    {
        LeftParenthesisToken = token;
        Expression = expression;
    }

    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        return Expression.DataRefCodeGen(codeGenContext);
    }


    public LangPath SetTypePath(SemanticAnalyzer semanticAnalyzer)
    {
        return Expression.SetTypePath(semanticAnalyzer);
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }

    public LangPath? BaseLangPath => Expression.BaseLangPath;
    public Token LookUpToken => LeftParenthesisToken;
}

public class BinaryOperationExpression : IExpression
{
    
    public IExpression Left { get; }
    public IExpression Right { get; }
    public IOperatorToken OperatorToken { get;  }

    public BinaryOperationExpression(IExpression left, IOperatorToken @operatorToken, IExpression right) 
    {                                                 
        Left = left;
        Right = right;
        OperatorToken = @operatorToken;
    }


    public VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        throw new NotImplementedException();
    }

    public LangPath? BaseLangPath { get; }

    public LangPath SetTypePath(SemanticAnalyzer semanticAnalyzer)
    {
        throw new NotImplementedException();
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }

    public Token LookUpToken => Left.LookUpToken;
}