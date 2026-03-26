using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions.Types;

public class StructTypeDefinition : ComposableTypeDefinition
{
    public StructTypeDefinition(string name, NormalLangPath module, StructToken token,
        IEnumerable<VariableDefinition> fields, IEnumerable<GenericParameter> genericParameters)
    {
        StructToken = token;
        Name = name;
        Module = module;
        Fields = fields.ToImmutableArray();
        GenericParameters = genericParameters.ToImmutableArray();
    }

    public override Token Token => StructToken;

    public StructToken StructToken { get; }
    public ImmutableArray<VariableDefinition> Fields { get; protected set; }

    public override string Name { get; }
    public override NormalLangPath Module { get; }

    public override ImmutableArray<LangPath> ComposedTypes => Fields.Select(i => i.TypePath).ToImmutableArray();

    public ImmutableArray<GenericParameter> GenericParameters { get; }

    public override void ResolvePaths(PathResolver resolver)
    {
        var list = new List<VariableDefinition>();
        foreach (var i in Fields)
            list.Add(new VariableDefinition(i.IdentifierToken, i.TypePath.Resolve(resolver)));

        Fields = list.ToImmutableArray();

        // Resolve trait bounds on generic params
        foreach (var gp in GenericParameters)
            for (int i = 0; i < gp.TraitBounds.Count; i++)
                gp.TraitBounds[i] = gp.TraitBounds[i].Resolve(resolver);
    }

    public override void Analyze(SemanticAnalyzer analyzer)
    {
        // Check for duplicate generic parameter names
        var seen = new HashSet<string>();
        foreach (var gp in GenericParameters)
        {
            if (!seen.Add(gp.Name))
                analyzer.AddException(new SemanticException(
                    $"Duplicate generic parameter name '{gp.Name}'\n{Token.GetLocationStringRepresentation()}"));
        }

        // Check that every generic parameter is used in at least one field type
        foreach (var gp in GenericParameters)
        {
            var used = Fields.Any(f => GenericParamUsedInType(gp.Name, f.TypePath));
            if (!used)
                analyzer.AddException(new SemanticException(
                    $"Generic parameter '{gp.Name}' is never used in struct '{Name}'\n{Token.GetLocationStringRepresentation()}"));
        }
    }

    private static bool GenericParamUsedInType(string paramName, LangPath? typePath)
    {
        if (typePath is NormalLangPath nlp)
        {
            foreach (var seg in nlp.PathSegments)
            {
                if (seg is NormalLangPath.NormalPathSegment ns && ns.Text == paramName) return true;
                if (seg is NormalLangPath.GenericTypesPathSegment gts)
                    foreach (var tp in gts.TypePaths)
                        if (GenericParamUsedInType(paramName, tp)) return true;
            }
        }
        if (typePath is TupleLangPath tlp)
        {
            foreach (var tp in tlp.TypePaths)
                if (GenericParamUsedInType(paramName, tp)) return true;
        }
        return false;
    }

