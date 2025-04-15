using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public class Struct : Type
{
    
    public override void AssignTo(CodeGenContext codeGenContext, VariableRefItem value, VariableRefItem ptr)
    {
        for (int i = 0; i < Fields.Length; i++)
        {
            var field = Fields[i];
            var fieldType = codeGenContext.GetRefItemFor(field.TypePath) as TypeRefItem;
            LLVMValueRef fieldValuePtr;
            if (value.ValueRef.TypeOf.Kind == LLVMTypeKind.LLVMPointerTypeKind)
            {
                fieldValuePtr = codeGenContext.Builder.BuildStructGEP2(TypeRef, value.ValueRef,
                    GetIndexOfField(field.Name));
            }
            else
            {
                fieldValuePtr = codeGenContext.Builder.BuildAlloca(fieldType.TypeRef);
                var toExtract = codeGenContext.Builder.BuildExtractValue(value.ValueRef,(uint)i);
                fieldType.Type.AssignTo(codeGenContext,
                    new VariableRefItem()
                    {
                        ValueRef = toExtract,
                        Type = fieldType.Type,
                        ValueClassification = ValueClassification.RValue
                    }, new VariableRefItem()
                    {
                        ValueRef = fieldValuePtr,
                        Type = fieldType.Type,
                        ValueClassification = ValueClassification.LValue
                    });
            }

            var fieldPtrPtr = codeGenContext.Builder.BuildStructGEP2(TypeRef,ptr.ValueRef,
                    GetIndexOfField(field.Name));
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
    public override LangPath Ident => GetTypeIden();
    public override Token LookUpToken { get; }

    public override void Analyze(SemanticAnalyzer analyzer)
    {
        
    }
    public unsafe override LLVMValueRef LoadValueForRetOrArg(CodeGenContext context,VariableRefItem variableRef)
    {
        
        if (GetPrimitivesCompositeCount(context) > 0)
        {

            LLVMValueRef aggr = LLVM.GetUndef(TypeRef);
            for (int i = 0; i < Fields.Length; i++)
            {
                var field = Fields[i];
                var type = context.GetRefItemFor(field.TypePath) as TypeRefItem;
                var otherField = context.Builder.BuildStructGEP2(TypeRef, variableRef.ValueRef, (uint)i);
                var refIt = new VariableRefItem()
                {
                    ValueRef = otherField,
                    Type = type.Type,
                    ValueClassification = ValueClassification.LValue
                };
              
                if (aggr == null)
                {
                    aggr = context.Builder.BuildExtractValue(aggr,(uint) i);
                }
                else
                {
                    aggr = context.Builder.BuildInsertValue(aggr, type.Type.LoadValueForRetOrArg(context,refIt) ,(uint) i);
                }
         
                
            }

            return aggr;
        }

        return  LLVM.GetUndef(TypeRef);
        
      
    }
    public LangPath GetTypeIden()
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