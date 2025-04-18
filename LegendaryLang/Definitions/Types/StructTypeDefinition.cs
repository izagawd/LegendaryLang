using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions.Types;

public class StructTypeDefinition : CustomTypeDefinition
{

    public override LangPath TypePath =>(this as IDefinition).FullPath;


    public override void Analyze(SemanticAnalyzer analyzer)
    {
        var list = new List<VariableDefinition>();
        foreach (var i in Fields)
        {
            list.Add(new VariableDefinition(i.IdentifierToken, i.TypePath.GetAsShortCutIfPossible(analyzer) ));
        }

        Fields = list.ToImmutableArray();
    }


    public override Token Token => StructToken;

    public StructToken StructToken { get; }
    public  ImmutableArray<VariableDefinition> Fields { get; protected set; }

    public static StructTypeDefinition Parse(Parser parser)
    {
        var token = parser.Pop();
        if (token is StructToken structToken)
        {
            var structIdentifier = Identifier.Parse(parser);
            CurlyBrace.ParseLeft(parser);
            var next = parser.Peek();
            var fields = new List<VariableDefinition>();
            while (next is not RightCurlyBraceToken)
            {
                var field = VariableDefinition.Parse(parser);
                if (field.TypePath is null)
                {
                    throw new ExpectedParserException(parser,(ParseType.BaseLangPath), field.IdentifierToken);
                }
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

            return new StructTypeDefinition(structIdentifier.Identity, parser.File.Module, structToken, fields);
            
        }
        else
        {
            throw new ExpectedParserException(parser,(ParseType.Struct), token);
        }
        
    }

    public class FieldNotFoundException : Exception
    {
        public string FieldName { get; }
        public StructTypeDefinition Struc { get; }

        public FieldNotFoundException(string fieldName, StructTypeDefinition struc)
        {
            FieldName = fieldName;
            Struc = struc;
        }

        public override string Message => $"Field {FieldName} doesn't exist in struct '{(Struc as IDefinition).FullPath}'\n{Struc.StructToken?.GetLocationStringRepresentation()}";
    }

    public VariableDefinition? GetField(string fieldName)
    {
        return Fields.FirstOrDefault(f => f.Name == fieldName);
    }
    public uint GetIndexOfField(string fieldName)
    {
        for (int i = 0; i < Fields.Length; i++)
        {
            var field = Fields[i];
            if (field.Name == fieldName)
            {
                return (uint) i;
            }
        }
        throw new FieldNotFoundException(fieldName, this);
    }


    public override Type GenerateIncompleteMono(CodeGenContext context, LangPath langPath)
    {
        return new StructType(this);
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        if (path != (this as IDefinition).FullPath)
        {
            return null;
        }

        return [];
    }

    public override string Name { get; }
    public override NormalLangPath Module { get; }

    public StructTypeDefinition( string name,NormalLangPath module, StructToken token, IEnumerable<VariableDefinition> fields) 
    {
        StructToken = token;
        Name = name;
        Module = module;
        Fields = fields.ToImmutableArray();
    }

    public override ImmutableArray<LangPath> ComposedTypes => Fields.Select(i => i.TypePath).ToImmutableArray();
    

    public ImmutableArray<GenericParameter> GenericParameters { get; init; }
}