    public static StructTypeDefinition Parse(Parser parser, NormalLangPath module)
    {
        var token = parser.Pop();
        if (token is StructToken structToken)
        {
            var structIdentifier = Identifier.Parse(parser);

            // Parse generic parameters
            var genericParameters = new List<GenericParameter>();
            if (parser.Peek() is OperatorToken { OperatorType: Operator.LessThan })
            {
                parser.Pop();
                var nextToken = parser.Peek();
                while (nextToken is not OperatorToken { OperatorType: Operator.GreaterThan })
                {
                    var paramIdentifier = Identifier.Parse(parser);
                    var traitBounds = new List<LangPath>();
                    if (parser.Peek() is ColonToken)
                    {
                        parser.Pop();
                        if (parser.Peek() is not OperatorToken { OperatorType: Operator.GreaterThan }
                            && parser.Peek() is not CommaToken)
                        {
                            traitBounds.Add(LangPath.Parse(parser, true));
                            while (parser.Peek() is OperatorToken { OperatorType: Operator.Add })
                            {
                                parser.Pop();
                                traitBounds.Add(LangPath.Parse(parser, true));
                            }
                        }
                    }
                    nextToken = parser.Peek();
                    genericParameters.Add(new GenericParameter(paramIdentifier, traitBounds));
                    if (nextToken is CommaToken)
                    {
                        parser.Pop();
                        nextToken = parser.Peek();
                    }
                    else
                    {
                        break;
                    }
                }
                Comparator.ParseGreater(parser);
            }

            CurlyBrace.ParseLeft(parser);
            var next = parser.Peek();
            var fields = new List<VariableDefinition>();
            while (next is not RightCurlyBraceToken)
            {
                var field = VariableDefinition.Parse(parser);
                if (field.TypePath is null)
                    throw new ExpectedParserException(parser, ParseType.BaseLangPath, field.IdentifierToken);
                fields.Add(field);
                next = parser.Peek();
                if (next is not RightCurlyBraceToken)
                {
                    Comma.Parse(parser);
                    next = parser.Peek();
                }
                else
                {
                    break;
                }
            }

            CurlyBrace.Parseight(parser);

            return new StructTypeDefinition(structIdentifier.Identity, module, structToken, fields, genericParameters);
        }

        throw new ExpectedParserException(parser, ParseType.Struct, token);
    }

    public VariableDefinition? GetField(string fieldName)
    {
        return Fields.FirstOrDefault(f => f.Name == fieldName);
    }

    public uint GetIndexOfField(string fieldName)
    {
        for (var i = 0; i < Fields.Length; i++)
        {
            var field = Fields[i];
            if (field.Name == fieldName) return (uint)i;
        }

        throw new FieldNotFoundException(fieldName, this);
    }


    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath> genericArguments)
    {
        // Push scope with generic param → concrete type mappings
        context.AddScope();
        for (int i = 0; i < GenericParameters.Length && i < genericArguments.Length; i++)
        {
            var argRefItem = context.GetRefItemFor(genericArguments[i]);
            if (argRefItem != null)
            {
                context.AddToDeepestScope(new NormalLangPath(null, [GenericParameters[i].Name]),
                    argRefItem);
            }
        }

        // Resolve field types while generic scope is active
        var resolvedFieldTypesList = new List<ConcreteDefinition.Type>();
        foreach (var field in Fields)
        {
            var refItem = context.GetRefItemFor(field.TypePath) as TypeRefItem;
            if (refItem == null)
            {
                // Field type unresolvable — can happen when generic args are invalid types
                context.PopScope();
                return new TypeRefItem()
                {
                    Type = new StructType(this, context.LLVMContext.CreateNamedStruct("__invalid__"))
                    {
                        TypeDefinition = { }
                    }
                };
            }
            resolvedFieldTypesList.Add(refItem.Type);
        }
        var resolvedFieldTypes = resolvedFieldTypesList.ToImmutableArray();

        var monomorphizedPath = genericArguments.Length > 0
            ? (LangPath)((NormalLangPath) TypePath).Append(new NormalLangPath.GenericTypesPathSegment(genericArguments))
            : TypePath;

        var structt 
            = context.LLVMContext.CreateNamedStruct(monomorphizedPath.ToString());
        structt.StructSetBody(
            resolvedFieldTypes.Select(t => t.TypeRef).ToArray(),
            false);

        context.PopScope();

        return new TypeRefItem()
        {
            Type = new StructType(this, structt, monomorphizedPath)
            {
                TypeDefinition = { },
                ResolvedFieldTypes = resolvedFieldTypes
            }
        };
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path != (this as IDefinition).TypePath) return null;

        return [];
    }



    public class FieldNotFoundException : Exception
    {
        public FieldNotFoundException(string fieldName, StructTypeDefinition struc)
        {
            FieldName = fieldName;
            Struc = struc;
        }

        public string FieldName { get; }
        public StructTypeDefinition Struc { get; }

        public override string Message =>
            $"Field {FieldName} doesn't exist in struct '{(Struc as IDefinition).TypePath}'\n{Struc.StructToken?.GetLocationStringRepresentation()}";
    }
}