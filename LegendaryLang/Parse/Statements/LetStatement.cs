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
        VariableDefinition.TypePath = VariableDefinition.TypePath?.GetFromShortCutIfPossible(resolver);
        foreach (var i in Children)
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
            var genedVal = EqualsTo.DataRefCodeGen(context);
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
        EqualsTo?.Analyze(analyzer);

        if (TypePath is null)
        {
            if (VariableDefinition.TypePath is null && EqualsTo is null)
                throw new SemanticUnableToDetermineTypeOfLetVarException(this);

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
                if (EqualsTo.TypePath != VariableDefinition.TypePath) throw new LetConflictingTypesException(this);
                TypePath = VariableDefinition.TypePath;
            }
        }

        ArgumentNullException.ThrowIfNull(TypePath);


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
}