using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Definitions.Types;


public class PointerTypeDefinition : TypeDefinition
{

    public PointerTypeDefinition(bool isMut)
    {
        IsMut =  isMut;
    

    }
    public static NormalLangPath GetPointerModule()
    {

        
        return new NormalLangPath(null, ["std", "pointer"]);
    }

    public static string GetPointerName(bool isMut)
    {
        return isMut ? "mut" : "immut";
    }

    public bool IsMut { get; }

    public override string Name => GetPointerName(IsMut);
    public override NormalLangPath Module => GetPointerModule();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="genericArguments">Pointers expect exactly 1 generic argument, which is the type its pointing to</param>
    /// <returns></returns>
    public override IRefItem CreateRefDefinition(CodeGenContext context, ImmutableArray<LangPath>  genericArguments)
    {
        var pointingToType = ((TypeRefItem)context.GetRefItemFor(genericArguments[0]));
        var typeRef
            = LLVMTypeRef.CreatePointer(pointingToType.TypeRef, 0);

        return new TypeRefItem()
        {
            Type = new ConcreteDefinition.PointerType(this, pointingToType.Type, typeRef),
            
        };
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        throw new NotImplementedException();
    }

    public override Token Token { get; }
    public override void Analyze(SemanticAnalyzer analyzer)
    {
        
    }

}