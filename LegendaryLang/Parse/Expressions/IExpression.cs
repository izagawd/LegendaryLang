using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Statements;

namespace LegendaryLang.Parse.Expressions;

public interface IExpression : IStatement
{
    
    /// <summary>
    ///     Should not be null after semantic analysis
    /// </summary>
    public LangPath? TypePath { get; }


 
    /// <summary>
    ///     SHOULD ONLY BE CALLED ONCE PER OBJECT
    /// </summary>
    /// <param name="codeGenContext"></param>
    /// <returns></returns>
    public ValueRefItem CodeGen(CodeGenContext codeGenContext);

    void IStatement.CodeGen(CodeGenContext codeGenContext)
    {
        CodeGen(codeGenContext);
    }
    /// <summary>
    ///     Parses field access, struct creation, variable assignment, etc. pretty much anything after a
    ///     path (NOTE: a path can be a local or global var. a::b::c is considered a path)
    /// </summary>
    /// <param name="parser"></param>
    /// <returns></returns>
    /// <summary>
    /// When true, ParsePossibleIdentPossibilities won't interpret { as struct creation.
    /// Used by match scrutinee parsing to prevent `match x { ... }` from being parsed as struct literal.
    /// </summary>
    public static bool SuppressStructLiteral { get; set; } = false;

    public static IExpression ParsePossibleIdentPossibilities(Parser parser)
    {
        var parsed = NormalLangPath.Parse(parser);
        var parsedPath = new PathExpression(parsed);
        var next = parser.Peek();
        if (next is EqualityToken) return AssignVariableExpression.Parse(parser, parsedPath);

        if (next is LeftCurlyBraceToken && !SuppressStructLiteral)
        {
            if (parsed is NormalLangPath normalLangPath)
                return StructCreationExpression.Parse(parser, normalLangPath);
            throw new ExpectedParserException(parser, [ParseType.StructPath], parsed.FirstIdentifierToken);
        }


        if (next is LeftParenthesisToken)
        {
            if (parsed is NormalLangPath normalLangPath)
                return FunctionCallExpression.ParseFunctionCallExpression(parser, normalLangPath);
            throw new ExpectedParserException(parser, [ParseType.FunctionCall], parsed.FirstIdentifierToken);
        }

        return parsedPath;
    }


    /// <summary>
    ///     Parses &lt;T as Trait&gt;::method(args) syntax.
    ///     Synthesizes a Trait::method path so the existing resolution machinery handles it.
    /// </summary>
    public static IExpression ParseQualifiedTraitExpression(Parser parser)
    {
        // Consume <
        parser.Pop();
        // Parse type path (e.g., T)
        var typePath = LangPath.Parse(parser, true);
        // Expect 'as'
        var asToken = parser.Pop();
        if (asToken is not AsToken)
            throw new ExpectedParserException(parser, ParseType.As, asToken);
        // Parse trait path
        var traitPath = LangPath.Parse(parser, true);
        // Expect >
        Comparator.ParseGreater(parser);
        // Expect ::
        DoubleColon.Parse(parser);
        // Parse method name
        var methodIdent = Identifier.Parse(parser);

        // Synthesize path: traitPath::methodName
        NormalLangPath synthesizedPath;
        if (traitPath is NormalLangPath nlp)
            synthesizedPath = nlp.Append(methodIdent.Identity);
        else
            throw new ParseException($"Expected a normal path for trait in qualified expression\n{asToken.GetLocationStringRepresentation()}");

        // Parse optional turbofish generics ::<U>
        if (parser.Peek() is DoubleColonToken && parser.PeekAt(1) is OperatorToken { OperatorType: Operator.LessThan })
        {
            parser.Pop(); // consume ::
            parser.Pop(); // consume <
            var typeArgs = new List<LangPath>();
            while (parser.Peek() is not OperatorToken { OperatorType: Operator.GreaterThan })
            {
                typeArgs.Add(LangPath.Parse(parser, true));
                if (parser.Peek() is CommaToken) parser.Pop();
                else break;
            }
            Comparator.ParseGreater(parser);
            synthesizedPath = synthesizedPath.Append(new NormalLangPath.GenericTypesPathSegment(typeArgs));
        }

        // Check if followed by ( for function call
        if (parser.Peek() is LeftParenthesisToken)
        {
            var callExpr = FunctionCallExpression.ParseFunctionCallExpression(parser, synthesizedPath);
            callExpr.QualifiedAsType = typePath;
            return callExpr;
        }

        // Otherwise return as path expression
        return new PathExpression(synthesizedPath);
    }


