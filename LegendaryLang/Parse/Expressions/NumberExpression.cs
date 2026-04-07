using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class NumberExpression : IExpression
{
    private static readonly HashSet<string> NumericPrimitiveNames = ["i32", "u8", "usize"];

    public NumberExpression(NumberToken token)
    {
        Token = token;
    }

    public NumberToken Token { get; }
    public IEnumerable<ISyntaxNode> Children => [];
    Token ISyntaxNode.Token => Token;


    public unsafe ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var typeRefItem = codeGenContext.GetRefItemFor(TypePath) as TypeRefItem;
        var primitiveType = typeRefItem?.Type as PrimitiveType;
        var llvmType = primitiveType?.TypeRef ?? LLVM.Int32Type();

        return new ValueRefItem
        {
            ValueRef = LLVM.ConstInt(llvmType, ulong.Parse(Token.Number), 0),
            Type = primitiveType ?? (codeGenContext.GetRefItemFor(TypePath) as TypeRefItem).Type
        };
    }

    public LangPath? TypePath { get; set; } = new I32TypeDefinition().TypePath;

    /// <summary>
    /// Coerce this numeric literal to match a target type.
    /// Only coerces to numeric primitive types (i32, u8, usize).
    /// Returns true if coerced, false if target is not a numeric type.
    /// </summary>
    public bool TryCoerceToType(LangPath? targetType)
    {
        if (targetType is NormalLangPath nlp)
        {
            var lastName = nlp.GetLastPathSegment()?.ToString();
            if (lastName != null && NumericPrimitiveNames.Contains(lastName))
            {
                TypePath = targetType;
                return true;
            }
        }
        return false;
    }

    public void Analyze(SemanticAnalyzer analyzer)
    {
    }

    public static NumberToken ParseToken(Parser parser)
    {
        var gotten = parser.Pop();
        if (gotten is not NumberToken numberToken) throw new ExpectedParserException(parser, ParseType.Number, gotten);
        return numberToken;
    }

    public static NumberExpression Parse(Parser parser)
    {
        return new NumberExpression(ParseToken(parser));
    }

    public bool HasGuaranteedExplicitReturn => false;
}