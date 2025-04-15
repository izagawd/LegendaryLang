using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public class Struct : Type
{
    public override LLVMValueRef AssignTo(CodeGenContext codeGenContext, VariableRefItem value, VariableRefItem ptr)
    {
        foreach (var i in Fields)
        {
            var fieldType = codeGenContext.GetRefItemFor(i.TypePath) as TypeRefItem;
            var fieldValuePtr = codeGenContext.Builder.BuildStructGEP2(TypeRef,value.ValueRef,
                GetIndexOfField(i.Name));
            var fieldPtrPtr = codeGenContext.Builder.BuildStructGEP2(TypeRef,ptr.ValueRef,
                GetIndexOfField(i.Name));
            fieldType.Type.AssignTo(codeGenContext, new VariableRefItem()
            {
                ValueRef = fieldValuePtr,
                ValueClassification = ValueClassification.LValue,
                Type = fieldType.Type
            },
                new VariableRefItem()
                {
                    ValueRef = fieldPtrPtr,
                    ValueClassification = ValueClassification.LValue,
                    Type = fieldType.Type
                });
        }
        return ptr.ValueRef;
    }

    public override int GetPrimitivesCompositeCount(CodeGenContext context)
    {
       return  Fields.Select(i => (context.GetRefItemFor(i.TypePath) as TypeRefItem).Type.GetPrimitivesCompositeCount(context))
            .Sum();
    }

    public override LLVMTypeRef TypeRef { get; protected set; }
    public NormalLangPath Module { get; set; } = new NormalLangPath(null,["something"]);
    public unsafe override void CodeGen(CodeGenContext context)
    {
        // 1. Check if the struct is already generated (avoid double-generation)
        if (TypeRef.Handle != IntPtr.Zero)
            return;

        // 2. Create an opaque (named but incomplete) LLVM struct first
        var  llvmStructName = GetTypeIden().ToString(); // e.g., "my.module.MyStruct"
        TypeRef = LLVM.StructCreateNamed(context.Module.Context, llvmStructName.ToCString());

        // 3. Generate LLVM types for the struct fields
        var fieldTypes = Fields.Select(field => (context.GetRefItemFor(field.TypePath) as TypeRefItem).Type).ToArray();

        fixed (void* ptr = fieldTypes.Select(i => i.TypeRef).ToArray())
        {        
            LLVM.StructSetBody(TypeRef,(LLVMSharp.Interop.LLVMOpaqueType**) ptr,(uint) fieldTypes.Length, 0);
            
        }
        // 4. Set the body of the opaque struct


        context.AddToTop(Ident,new TypeRefItem()
        {
            Type = this
        });
    }

    public override int Priority => 1;
    public override BaseLangPath Ident => GetTypeIden();
    public override Token LookUpToken { get; }

    public override void Analyze(SemanticAnalyzer analyzer)
    {
        
    }
    
    public BaseLangPath GetTypeIden()
    {
        return new NormalLangPath(null,[..Module.Path, Name]);
    }
    public Token Token => StructToken;

    public StructToken StructToken { get; }
    public readonly ImmutableArray<Variable> Fields;
    public readonly string Name;
    public static Struct Parse(Parser parser)
    {
        var token = parser.Pop();
        if (token is StructToken structToken)
        {
            var structName = Identifier.Parse(parser);
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

            return new Struct(structToken, structName.Identity, fields);
            
        }
        else
        {
            throw new ExpectedParserException(parser,(ParseType.Struct), token);
        }
        
    }

    public class FieldNotFoundException : Exception
    {
        public FieldNotFoundException(string fieldName, Struct struc)
        {
            
        }
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
    public Struct(StructToken token, string name, IEnumerable<Variable> fields)
    {
        StructToken = token;
        Name = name;
        Fields = fields.ToImmutableArray();
    }
}