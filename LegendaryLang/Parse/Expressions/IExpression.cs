using System.Collections.Immutable;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Statements;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public interface IExpression : IStatement
{
    
    /// <summary>
    ///     Should not be null after semantic analysis
    /// </summary>
    public LangPath? TypePath { get; }

    /// <summary>
    /// True if this expression produces a fresh temporary value (struct creation, literal, call result).
    /// False if it references an existing place (variable, field, deref). Used to determine if the
    /// value needs a temporary scope for dropping when used as a method receiver.
    /// Must be explicitly implemented — no default, wrong value causes drop bugs.
    /// </summary>
    public bool IsTemporary { get; }


 
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
    ///     Parses `make TypePath { field: value, ... }` struct creation.
    /// </summary>
    public static IExpression ParseMakeExpression(Parser parser)
    {
        parser.Pop(); // consume 'make'
        var typePath = LangPath.Parse(parser, true);
        if (typePath is not NormalLangPath normalPath)
            throw new ExpectedParserException(parser, [ParseType.StructPath], parser.Peek());
        return StructCreationExpression.Parse(parser, normalPath);
    }



    public static IExpression ParseBracketOrTuple(Parser parser)
    {
        var leftParenthesis = Parenthesis.ParseLeft(parser);

        // Empty tuple: ()
        if (parser.Peek() is RightParenthesisToken)
        {
            Parenthesis.ParseRight(parser);
            return new TupleCreationExpression(leftParenthesis, []);
        }

        var exprs = new List<IExpression> { Parse(parser) };
        var next = parser.Peek();

        // Check for qualified trait expression: (Type as Trait)
        if (next is AsToken)
        {
            parser.Pop(); // consume 'as'
            var typePath = ExtractPathFromExpression(exprs.First());
            var traitPath = LangPath.Parse(parser, true);
            Parenthesis.ParseRight(parser);

            // Expect .method after )
            if (parser.Peek() is not DotToken)
                return new BracketExpression(leftParenthesis, exprs.First());

            parser.Pop(); // consume .
            var methodIdent = Identifier.Parse(parser);

            NormalLangPath synthesizedPath;
            if (traitPath is NormalLangPath nlp)
                synthesizedPath = nlp.Append(methodIdent.Identity);
            else
                throw new ParseException($"Expected a normal path for trait in qualified expression");

            // Check for ( args
            if (parser.Peek() is LeftParenthesisToken)
            {
                var args = ParseCallArgs(parser);
                var rootExpr = new PathExpression(synthesizedPath);
                return new ChainExpression(rootExpr,
                    [new CallStep { Arguments = args, Token = rootExpr.Token }],
                    qualifiedAsType: typePath as NormalLangPath);
            }

            return new PathExpression(synthesizedPath);
        }

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

    /// <summary>
    /// Extracts a LangPath from a parsed expression (for qualified trait expressions).
    /// Handles ChainExpression, PathExpression, etc.
    /// </summary>
    private static LangPath? ExtractPathFromExpression(IExpression expr)
    {
        if (expr is ChainExpression chain)
        {
            var segments = new List<NormalLangPath.PathSegment>
                { (NormalLangPath.PathSegment)chain.RootName };
            foreach (var step in chain.Steps)
            {
                if (step is AccessStep access)
                    segments.Add((NormalLangPath.PathSegment)access.Name);
                else if (step is CallStep call)
                {
                    var innerArgs = call.Arguments
                        .Select(a => ExtractPathFromExpression(a))
                        .Where(p => p != null)
                        .Cast<LangPath>()
                        .ToImmutableArray();
                    if (innerArgs.Length > 0 && segments[^1] is NormalLangPath.NormalPathSegment lastSeg)
                        segments[^1] = lastSeg.WithGenericArgs(innerArgs);
                }
            }
            return new NormalLangPath(chain.RootToken, segments);
        }
        if (expr is PathExpression pe) return pe.Path;
        return null;
    }

    public static IExpression ParsePrimary(Parser parser, bool handleAssignment = true)
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
                // Parse inner WITHOUT assignment handling — *expr = value
                // means the = belongs to the outer deref, not the inner expr.
                // e.g., *self.r = 5 should be (*self.r) = 5, not *(self.r = 5)
                expression = new DerefExpression(ParsePrimary(parser, false), (Token)derefTok);
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
            case IdentifierToken identToken when identToken.Identity == "make":
                expression = ParseMakeExpression(parser);
                break;
            case IdentifierToken:
                expression = ChainExpression.Parse(parser);
                break;
            case IBoolToken:
                expression = BoolExpression.Parse(parser);
                break;
            case StringLiteralToken:
                expression = StringLiteralExpression.Parse(parser);
                break;
            // Add more cases as needed,
            default:
                throw new ExpectedParserException(parser,
                    [ParseType.Fn, ParseType.Let, ParseType.LeftParenthesis, ParseType.Identifier], token);
        }

        token = parser.Peek();

        // Dot-loop for non-chain expressions (e.g., (expr).field, if_expr.field)
        // ChainExpression already consumed its dots during Parse.
        if (expression is not ChainExpression)
        {
            while (token is DotToken)
            {
                expression = FieldAccessExpression.Parse(parser, expression);
                token = parser.Peek();

                // Check for method call: expr.method(args)
                if (token is LeftParenthesisToken && expression is FieldAccessExpression fieldExpr)
                {
                    var args = ParseCallArgs(parser);
                    expression = new ChainExpression(fieldExpr.Caller,
                    [
                        new AccessStep { Name = fieldExpr.Field.Identity, 
                            IdentifierToken = fieldExpr.Field, Token = fieldExpr.Token },
                        new CallStep { Arguments = args, Token = fieldExpr.Token }
                    ]);
                    token = parser.Peek();
                }
            }
        }

        if (handleAssignment && token is EqualityToken) expression = AssignVariableExpression.Parse(parser, expression);
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

    /// <summary>Parse a parenthesized argument list: (arg1, arg2, ...)</summary>
    private static ImmutableArray<IExpression> ParseCallArgs(Parser parser)
    {
        Parenthesis.ParseLeft(parser);
        var args = new List<IExpression>();
        while (parser.Peek() is not RightParenthesisToken)
        {
            args.Add(Parse(parser));
            if (parser.Peek() is CommaToken) parser.Pop();
        }
        Parenthesis.ParseRight(parser);
        return args.ToImmutableArray();
    }

    /// <summary>
    /// Extracts the simple variable name from an expression, if it's a bare identifier.
    /// Works for both ChainExpression (new) and PathExpression (legacy).
    /// Returns null if the expression is not a simple variable reference.
    /// </summary>
    public static string? TryGetSimpleVariableName(IExpression? expr) => expr switch
    {
        ChainExpression { SimpleVariableName: string name } => name,
        PathExpression pe when pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1
            => nlp.PathSegments[0].ToString(),
        _ => null
    };

    /// <summary>
    /// Extracts the full field path from an expression for move tracking.
    /// Returns ["x"] for a simple variable, ["x", "a", "b"] for x.a.b.
    /// Only follows OWNED field access chains — stops at auto-deref (references, smart pointers).
    /// Returns null if the expression is not a movable path (e.g., function call, literal).
    /// </summary>
    public static FieldPath? TryGetFieldPath(IExpression? expr)
    {
        if (expr is ChainExpression chain && chain.ResolvedKind != null)
            return TryGetFieldPathFromKind(chain.ResolvedKind);

        var name = TryGetSimpleVariableName(expr);
        return name != null ? new FieldPath(name) : null;
    }

    internal static FieldPath? TryGetFieldPathFromKind(IChainKind kind)
    {
        if (kind is VariableRefKind vrk)
        {
            if (vrk.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
                return new FieldPath(nlp.PathSegments[0].ToString());
            return null;
        }

        if (kind is FieldAccessKind fak)
        {
            // Auto-deref means going through a reference or smart pointer — not an owned field.
            if (fak.AutoDeref || fak.AutoDerefDepth > 0)
                return null;

            var parent = TryGetFieldPathFromKind(fak.Receiver);
            return parent?.Append(fak.FieldName);
        }

        return null;
    }
}