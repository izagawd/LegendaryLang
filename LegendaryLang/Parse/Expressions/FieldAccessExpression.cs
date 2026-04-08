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

    /// <summary>
    /// Resolves a field's type on a given type, walking through the Receiver/Deref chain.
    /// Returns (fieldType, derefDepth) or (null, 0) if not found.
    /// </summary>
    public static (LangPath? fieldType, int derefDepth) ResolveFieldType(
        string fieldName, LangPath startType, SemanticAnalyzer analyzer)
    {
        var currentType = startType;
        int derefDepth = 0;
        const int maxDeref = 10;
        while (currentType != null && derefDepth < maxDeref)
        {
            var definition = analyzer.GetDefinition(currentType);
            if (definition == null && currentType is NormalLangPath nlpCur && nlpCur.GetFrontGenerics().Length > 0)
                definition = analyzer.GetDefinition(nlpCur.PopGenerics());

            if (definition is StructTypeDefinition std && std.Fields.Any(f => f.Name == fieldName))
            {
                var fieldType = std.Fields.First(f => f.Name == fieldName).TypePath;
                if (currentType is NormalLangPath nlpType)
                {
                    var genericArgs = nlpType.GetFrontGenerics();
                    if (genericArgs.Length > 0 && std.GenericParameters.Length > 0)
                        fieldType = SubstituteGenerics(fieldType, std.GenericParameters, genericArgs);
                }
                if (fieldType != null)
                    fieldType = analyzer.ResolveQualifiedTypePath(fieldType);
                return (fieldType, derefDepth);
            }

            var target = analyzer.ResolveAssociatedType(currentType,
                SemanticAnalyzer.ReceiverTraitPath, "Target");
            if (target == null || target == currentType) break;
            currentType = target;
            derefDepth++;
        }
        return (null, 0);
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        Caller.Analyze(analyzer);

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

        var (fieldType, derefDepth) = ResolveFieldType(Field.Identity, callerTypePath, analyzer);
        if (fieldType != null)
        {
            AutoDerefDepth = derefDepth;
            TypePath = fieldType;
            return;
        }

        // Nothing found — report error
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
    /// Shared codegen for field access: auto-deref receiver, GEP into struct, return field pointer.
    /// Used by both FieldAccessExpression.CodeGen and FieldAccessKind.CodeGen.
    /// </summary>
    public static ValueRefItem EmitFieldAccess(
        CodeGenContext ctx, ValueRefItem receiverVal,
        string fieldName, bool autoDeref, int autoDerefDepth)
    {
        if (autoDeref && receiverVal.Type is RefType ptrType)
        {
            var derefPtr = ptrType.LoadValue(ctx, receiverVal);
            receiverVal = new ValueRefItem
            {
                Type = ptrType.PointingToType,
                ValueRef = derefPtr
            };
        }

        for (int d = 0; d < autoDerefDepth; d++)
            receiverVal = DerefExpression.EmitDeref(ctx, receiverVal);

        var structType = (StructType)receiverVal.Type;
        var fieldIndex = structType.GetIndexOfField(fieldName);
        var fieldPtr = ctx.Builder.BuildStructGEP2(
            structType.TypeRef, receiverVal.ValueRef, fieldIndex);

        ConcreteDefinition.Type fieldType;
        if (structType.ResolvedFieldTypes != null
            && fieldIndex < structType.ResolvedFieldTypes.Value.Length)
            fieldType = structType.ResolvedFieldTypes.Value[(int)fieldIndex];
        else
        {
            var field = structType.GetField(fieldName);
            fieldType = ((TypeRefItem)ctx.GetRefItemFor(field.TypePath)).Type;
        }

        return new ValueRefItem { Type = fieldType, ValueRef = fieldPtr };
    }

    /// <summary>
    ///     Returns the pointer to the accessed field
    /// </summary>
    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var variableRef = Caller.CodeGen(codeGenContext);
        return EmitFieldAccess(codeGenContext, variableRef, Field.Identity, AutoDeref, AutoDerefDepth);
    }

    public LangPath? TypePath { get; set; }
    public bool IsTemporary => Caller.IsTemporary; // delegates to what it accesses from

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