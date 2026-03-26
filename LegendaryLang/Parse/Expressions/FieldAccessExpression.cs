using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public class FieldAccessExpression : IExpression
{
    public FieldAccessExpression(IdentifierToken field, IExpression caller)
    {
        Field = field;
        Caller = caller;
    }

    public IdentifierToken Field { get; }
    public IExpression Caller { get; }
    public IEnumerable<ISyntaxNode> Children => [Caller];
    public Token Token => Field;

    public void Analyze(SemanticAnalyzer analyzer)
    {
        Caller.Analyze(analyzer);

        // Look up definition, stripping generics if needed
        var callerTypePath = Caller.TypePath;
        var definition = analyzer.GetDefinition(callerTypePath);
        if (definition == null && callerTypePath is NormalLangPath nlpCaller && nlpCaller.GetFrontGenerics().Length > 0)
            definition = analyzer.GetDefinition(nlpCaller.PopGenerics());

        if (definition is not StructTypeDefinition structTypeDefinition)
        {
            analyzer.AddException(new SemanticException(
                $"Type '{Caller.TypePath}' is not a struct, so field access does not make sense\n{Token.GetLocationStringRepresentation()}"));
            return;
        }

        if (!structTypeDefinition.Fields.Any(i => i.Name == Field.Identity))
        {
            analyzer.AddException(new SemanticException(
                $"Type '{callerTypePath}' does not contain a field named '{Field.Identity}'\n{Token.GetLocationStringRepresentation()}"));
            return;
        }

        var fieldType = structTypeDefinition.Fields.First(i => i.Name == Field.Identity).TypePath;

        // If the struct has generics, substitute them in the field type
        if (callerTypePath is NormalLangPath nlpType)
        {
            var genericArgs = nlpType.GetFrontGenerics();
            if (genericArgs.Length > 0 && structTypeDefinition.GenericParameters.Length > 0)
                fieldType = SubstituteGenerics(fieldType, structTypeDefinition.GenericParameters, genericArgs);
        }

        TypePath = fieldType;
    }

    /// <summary>
    /// Substitutes generic parameter names with their concrete type arguments in a LangPath.
    /// </summary>
    public static LangPath SubstituteGenerics(LangPath path, ImmutableArray<GenericParameter> genericParams, ImmutableArray<LangPath> genericArgs)
    {
        // Direct match: path is a single-segment name matching a generic param
        if (path is NormalLangPath nlp && nlp.PathSegments.Length == 1
            && nlp.PathSegments[0] is NormalLangPath.NormalPathSegment ns)
        {
            for (int i = 0; i < genericParams.Length && i < genericArgs.Length; i++)
                if (ns.Text == genericParams[i].Name)
                    return genericArgs[i];
        }

        // Recurse into generic type segments
        if (path is NormalLangPath nlp2)
        {
            var newSegments = new List<NormalLangPath.PathSegment>();
            foreach (var seg in nlp2.PathSegments)
            {
                if (seg is NormalLangPath.GenericTypesPathSegment gts)
                {
                    var newTypes = gts.TypePaths.Select(tp => SubstituteGenerics(tp, genericParams, genericArgs));
                    newSegments.Add(new NormalLangPath.GenericTypesPathSegment(newTypes));
                }
                else
                    newSegments.Add(seg);
            }
            return new NormalLangPath(nlp2.FirstIdentifierToken, newSegments);
        }

        if (path is TupleLangPath tlp)
            return new TupleLangPath(tlp.TypePaths.Select(tp => SubstituteGenerics(tp, genericParams, genericArgs)));

        return path;
    }


    /// <summary>
    ///     Returns the pointer to the accessed field
    /// </summary>
    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var variableRef = Caller.CodeGen(codeGenContext);


        var structType = variableRef?.Type as StructType;


        var fieldPtr = codeGenContext.Builder.BuildStructGEP2(structType.TypeRef, variableRef.ValueRef,
            structType.GetIndexOfField(Field.Identity));

        var field = structType.GetField(Field.Identity);
        var fieldTypeRef = codeGenContext.GetRefItemFor(field.TypePath) as TypeRefItem;

        return new ValueRefItem
        {
            Type = fieldTypeRef.Type,

            ValueRef = fieldPtr
        };
    }

    public LangPath? TypePath { get; set; }

    public static FieldAccessExpression Parse(Parser parser, IExpression caller)
    {
        DotParser.Parse(parser);
        var fieldIden = Identifier.Parse(parser);
        var field = new FieldAccessExpression(fieldIden, caller);
        var nextToken = parser.Peek();
        while (nextToken is DotToken)
        {
            DotParser.Parse(parser);
            fieldIden = Identifier.Parse(parser);
            field = new FieldAccessExpression(fieldIden, field);
            nextToken = parser.Peek();
        }

        return field;
    }

    public bool HasGuaranteedExplicitReturn => Caller.HasGuaranteedExplicitReturn;
}