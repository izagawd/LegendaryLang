using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public class Struct : CustomType
{


    public override LLVMTypeRef TypeRef { get; protected set; }


    public unsafe override void CodeGen(CodeGenContext context)
    {
        // 1. Check if the struct is already generated (avoid double-generation)
        if (TypeRef.Handle != IntPtr.Zero)
            return;

        // 2. Create an opaque (named but incomplete) LLVM struct first
        var llvmStructName = (this as IDefinition).FullPath.ToString();// e.g., "my.module.MyStruct"
        TypeRef = LLVM.StructCreateNamed(context.Module.Context, llvmStructName.ToCString());

        // 3. Generate LLVM types for the struct fields
        var fieldTypes = Fields.Select(field =>
        {
            var idk = context.GetRefItemFor(field.TypePath);
            return (idk as TypeRefItem).Type;
        }).ToArray();

        fixed (void* ptr = fieldTypes.Select(i => i.TypeRef).ToArray())
        {        
            LLVM.StructSetBody(TypeRef,(LLVMSharp.Interop.LLVMOpaqueType**) ptr,(uint) fieldTypes.Length, 0);
            
        }
        // 4. Set the body of the opaque struct


        context.AddToDeepestScope(TypePath,new TypeRefItem()
        {
            Type = this
        });
    }

    public override int Priority => 1;
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

    public static Struct Parse(Parser parser)
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

            return new Struct(structIdentifier.Identity, parser.File.Module, structToken, fields);
            
        }
        else
        {
            throw new ExpectedParserException(parser,(ParseType.Struct), token);
        }
        
    }

    public class FieldNotFoundException : Exception
    {
        public string FieldName { get; }
        public Struct Struc { get; }

        public FieldNotFoundException(string fieldName, Struct struc)
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

    public override string Name { get; }
    public override NormalLangPath Module { get; }

    public Struct( string name,NormalLangPath module, StructToken token, IEnumerable<Variable> fields) 
    {
        StructToken = token;
        Name = name;
        Module = module;
        Fields = fields.ToImmutableArray();
    }

    public override ImmutableArray<LangPath> ComposedTypes => Fields.Select(i => i.TypePath).ToImmutableArray();
}