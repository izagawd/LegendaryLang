using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse.Expressions;

public class TupleCreationExpression : IExpression
{
    public TupleCreationExpression(LeftParenthesisToken token, IEnumerable<IExpression> composites)
    {
        Composites = composites.ToImmutableArray();
        Token = token;
    }

    public ImmutableArray<IExpression> Composites { get; set; }
    public IEnumerable<ISyntaxNode> Children => Composites;
    public Token Token { get; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        foreach (var i in Composites) i.Analyze(analyzer);
        TypePath = new TupleLangPath(Composites.Select(c => c.TypePath!));
    }

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var typeRef = codeGenContext.GetRefItemFor(TypePath!) as TypeRefItem;
        if (typeRef?.Type is not TupleType tupleType)
            throw new InvalidOperationException($"Cannot resolve tuple type '{TypePath}' during codegen.");

        var alloca = codeGenContext.Builder.BuildAlloca(tupleType.TypeRef);

        for (int i = 0; i < Composites.Length; i++)
        {
            var fieldVal = Composites[i].CodeGen(codeGenContext);
            var fieldPtr = codeGenContext.Builder.BuildStructGEP2(tupleType.TypeRef, alloca, (uint)i);
            fieldVal.Type.AssignTo(codeGenContext, fieldVal,
                new ValueRefItem { Type = fieldVal.Type, ValueRef = fieldPtr });
        }

        return new ValueRefItem { Type = tupleType, ValueRef = alloca };
    }

    public LangPath? TypePath { get; private set; }
    public bool IsTemporary => true; // fresh value
    public bool HasGuaranteedExplicitReturn => Composites.Any(i => i.HasGuaranteedExplicitReturn);
}