using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public class BoolType : PrimitiveType
{
    public override Token LookUpToken { get; }
    public override void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }

    

    public override string Name => "bool";
    public override LLVMTypeRef TypeRef
    {
        get => LLVMTypeRef.Int1;
        protected set => throw new NotImplementedException();
    }
}