using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Definitions.Types;


public abstract class PointerTypeDefinition : TypeDefinition
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


    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath path)
    {
        throw new NotImplementedException();
    }

    public override Token Token { get; }
    public override void Analyze(SemanticAnalyzer analyzer)
    {
        
    }

}