using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Parse.Types;

public class StructTypeDefinition : CustomTypeDefinition
{








    public override LangPath TypePath =>(this as IDefinition).FullPath;
    public override Token LookUpToken { get; }

    public override void Analyze(SemanticAnalyzer analyzer)
    {
        foreach (var i in ComposedTypes)
        {
            i.LoadAsShortCutIfPossible(analyzer);
        }
    }


    public Token Token => StructToken;

    public StructToken StructToken { get; }
    public readonly ImmutableArray<Variable> Fields;

    public static StructTypeDefinition Parse(Parser parser)
    {
        var token = parser.Pop();
        if (token is StructToken structToken)
        {
            var structIdentifier = Identifier.Parse(parser);
            CurlyBrace.ParseLeft(parser);
            var next = parser.Peek();
            var fields = new List<Variable>();
            while (next is not RightCurlyBraceToken)
            {
                var field = Variable.Parse(parser);
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

    public Variable? GetField(string fieldName)
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

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath langPath)
    {
        if (langPath != (this as IDefinition).FullPath)
        {
            return null;
        }

        return [];
    }

    public override string Name { get; }
    public override NormalLangPath Module { get; }

    public StructTypeDefinition( string name,NormalLangPath module, StructToken token, IEnumerable<Variable> fields) 
    {
        StructToken = token;
        Name = name;
        Module = module;
        Fields = fields.ToImmutableArray();
    }

    public override ImmutableArray<LangPath> ComposedTypes => Fields.Select(i => i.TypePath).ToImmutableArray();
    public IConcreteDefinition Monomorphize(CodeGenContext context)
    {
        throw new NotImplementedException();
    }

    public ImmutableArray<GenericParameter> GenericParameters { get; init; }
}