using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.Parse.Types.Type;

namespace LegendaryLang.Parse.Statements;


public class LetConflictingTypesException : SemanticException
{
    public LetStatement Statement { get; }

    public LetConflictingTypesException(LetStatement statement)
    {
        Statement = statement;
    }

    public override string Message => $"Conflicting types:\nThe declared type '{Statement.Variable.TypePath}' and assigned expression with type" +
                                      $" '{Statement.EqualsTo.TypePath}' do not have matching types\n{Statement.LetToken?.GetLocationStringRepresentation()}";
}
public class LetStatement : IStatement
{
    public class UnknownTypeException : SemanticException
    {
        public LetStatement Statement { get; }
        public UnknownTypeException(LetStatement statement)
        {
            Statement = statement;
        }
        public override string Message => $"The type of the let statement is unknown, since theres no equals to expression, and the type wasnt declared" +
                                          $"\n{Statement.LetToken.GetLocationStringRepresentation()}";
    }
    public static LetStatement Parse(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not LetToken letToken)
        {
            throw new ExpectedParserException(parser,[ParseType.Let], gotten);
        }
        var variable = Variable.Parse(parser);
        var next = parser.Peek();
        if (next is EqualityToken)
        {
            parser.Pop();
            var expr = IExpression.Parse(parser);
            return new LetStatement(letToken, variable, expr);
        }
        return new LetStatement(letToken, variable, null);
    }
    public LetStatement(LetToken letToken,  Variable variable,IExpression? equalsTo)
    {
        LetToken = letToken;
        EqualsTo = equalsTo;
        Variable = variable;
        if (EqualsTo is null && Variable.TypePath is null)
        {
            throw new UnknownTypeException(this);
        }
    }

    public LetToken LetToken { get; }
    public IExpression? EqualsTo { get; }
    public Variable Variable { get; }

    public unsafe void CodeGen(CodeGenContext context)
    {

        if (EqualsTo is not null)
        {
            var genedVal = EqualsTo.DataRefCodeGen(context);
            var stackPtr = genedVal.StackAllocate(context);

            context.AddToDeepestScope(new NormalLangPath( null,[Variable.Name]),new VariableRefItem()
            {
                Type = genedVal.Type,
                ValueRef = stackPtr
            });
        }
        else
        {
            var type = context.GetRefItemFor(Variable.TypePath) as TypeRefItem;
             
            var stackPtr = context.Builder.BuildAlloca(type.TypeRef, Variable.Name);

            context.AddToDeepestScope(new NormalLangPath( null,[Variable.Name]),new VariableRefItem()
            {
                Type = type.Type,
                ValueRef = stackPtr
            });
        }
    }

    public class SemanticUnableToDetermineTypeOfLetVarException : SemanticException
    {
        public LetStatement Statement { get; }

        public SemanticUnableToDetermineTypeOfLetVarException(LetStatement statement)
        {
            Statement = statement;
        }

        public override string Message =>
            $"No type was declared and no expression was set for the let statement, so unable to determine the" +
            $" type of the let\n{Statement.LetToken.GetLocationStringRepresentation()}";
    }
    // Would be set after semantic analysis
    private LangPath? TypePath { get;  set; }

    public Token LookUpToken => LetToken;
    public void Analyze(SemanticAnalyzer analyzer)
    {
        if (TypePath is null)
        {
            EqualsTo?.Analyze(analyzer);
            
            if (Variable.TypePath is null && EqualsTo is null)
            {
                throw new SemanticUnableToDetermineTypeOfLetVarException(this);
            }

            if (Variable.TypePath is null && EqualsTo is not null)
            {
                TypePath = EqualsTo.TypePath;
            } else if (Variable.TypePath is not null && EqualsTo is  null)
            {
                TypePath = Variable.TypePath;
            } else if (EqualsTo is not null && Variable.TypePath is not null)
            {
                if (EqualsTo.TypePath != Variable.TypePath)
                {
                    throw new LetConflictingTypesException(this);
                }
                TypePath = Variable.TypePath;
            }
        }

        ArgumentNullException.ThrowIfNull(TypePath);
        

        
    }
}