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

    /// <summary>
    /// Set during Analyze if the caller is a reference type and was auto-deref'd
    /// </summary>
    public bool AutoDeref { get; private set; }

    /// <summary>
    /// Number of trait-based deref steps (e.g., Box&lt;T&gt; → T is 1 step)
    /// </summary>
    public int AutoDerefDepth { get; private set; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        Caller.Analyze(analyzer);

        // Look up definition, stripping generics if needed
        var callerTypePath = Caller.TypePath;

        // Auto-deref: if caller is a reference type (&T), unwrap to T
        if (callerTypePath is NormalLangPath nlpRef
            && nlpRef.Contains(RefTypeDefinition.GetRefModule()))
        {
            var generics = nlpRef.GetFrontGenerics();
            if (generics.Length == 1)
            {
                callerTypePath = generics[0];
                AutoDeref = true;
            }
        }

        // Try to find the field, walking through Receiver/Deref chain if needed
        var currentType = callerTypePath;
        int derefDepth = 0;
        const int maxDeref = 10;
        while (currentType != null && derefDepth < maxDeref)
        {
            var definition = analyzer.GetDefinition(currentType);
            if (definition == null && currentType is NormalLangPath nlpCur && nlpCur.GetFrontGenerics().Length > 0)
                definition = analyzer.GetDefinition(nlpCur.PopGenerics());

            if (definition is StructTypeDefinition std && std.Fields.Any(i => i.Name == Field.Identity))
            {
                // Found the field
                callerTypePath = currentType;
                AutoDerefDepth = derefDepth;

                var fieldType = std.Fields.First(i => i.Name == Field.Identity).TypePath;

                // Substitute generics in field type
                if (currentType is NormalLangPath nlpType)
                {
                    var genericArgs = nlpType.GetFrontGenerics();
                    if (genericArgs.Length > 0 && std.GenericParameters.Length > 0)
                        fieldType = SubstituteGenerics(fieldType, std.GenericParameters, genericArgs);
                }

                TypePath = fieldType;
                if (TypePath != null)
                    TypePath = analyzer.ResolveQualifiedTypePath(TypePath);
                return;
            }

            // Try Receiver.Target to walk one level deeper
            var target = analyzer.ResolveAssociatedType(currentType,
                SemanticAnalyzer.ReceiverTraitPath, "Target");
            if (target == null || target == currentType) break;
            currentType = target;
            derefDepth++;
        }

        // Nothing found — report error on original type
        var origDef = analyzer.GetDefinition(callerTypePath);
        if (origDef == null && callerTypePath is NormalLangPath nlpOrig && nlpOrig.GetFrontGenerics().Length > 0)
            origDef = analyzer.GetDefinition(nlpOrig.PopGenerics());

        if (origDef is not StructTypeDefinition)
        {
            analyzer.AddException(new SemanticException(
                $"Type '{Caller.TypePath}' is not a struct, so field access does not make sense\n{Token.GetLocationStringRepresentation()}"));
            return;
        }

        analyzer.AddException(new SemanticException(
            $"Type '{callerTypePath}' does not contain a field named '{Field.Identity}'\n{Token.GetLocationStringRepresentation()}"));
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
                if (seg is NormalLangPath.NormalPathSegment { HasGenericArgs: true } nps)
                {
                    var newTypes = nps.GenericArgs!.Value.Select(tp => SubstituteGenerics(tp, genericParams, genericArgs)).ToImmutableArray();
                    newSegments.Add(nps.WithGenericArgs(newTypes));
                }
                else
                    newSegments.Add(seg);
            }
            return new NormalLangPath(nlp2.FirstIdentifierToken, newSegments);
        }

        if (path is TupleLangPath tlp)
            return new TupleLangPath(tlp.TypePaths.Select(tp => SubstituteGenerics(tp, genericParams, genericArgs)));

        if (path is QualifiedAssocTypePath qp)
            return new QualifiedAssocTypePath(
                SubstituteGenerics(qp.ForType, genericParams, genericArgs),
                SubstituteGenerics(qp.TraitPath, genericParams, genericArgs),
                qp.AssociatedTypeName,
                qp.FirstIdentifierToken);

        return path;
    }


    /// <summary>
    ///     Returns the pointer to the accessed field
    /// </summary>
    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var variableRef = Caller.CodeGen(codeGenContext);

        // Auto-deref: if the caller is a reference, load the pointer to get the struct address
        if (AutoDeref && variableRef.Type is RefType ptrType)
        {
            var derefPtr = ptrType.LoadValue(codeGenContext, variableRef);
            variableRef = new ValueRefItem
            {
                Type = ptrType.PointingToType,
                ValueRef = derefPtr
            };
        }

        // Trait-based auto-deref (e.g., Box(T) → T via Deref)
        for (int d = 0; d < AutoDerefDepth; d++)
        {
            variableRef = DerefExpression.EmitDeref(codeGenContext, variableRef);
        }

        var structType = variableRef?.Type as StructType;
        var fieldIndex = structType.GetIndexOfField(Field.Identity);

        var fieldPtr = codeGenContext.Builder.BuildStructGEP2(structType.TypeRef, variableRef.ValueRef,
            fieldIndex);

        // Use ResolvedFieldTypes if available (handles generic structs where T is no longer in scope)
        ConcreteDefinition.Type fieldType;
        if (structType.ResolvedFieldTypes != null && fieldIndex < structType.ResolvedFieldTypes.Value.Length)
        {
            fieldType = structType.ResolvedFieldTypes.Value[(int)fieldIndex];
        }
        else
        {
            var field = structType.GetField(Field.Identity);
            fieldType = ((TypeRefItem)codeGenContext.GetRefItemFor(field.TypePath)).Type;
        }

        return new ValueRefItem
        {
            Type = fieldType,
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