    public static IExpression ParseBracketOrTuple(Parser parser)
    {
        var leftParenthesis = Parenthesis.ParseLeft(parser);
        var exprs = new List<IExpression> { Parse(parser) };
        var next = parser.Peek();
        IExpression expression = null;
        var doneOnce = true;
        while (next is CommaToken)
        {
            doneOnce = false;
            parser.Pop();
            next = parser.Peek();
            if (next is not RightParenthesisToken)
                exprs.Add(Parse(parser));
            else
                break;
            next = parser.Peek();
        }

        Parenthesis.ParseRight(parser);
        if (doneOnce) return new BracketExpression(leftParenthesis, exprs.First());
        return new TupleCreationExpression(leftParenthesis, exprs);
    }

    public static IExpression ParsePrimary(Parser parser)
    {
        var token = parser.Peek();
        IExpression expression;

        switch (token)
        {
            case AmpersandToken:
                expression = PointerGetterExpression.Parse(parser);
                break;
            case WhileToken:
                expression = WhileExpression.Parse(parser);
                break;
            // Block and grouping
            case LeftCurlyBraceToken:
                expression = BlockExpression.Parse(parser, null);
                break;
            // Literals and identifiers
            case OperatorToken {OperatorType: Operator.ExclamationMark}:
                expression = UnaryOperationExpression.Parse(parser);
                break;
            case OperatorToken {OperatorType: Operator.Multiply}:
                var derefTok = parser.Pop();
                expression = new DerefExpression(ParsePrimary(parser), (Token)derefTok);
                break;
            case OperatorToken {OperatorType: Operator.LessThan}:
                expression = ParseQualifiedTraitExpression(parser);
                break;
            case IfToken:
                expression = IfExpression.Parse(parser);
                break;
            case MatchToken:
                expression = MatchExpression.Parse(parser);
                break;
            case NumberToken:
                expression = NumberExpression.Parse(parser);
                break;
            case OperatorToken:
                var operatorToken = OperatorToken.Parse(parser);
                expression = Parse(parser);
                expression = new UnaryOperationExpression(expression, operatorToken);
                break;
            case LeftParenthesisToken:
                expression = ParseBracketOrTuple(parser);
                break;
            case IdentifierToken:
                expression = ParsePossibleIdentPossibilities(parser);
                break;
            case IBoolToken:
                expression = BoolExpression.Parse(parser);
                break;
            // Add more cases as needed,
            default:
                throw new ExpectedParserException(parser,
                    [ParseType.Fn, ParseType.Let, ParseType.LeftParenthesis, ParseType.Identifier], token);
        }

        token = parser.Peek();

        while (token is DotToken)
        {
            expression = FieldAccessExpression.Parse(parser, expression);
            token = parser.Peek();
        }

        if (token is EqualityToken) expression = AssignVariableExpression.Parse(parser, expression);
        return expression;
    }


    public static IExpression Parse(Parser parser, int minPrec = 0)
    {
        var lhs = ParsePrimary(parser);

        while (true)
        {
            // Peek the next token; if it is an operator, get its precedence.
            var nextToken = parser.Peek();

            // Check if the token is an operator token.
            if (!(nextToken is OperatorToken opToken))
                break;

            var prec = opToken.OperatorType.GetPrecedence();
            if (prec < minPrec)
                break;

            // Consume the operator token.
            parser.Pop();

            // For left-associative operators, the next operator must have a precedence greater than the current one.
            // (For right-associative operators – such as exponentiation – you might not add 1.)
            var nextMinPrec = prec + 1;

            // Recursively parse the right-hand side expression.
            var rhs = Parse(parser, nextMinPrec);

            // Build the binary operation node.
            lhs = new BinaryOperationExpression(lhs, opToken, rhs);
        }

        return lhs;
    }
}