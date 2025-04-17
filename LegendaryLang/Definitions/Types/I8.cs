using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse.Types;

public class I8 : PrimitiveType
{
    public override Token LookUpToken { get; }
    public override void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }

    public override string Name => "i8";
    public override LLVMTypeRef TypeRef
    {
        get => LLVMTypeRef.Int8;
        protected set => throw new NotImplementedException();
    }
}