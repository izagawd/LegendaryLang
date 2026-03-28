using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Statements;

public class LetConflictingTypesException : SemanticException
{
    public LetConflictingTypesException(LetStatement statement)
    {
        Statement = statement;
    }

    public LetStatement Statement { get; }

    public override string Message =>
        $"Conflicting types:\nThe declared type '{Statement.VariableDefinition.TypePath}' and assigned expression with type" +
        $" '{Statement.EqualsTo.TypePath}' do not have matching types\n{Statement.LetToken?.GetLocationStringRepresentation()}";
}

public class LetStatement : IStatement
{
    public LetStatement(LetToken letToken, VariableDefinition variableDefinition, IExpression? equalsTo)
    {
        LetToken = letToken;
        EqualsTo = equalsTo;
        VariableDefinition = variableDefinition;
        if (EqualsTo is null && VariableDefinition.TypePath is null) throw new UnknownTypeException(this);
    }

    public LetToken LetToken { get; }
    public IExpression? EqualsTo { get; }

    public VariableDefinition VariableDefinition { get; }

    // Would be set after semantic analysis
    private LangPath? TypePath { get; set; }

    public void ResolvePaths(PathResolver resolver)
    {
        VariableDefinition.TypePath = VariableDefinition.TypePath?.Resolve(resolver);
        foreach (var i in Children.OfType<IPathResolvable>())
        {
            i.ResolvePaths(resolver);
        }
        resolver.AddToDeepestScope(VariableDefinition.Name,
            new NormalLangPath(VariableDefinition.IdentifierToken, [VariableDefinition.Name]));
    }

    public void CodeGen(CodeGenContext context)
    {
        if (EqualsTo is not null)
        {
            var genedVal = EqualsTo.CodeGen(context);
            var stackPtr = genedVal.StackAllocate(context);

            context.AddToDeepestScope(new NormalLangPath(null, [VariableDefinition.Name]), new ValueRefItem
            {
                Type = genedVal.Type,
                ValueRef = stackPtr
            });
        }
        else
        {
            var type = context.GetRefItemFor(VariableDefinition.TypePath) as TypeRefItem;

            var stackPtr = context.Builder.BuildAlloca(type.TypeRef, VariableDefinition.Name);

            context.AddToDeepestScope(new NormalLangPath(null, [VariableDefinition.Name]), new ValueRefItem
            {
                Type = type.Type,
                ValueRef = stackPtr
            });
        }
    }


    public IEnumerable<ISyntaxNode> Children
    {
        get
        {
            if (EqualsTo is not null) yield return EqualsTo;
        }
    }


    public Token Token => LetToken;

