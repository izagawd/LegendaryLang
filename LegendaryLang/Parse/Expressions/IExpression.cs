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

    public static IExpression ParsePossibleIdentPossibilities(Parser parser)
    {
        var parsed = NormalLangPath.Parse(parser);
        var parsedPath = new PathExpression(parsed);
        var next = parser.Peek();
        if (next is EqualityToken)
        {
            return AssignVariableExpression.Parse(parser, parsedPath);
        }

        if (next is LeftCurlyBraceToken)
        {
            return StructCreationExpression.Parse(parser, parsed);
        }

        if (next is DotToken)
        {
            var accessExpr = FieldAccessExpression.Parse(parser,parsedPath);
            next = parser.Peek();
            if (next is EqualityToken)
            {
                return AssignVariableExpression.Parse(parser, accessExpr);
            }
            return accessExpr;
        }
        return parsedPath;
    }

    public LangPath? BaseLangPath { get; }
    public LangPath SetTypePath(SemanticAnalyzer semanticAnalyzer);

    public static IExpression ParsePrimary(Parser parser)
    {
        var token = parser.Peek();
        switch (token)
        {
            // Block and grouping
            case LeftCurlyBraceToken:
                return BlockExpression.Parse(parser);
            // Literals and identifiers
            case ExclamationMarkToken:
                return UnaryOperationExpression.Parse(parser); 
            case NumberToken:
                return NumberExpression.Parse(parser);
            case LeftParenthesisToken:
                return BracketExpression.Parse(parser);
            case IdentifierToken:
                return ParsePossibleIdentPossibilities(parser);
            case IBoolToken:
                return BoolExpression.Parse(parser);
            // Add more cases as needed,
            default:
                throw new ExpectedParserException(parser,[ParseType.Fn, ParseType.Let, ParseType.LeftParenthesis, ParseType.Identifier],token);
        }
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