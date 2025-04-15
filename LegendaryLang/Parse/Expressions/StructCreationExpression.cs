using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Parse.Types;
using LegendaryLang.Semantics;
using LLVMSharp;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;


public class StructCreationExpression : IExpression
{
    
    public static  StructCreationExpression Parse(Parser parser, NormalLangPath path)
    {

        CurlyBrace.ParseLeft(parser);
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
        CurlyBrace.Parseight(parser);
        var variableAssignments = assignments.Select(i => new AssignedField()
        {
            EqualsTo = i.EqualsTo,
            FieldToken = ((i.Assigner as PathExpression).Path as NormalLangPath).FirstIdentifierToken
        }).ToArray();
        foreach (var assignment in variableAssignments)
        {
            if (assignment.FieldToken is null)
            {
                throw new Exception("Field token is null");
            }
        }
        return new StructCreationExpression(path, variableAssignments );
    }
    public BaseLangPath StructTypePath { get; }


    public ImmutableArray<AssignedField> AssignFields { get; }
    public Token LookUpToken { get; }
    public void Analyze( SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }

    public class AssignedField
    {
        public IdentifierToken FieldToken { get; init; }
        public IExpression EqualsTo { get; init; }
    }
    public StructCreationExpression(BaseLangPath structTypePath, IEnumerable<AssignedField> assignVariableExpressions)
    {
        StructTypePath = structTypePath;
        AssignFields = assignVariableExpressions.ToImmutableArray();
        
    }
    private VariableRefItem? GeneratedDataRef { get; set; }
    public unsafe VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {

        var typeRef = (codeGenContext.GetRefItemFor(StructTypePath) as TypeRefItem);
        var structType = typeRef?.Type as Struct;
        
        LLVMValueRef structPtr = codeGenContext.Builder.BuildAlloca(structType.TypeRef);
        
        for (var i = 0; i < structType.Fields.Length; i++)
        {
            
            var gotten = AssignFields
                .First(j => j.FieldToken.Identity == structType.Fields[i].Name);
            var data = gotten.EqualsTo.DataRefCodeGen(codeGenContext);
            var pt = codeGenContext.Builder.BuildStructGEP2(structType.TypeRef,structPtr,(uint)i);
             codeGenContext.Builder.BuildStore(data.ValueRef, pt);
        }
        
        
        return new VariableRefItem()
        {
            Type = structType,
            ValueRef = structPtr,
            ValueClassification = ValueClassification.RValue
        };

    }

    public BaseLangPath? BaseLangPath { get; }
    public BaseLangPath SetTypePath(SemanticAnalyzer semanticAnalyzer)
    {
        throw new NotImplementedException();
    }
}