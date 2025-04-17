using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp;
using LLVMSharp.Interop;
using StructType = LegendaryLang.ConcreteDefinition.StructType;

namespace LegendaryLang.Parse.Expressions;


public class StructCreationExpression : IExpression
{
    public class StructFieldTokenIsNullException : ParseException
    {
        public Token LookUpToken { get; }

        public StructFieldTokenIsNullException(Token lookUpToken)
        {
            LookUpToken = lookUpToken;
        }

        public override string Message => $"Expected struct all fields to have names\n{LookUpToken?.GetLocationStringRepresentation()}";
    }
    public static  StructCreationExpression Parse(Parser parser, NormalLangPath path)
    {

        var curlyBrace = CurlyBrace.ParseLeft(parser);
        var token = parser.Peek();
        var assignments = new List<AssignVariableExpression>();
        while (token is not RightCurlyBraceToken)
        {
            var identTok = Identifier.Parse(parser);
            var fieldAssignment = AssignVariableExpression.Parse(parser,new PathExpression(
                new NormalLangPath(identTok,[identTok.Identity])));
            token = parser.Peek();
            if (token is CommaToken)
            {
                Comma.Parse(parser);
                token = parser.Peek();
            }
            
            assignments.Add(fieldAssignment);
        }
        var curlyBrace2 = CurlyBrace.Parseight(parser);
        var variableAssignments = assignments.Select(i => new AssignedField()
        {
            EqualsTo = i.EqualsTo,
            FieldToken = ((i.Assigner as PathExpression)?.Path as NormalLangPath)?.FirstIdentifierToken
        }).ToArray();
        foreach (var assignment in variableAssignments)
        {
            if (assignment.FieldToken is null)
            {
                throw new StructFieldTokenIsNullException(curlyBrace2);
            }
        }
        return new StructCreationExpression(path, variableAssignments );
    }
    

    public ImmutableArray<AssignedField> AssignFields { get; }
    public Token LookUpToken { get; }
    public void Analyze( SemanticAnalyzer analyzer)
    {
        TypePath = TypePath.GetAsShortCutIfPossible(analyzer);
        var str = analyzer.GetDefinition(TypePath);
        if (str is null)
        {
            throw new ParseException($"No definition found for {TypePath}\n{LookUpToken.GetLocationStringRepresentation()}");
        }
        var asStruct = str as StructTypeDefinition;
        if (asStruct is null)
        {
            throw new ParseException($"Expected struct type but found {str.FullPath}\n{LookUpToken.GetLocationStringRepresentation()}");
        }

        if (AssignFields.Length != asStruct.Fields.Length)
        {
            throw new ParseException($"Not all fields are assigned to the instance '{TypePath}'\nfields: " +
                                     $"{string.Join(",", asStruct.Fields.AsEnumerable().Where(i => !AssignFields.Select(j => j.FieldToken.Identity).Contains(i.Name))
                                         .Select(i =>$"{i.Name}: {i.TypePath}"))}\n{LookUpToken.GetLocationStringRepresentation()}");
        }

        var invalidFields = new List<AssignedField>();
        foreach (var field in AssignFields)
        {
            if (!asStruct.Fields.Any(i => i.Name == field.FieldToken.Identity))
            {
                invalidFields.Add(field);
            }
        }

        if (invalidFields.Any())
        {
            throw new ParseException($"The following fields are not part of type '{TypePath}':\n{string.Join("\n",invalidFields.Select(i => i.FieldToken.Identity))}" +
                                     $"\n{LookUpToken.GetLocationStringRepresentation()}");
        }
        foreach (var i in AssignFields)
        {
            i.EqualsTo.Analyze(analyzer);
        }

    }

    public class AssignedField
    {
        public IdentifierToken FieldToken { get; init; }
        public IExpression EqualsTo { get; init; }
    }
    public StructCreationExpression( LangPath typePath, IEnumerable<AssignedField> assignVariableExpressions)
    {
        TypePath = typePath;
        AssignFields = assignVariableExpressions.ToImmutableArray();
        LookUpToken = typePath.FirstIdentifierToken!;
    }
    private VariableRefItem? GeneratedDataRef { get; set; }
    public IEnumerable<LangPath> GetAllTypesUsed(MonomorphizationHelper helper)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return AssignFields.SelectMany(i => i.EqualsTo?.GetAllFunctionsUsed());
    }

    public unsafe VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {

        var typeRef = (codeGenContext.GetRefItemFor(TypePath) as TypeRefItem);
        var structType = typeRef?.Type as StructType;

        LLVMValueRef structPtr = codeGenContext.Builder.BuildAlloca(structType.TypeRef);
        
        
        for (var i = 0; i < structType.Fields.Length; i++)
        {
            
            // assigns each field pretty much
            var gotten = AssignFields
                .First(j => j.FieldToken.Identity == structType.Fields[i].Name);
            var data = gotten.EqualsTo.DataRefCodeGen(codeGenContext);
            var fieldPtr = codeGenContext.Builder.BuildStructGEP2(structType.TypeRef,structPtr,(uint)i);

            data.Type.AssignTo(codeGenContext, data, new VariableRefItem()
            {
                Type = data.Type,
                ValueRef = fieldPtr
            });

        }
        
        
        return new VariableRefItem()
        {
            Type = structType,
            ValueRef = structPtr
        };

    }

    
    public LangPath TypePath { get; protected set; }
}