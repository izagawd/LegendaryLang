using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using StructType = LegendaryLang.ConcreteDefinition.StructType;

namespace LegendaryLang.Parse.Expressions;

public class StructCreationExpression : IExpression
{
    public StructCreationExpression(LangPath typePath, IEnumerable<AssignedField> assignVariableExpressions)
    {
        TypePath = typePath;
        AssignFields = assignVariableExpressions.ToImmutableArray();
        Token = typePath.FirstIdentifierToken!;
    }


    public ImmutableArray<AssignedField> AssignFields { get; }
    private ValueRefItem? GeneratedDataRef { get; set; }
    public IEnumerable<ISyntaxNode> Children => AssignFields.Select(i => i.EqualsTo);
    public Token Token { get; }

    /// <summary>
    /// When set by LetStatement, the declared type on the LHS (e.g., Wrapper&lt;bool&gt;).
    /// Used to infer generic args for struct construction.
    /// </summary>
    public LangPath? DeclaredType { get; set; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        foreach (var i in AssignFields) i.EqualsTo.Analyze(analyzer);

        // Look up definition, stripping generics if needed
        var str = analyzer.GetDefinition(TypePath);
        if (str == null && TypePath is NormalLangPath nlpType && nlpType.GetFrontGenerics().Length > 0)
            str = analyzer.GetDefinition(nlpType.PopGenerics());

        if (str is null)
        {
            analyzer.AddException(
                new SemanticException(
                    $"No definition found for {TypePath}\n{Token.GetLocationStringRepresentation()}"));
            return;
        }

        var asStruct = str as StructTypeDefinition;
        if (asStruct is null)
        {
            analyzer.AddException(new SemanticException(
                $"Expected struct type but found {str.TypePath}\n{Token.GetLocationStringRepresentation()}"));
            return;
        }

        var genericArgs = (TypePath is NormalLangPath nlp) ? nlp.GetFrontGenerics() : [];

        // === TYPE INFERENCE ===
        // If struct has generic params but none were provided, try to infer them
        if (asStruct.GenericParameters.Length > 0 && genericArgs.Length == 0)
        {
            ImmutableArray<LangPath>? inferred = null;

            // Priority 1: Infer from DeclaredType (let a: Wrapper<i32> = Wrapper { ... })
            if (DeclaredType is NormalLangPath nlpDeclared)
            {
                var declaredBase = nlpDeclared.PopGenerics();
                var structBase = (TypePath is NormalLangPath nlpTp) ? nlpTp.PopGenerics() : TypePath;
                // Check the declared type is the same struct
                if (declaredBase == structBase || declaredBase == asStruct.TypePath
                    || (structBase != null && declaredBase == structBase))
                {
                    var declaredGenerics = nlpDeclared.GetFrontGenerics();
                    if (declaredGenerics.Length == asStruct.GenericParameters.Length)
                        inferred = declaredGenerics;
                }
            }

            // Priority 2: Infer from field expression types
            if (inferred == null)
            {
                var constraints = new List<(LangPath, LangPath)>();
                foreach (var field in asStruct.Fields)
                {
                    var assigned = AssignFields.FirstOrDefault(af => af.FieldToken.Identity == field.Name);
                    if (assigned?.EqualsTo?.TypePath != null && field.TypePath != null)
                        constraints.Add((field.TypePath, assigned.EqualsTo.TypePath));
                }
                inferred = TypeInference.InferFromConstraints(asStruct.GenericParameters, constraints);
            }

            if (inferred != null)
            {
                // Update TypePath with inferred generics
                var basePath = TypePath as NormalLangPath ?? new NormalLangPath(null, []);
                TypePath = basePath.AppendGenerics(inferred.Value);
                genericArgs = inferred.Value;
            }
            else
            {
                analyzer.AddException(new CannotInferGenericArgsException(
                    asStruct.Name, Token.GetLocationStringRepresentation()));
                return;
            }
        }

        // Check generic param count (for explicit generic args that's wrong)
        if (asStruct.GenericParameters.Length != genericArgs.Length)
        {
            if (asStruct.GenericParameters.Length > 0)
                analyzer.AddException(new SemanticException(
                    $"Struct '{asStruct.Name}' expects {asStruct.GenericParameters.Length} generic parameter(s), " +
                    $"but {genericArgs.Length} were provided\n{Token.GetLocationStringRepresentation()}"));
        }

        if (AssignFields.Length < asStruct.Fields.Length)
            analyzer.AddException(new SemanticException(
                $"Not all fields are assigned to the instance '{TypePath}'\nthe following fields are missing:\n" +
                $"{string.Join("\n", asStruct.Fields.AsEnumerable().Where(i => !AssignFields.Select(j => j.FieldToken.Identity).Contains(i.Name))
                    .Select(i => $"{i.Name}: {i.TypePath}"))}\n\n" +
                $"{Token.GetLocationStringRepresentation()}"));
        else if (AssignFields.Length > asStruct.Fields.Length)
        {
            analyzer.AddException(new SemanticException(
                $"The following fields do not exist for '{TypePath}'\n" +
                $"{string.Join("\n", AssignFields.AsEnumerable().Where(i => !asStruct.Fields.AsEnumerable().Select(j => j.Name).Contains(i.FieldToken.Identity))
                    .Select(i => $"{i.FieldToken.Identity}"))}\n\n" +
                $"{Token.GetLocationStringRepresentation()}"));
        }

        var invalidFields = new List<AssignedField>();
        foreach (var field in AssignFields)
            if (!asStruct.Fields.Any(i => i.Name == field.FieldToken.Identity))
                invalidFields.Add(field);

        if (invalidFields.Any())
            analyzer.AddException(new SemanticException(
                $"The following fields are not part of type '{TypePath}':\n{string.Join("\n", invalidFields.Select(i => i.FieldToken.Identity))}" +
                $"\n{Token.GetLocationStringRepresentation()}"));

        foreach (var field in AssignFields.Except(invalidFields))
        {
            var fieldType = asStruct.Fields.First(i => i.Name == field.FieldToken.Identity).TypePath;
            // Substitute generic params in field type
            if (genericArgs.Length > 0 && asStruct.GenericParameters.Length > 0)
                fieldType = FieldAccessExpression.SubstituteGenerics(fieldType, asStruct.GenericParameters, genericArgs);
            // Resolve qualified associated type paths in field type
            fieldType = analyzer.ResolveQualifiedTypePath(fieldType);
            if (field.EqualsTo.TypePath != fieldType)
                analyzer.AddException(new SemanticException(
                    $"Field '{field.FieldToken.Identity}' expects type '{fieldType}', found '{field.EqualsTo.TypePath}'\n" +
                    $"{field.FieldToken.GetLocationStringRepresentation()}"));
        }

        // Mark non-Copy field values as moved — ownership transfers to the struct
        foreach (var field in AssignFields)
            analyzer.TryMarkExpressionAsMoved(field.EqualsTo);
    }

    public bool HasGuaranteedExplicitReturn => AssignFields.Any(i => i.EqualsTo.HasGuaranteedExplicitReturn);
    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var typeRef = codeGenContext.GetRefItemFor(TypePath) as TypeRefItem;
        var structType = typeRef?.Type as StructType;

        var structPtr = codeGenContext.Builder.BuildAlloca(structType.TypeRef);


        for (var i = 0; i < structType.Fields.Length; i++)
        {
            var gotten = AssignFields
                .First(j => j.FieldToken.Identity == structType.Fields[i].Name);

            // Mark droppable field values as moved — ownership transfers to the struct
            codeGenContext.TryMarkExpressionDropMoved(gotten.EqualsTo);

            var data = gotten.EqualsTo.CodeGen(codeGenContext);
            var fieldPtr = codeGenContext.Builder.BuildStructGEP2(structType.TypeRef, structPtr, (uint)i);

            data.Type.AssignTo(codeGenContext, data, new ValueRefItem
            {
                Type = data.Type,
                ValueRef = fieldPtr
            });
        }


        return new ValueRefItem
        {
            Type = structType,
            ValueRef = structPtr
        };
    }


    public LangPath TypePath { get; protected set; }
    public bool IsTemporary => true; // fresh allocation

    public void ResolvePaths(PathResolver resolver)
    {
        TypePath = TypePath.Resolve(resolver);
        foreach (var i in Children.OfType<IPathResolvable>())
        {
            i.ResolvePaths(resolver);
        }
    }

    public static StructCreationExpression Parse(Parser parser, NormalLangPath path)
    {
        var curlyBrace = CurlyBrace.ParseLeft(parser);
        var token = parser.Peek();
        var fields = new List<AssignedField>();
        while (token is not RightCurlyBraceToken)
        {
            var identTok = Identifier.Parse(parser);
            Colon.Parse(parser);
            var value = IExpression.Parse(parser);
            token = parser.Peek();
            if (token is CommaToken)
            {
                Comma.Parse(parser);
                token = parser.Peek();
            }

            fields.Add(new AssignedField
            {
                FieldToken = identTok,
                EqualsTo = value
            });
        }

        var curlyBrace2 = CurlyBrace.Parseight(parser);
        foreach (var field in fields)
            if (field.FieldToken is null)
                throw new StructFieldTokenIsNullException(curlyBrace2);

        return new StructCreationExpression(path, fields);
    }

    public class StructFieldTokenIsNullException : ParseException
    {
        public StructFieldTokenIsNullException(Token lookUpToken)
        {
            LookUpToken = lookUpToken;
        }

        public Token LookUpToken { get; }

        public override string Message =>
            $"Expected struct all fields to have names\n{LookUpToken?.GetLocationStringRepresentation()}";
    }

    public class AssignedField
    {
        public IdentifierToken FieldToken { get; init; }
        public IExpression EqualsTo { get; init; }
    }
}