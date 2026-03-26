using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Expressions;

public class DerefExpression : IExpression
{
    public DerefExpression(IExpression inner, Token token)
    {
        Inner = inner;
        DerefToken = token;
    }

    public IExpression Inner { get; }
    public Token DerefToken { get; }
    public IEnumerable<ISyntaxNode> Children => [Inner];
    public Token Token => DerefToken;

    public LangPath? TypePath { get; private set; }

    public void Analyze(SemanticAnalyzer analyzer)
    {
        Inner.Analyze(analyzer);

        // Check that inner is a reference type (std::pointer::immut::<T> or std::pointer::mut::<T>)
        if (Inner.TypePath is NormalLangPath nlp)
        {
            var pointerModule = PointerTypeDefinition.GetPointerModule();
            // Check if path starts with std::pointer
            if (nlp.PathSegments.Length >= 3
                && nlp.Contains(pointerModule))
            {
                // Inner type is the generic arg
                var generics = nlp.GetFrontGenerics();
                if (generics.Length == 1)
                {
                    TypePath = generics[0];

                    // Cannot move out of a shared reference if the type is not Copy
                    if (!analyzer.IsTypeCopy(TypePath))
                    {
                        analyzer.AddException(new SemanticException(
                            $"Cannot move out of shared reference '&{TypePath}' — type '{TypePath}' does not implement Copy\n{Token.GetLocationStringRepresentation()}"));
                    }
                    return;
                }
            }
        }

        analyzer.AddException(new SemanticException(
            $"Cannot dereference non-reference type '{Inner.TypePath}'\n{Token.GetLocationStringRepresentation()}"));
        TypePath = Inner.TypePath;
    }

    public ValueRefItem CodeGen(CodeGenContext codeGenContext)
    {
        var innerVal = Inner.CodeGen(codeGenContext);
        var ptrType = innerVal.Type as PointerType;
        if (ptrType == null)
            throw new InvalidOperationException("Cannot dereference non-pointer type");

        // Load the pointer value (the address)
        var ptrVal = ptrType.LoadValue(codeGenContext, innerVal);
        var pointeeType = ptrType.PointingToType;

        // Return a ValueRefItem pointing to the dereferenced location
        // The pointer value IS the address of the pointee, so it serves as the "alloca" for the pointee
        return new ValueRefItem
        {
            Type = pointeeType,
            ValueRef = ptrVal
        };
    }

    public bool HasGuaranteedExplicitReturn => Inner.HasGuaranteedExplicitReturn;
}
