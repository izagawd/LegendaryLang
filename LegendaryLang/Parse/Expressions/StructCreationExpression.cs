using System.Collections.Immutable;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp;
using LLVMSharp.Interop;
using StructType = LegendaryLang.ConcreteDefinition.StructType;

namespace LegendaryLang.Parse.Expressions;


public class StructCreationExpression : IExpression, IPathHaver
{
    public IEnumerable<ISyntaxNode> Children => AssignFields.Select(i => i.EqualsTo);
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
    public Token Token { get; }
    public void SetFullPathOfShortCutsDirectly(SemanticAnalyzer analyzer)
    {
        TypePath = TypePath.GetFromShortCutIfPossible(analyzer);
    }
    public void Analyze( SemanticAnalyzer analyzer)
    {

        foreach (var i in AssignFields)
        {
            i.EqualsTo.Analyze(analyzer);
        }
        var str = analyzer.GetDefinition(TypePath);
        if (str is null)
        {
            analyzer.AddException( new SemanticException($"No definition found for {TypePath}\n{Token.GetLocationStringRepresentation()}"));
            return;
        }
        var asStruct = str as StructTypeDefinition;
        if (asStruct is null)
        {
            analyzer.AddException(new SemanticException($"Expected struct type but found {str.FullPath}\n{Token.GetLocationStringRepresentation()}"));
            return;
        }

        if (AssignFields.Length != asStruct.Fields.Length)
        {
            analyzer.AddException(new SemanticException($"Not all fields are assigned to the instance '{TypePath}'\nthe following fields are missing:\n" +
                                                        $"{string.Join("\n", asStruct.Fields.AsEnumerable().Where(i => !AssignFields.Select(j => j.FieldToken.Identity).Contains(i.Name))
                                                            .Select(i =>$"{i.Name}: {i.TypePath}"))}\n\n" +
                                                        $"{Token.GetLocationStringRepresentation()}"));
          
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
            analyzer.AddException(new SemanticException($"The following fields are not part of type '{TypePath}':\n{string.Join("\n",invalidFields.Select(i => i.FieldToken.Identity))}" +
                                                              $"\n{Token.GetLocationStringRepresentation()}"));
            
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
        Token = typePath.FirstIdentifierToken!;
    }
    private ValueRefItem? GeneratedDataRef { get; set; }



    public unsafe ValueRefItem DataRefCodeGen(CodeGenContext codeGenContext)
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

            data.Type.AssignTo(codeGenContext, data, new ValueRefItem()
            {
                Type = data.Type,
                ValueRef = fieldPtr
            });

        }
        
        
        return new ValueRefItem()
        {
            Type = structType,
            ValueRef = structPtr
        };

    }

    
    public LangPath TypePath { get; protected set; }
}