    public void Analyze(SemanticAnalyzer analyzer)
    {
        // Propagate declared type to RHS for inference before analyzing
        if (VariableDefinition.TypePath != null && EqualsTo != null)
        {
            if (EqualsTo is StructCreationExpression sce)
                sce.DeclaredType = VariableDefinition.TypePath;
            if (EqualsTo is FunctionCallExpression fce)
                fce.ExpectedReturnType = VariableDefinition.TypePath;
        }

        EqualsTo?.Analyze(analyzer);

        // Resolve qualified associated type paths (e.g., <i32 as Add<i32>>::Output → i32)
        if (VariableDefinition.TypePath != null)
            VariableDefinition.TypePath = analyzer.ResolveQualifiedTypePath(VariableDefinition.TypePath);

        if (TypePath is null)
        {
            if (VariableDefinition.TypePath is null && EqualsTo is null)
            {
                analyzer.AddException(new SemanticException(
                    $"Cannot determine type of variable '{VariableDefinition.Name}' — no type annotation and no initializer\n{Token.GetLocationStringRepresentation()}"));
                return;
            }

            if (VariableDefinition.TypePath is null && EqualsTo is not null)
            {
                TypePath = EqualsTo.TypePath;
            }
            else if (VariableDefinition.TypePath is not null && EqualsTo is null)
            {
                TypePath = VariableDefinition.TypePath;
            }
            else if (EqualsTo is not null && VariableDefinition.TypePath is not null)
            {
                if (EqualsTo.TypePath != VariableDefinition.TypePath)
                {
                    analyzer.AddException(new TypeMismatchException(
                        VariableDefinition.TypePath, EqualsTo.TypePath,
                        "Conflicting types in let binding",
                        Token.GetLocationStringRepresentation()));
                    TypePath = VariableDefinition.TypePath;
                }
                else
                {
                    TypePath = VariableDefinition.TypePath;
                }
            }
        }

        if (TypePath is null) return;

        // Shadowing: invalidate borrows from the old binding before replacing it
        analyzer.InvalidateBorrowsFrom(VariableDefinition.Name);

        // Fresh binding — unmark this variable from moved (handles shadowing)
        analyzer.UnmarkMoved(VariableDefinition.Name);

        // Track this variable in the current scope for lifetime invalidation
        analyzer.TrackScopeVariable(VariableDefinition.Name);

        // If RHS is a borrow (&expr), register the borrow relationship
        if (EqualsTo is PointerGetterExpression pge)
        {
            // Extract the source variable name from the borrowed expression
            string? sourceName = null;
            if (pge.PointingTo is PathExpression srcPe && srcPe.Path is NormalLangPath srcNlp
                && srcNlp.PathSegments.Length == 1)
            {
                sourceName = srcNlp.PathSegments[0].ToString();
            }
            else if (pge.PointingTo is FieldAccessExpression fae)
            {
                // &foo.bar — source is "foo"
                var root = fae.Caller;
                while (root is FieldAccessExpression innerFae) root = innerFae.Caller;
                if (root is PathExpression rootPe && rootPe.Path is NormalLangPath rootNlp
                    && rootNlp.PathSegments.Length == 1)
                    sourceName = rootNlp.PathSegments[0].ToString();
            }

            if (sourceName != null)
            {
                // NLL-style: instead of erroring on conflict, invalidate conflicting borrows.
                // If the invalidated borrow is used later, THAT's when the error fires.
                analyzer.InvalidateConflictingBorrows(sourceName, pge.RefKind);

                analyzer.RegisterBorrow(sourceName, VariableDefinition.Name, pge.RefKind);

                // If borrowing from a local (not a function parameter), mark this variable
                // as holding a local borrow — it cannot be returned from the function
                if (!analyzer.IsFunctionParameter(sourceName))
                    analyzer.MarkAsLocalBorrow(VariableDefinition.Name);
            }
        }

        // If RHS is a variable and the type is not Copy, mark the source as moved
        if (EqualsTo is not null)
            analyzer.TryMarkExpressionAsMoved(EqualsTo);

        analyzer.RegisterVariableType(new NormalLangPath(VariableDefinition.IdentifierToken, [VariableDefinition.Name]),
            TypePath);
    }

    public static LetStatement Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not LetToken letToken) throw new ExpectedParserException(parser, [ParseType.Let], gotten);
        var variable = VariableDefinition.Parse(parser);
        var next = parser.Peek();
        if (next is EqualityToken)
        {
            parser.Pop();
            var expr = IExpression.Parse(parser);
            return new LetStatement(letToken, variable, expr);
        }

        return new LetStatement(letToken, variable, null);
    }

    public class UnknownTypeException : SemanticException
    {
        public UnknownTypeException(LetStatement statement)
        {
            Statement = statement;
        }

        public LetStatement Statement { get; }

        public override string Message =>
            $"The type of the let statement is unknown, since theres no equals to expression, and the type wasnt declared" +
            $"\n{Statement.LetToken.GetLocationStringRepresentation()}";
    }

    public class SemanticUnableToDetermineTypeOfLetVarException : SemanticException
    {
        public SemanticUnableToDetermineTypeOfLetVarException(LetStatement statement)
        {
            Statement = statement;
        }

        public LetStatement Statement { get; }

        public override string Message =>
            $"No type was declared and no expression was set for the let statement, so unable to determine the" +
            $" type of the let\n{Statement.LetToken.GetLocationStringRepresentation()}";
    }

    public bool HasGuaranteedExplicitReturn => EqualsTo?.HasGuaranteedExplicitReturn ?? false;

    /// <summary>Check if a type path is a reference type (&amp;T).</summary>
    private static bool IsReferenceType(LangPath? typePath)
    {
        return typePath is NormalLangPath nlp
               && nlp.Contains(RefTypeDefinition.GetRefModule());
    }
}