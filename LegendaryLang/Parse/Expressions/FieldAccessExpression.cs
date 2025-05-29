using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Statements;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class FieldAccessExpression : IExpression
{

    public static FieldAccessExpression Parse(Parser parser, IExpression caller)
    {
        DotParser.Parse(parser);
        var fieldIden = Identifier.Parse(parser);
        var field = new FieldAccessExpression(fieldIden,caller);
        var nextToken = parser.Peek();
        while (nextToken is DotToken)
        {
            DotParser.Parse(parser);
            fieldIden = Identifier.Parse(parser);
            field = new FieldAccessExpression(fieldIden,field);
            nextToken = parser.Peek();
        }
        return field;
    }
    public IEnumerable<ISyntaxNode> Children => [Caller];
    public Token Token => Field;
    public IdentifierToken Field { get; }
    public IExpression Caller { get; }

    public FieldAccessExpression(IdentifierToken field, IExpression caller)
    {
        Field = field;
        Caller = caller;
    }
    public void Analyze(SemanticAnalyzer analyzer)
    {
      
        Caller.Analyze(analyzer);
        var definition = analyzer.GetDefinition(Caller.TypePath);
        if (definition is not StructTypeDefinition structTypeDefinition)
        {
            analyzer.AddException(new SemanticException($"Type '{Caller.TypePath}' is not a struct, so field access does not make sense\n{Token.GetLocationStringRepresentation()}"));
            return;
        }

        if (!structTypeDefinition.Fields.Any(i => i.Name == Field.Identity))
        {
            analyzer.AddException(new SemanticException($"Type '{structTypeDefinition.Fields}' does not contain a field named '{Field.Identity}'\n{Token.GetLocationStringRepresentation()}"));
        }
        TypePath = structTypeDefinition.Fields.First(i => i.Name == Field.Identity).TypePath;
    }



    /// <summary>
    /// Returns the pointer to the accessed field
    /// </summary>
    public unsafe VariableRefItem DataRefCodeGen(CodeGenContext codeGenContext)
    {
        
 
            var variableRef = Caller.DataRefCodeGen(codeGenContext);
            
            
            var structType = variableRef?.Type as StructType;

            
            LLVMValueRef fieldPtr = codeGenContext.Builder.BuildStructGEP2(structType.TypeRef, variableRef.ValueRef,
                structType.GetIndexOfField(Field.Identity));

            var field = structType.GetField(Field.Identity);
            var fieldTypeRef = codeGenContext.GetRefItemFor(field.TypePath) as TypeRefItem;

            return new VariableRefItem()
            {
                Type = fieldTypeRef.Type,
            
                ValueRef = fieldPtr
            };
 
    }

    public LangPath? TypePath { get; set; }


}