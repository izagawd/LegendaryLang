using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Parse.Types;

public abstract class PrimitiveTypeDefinition : TypeDefinition
{
    public override LangPath TypePath =>LangPath.PrimitivePath.Append([Name]);
    public override NormalLangPath Module => LangPath.PrimitivePath;
}
public class I32TypeDefinition : PrimitiveTypeDefinition
{
    public override Type GenerateIncompleteMono(CodeGenContext context, LangPath langPath)
    {
        return new I32Type(this);
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath langPath)
    {
        if (langPath != (this as IDefinition).FullPath)
        {
            return null;
        }

        return [];
    }


        public override Token LookUpToken { get; }


        public override void Analyze(SemanticAnalyzer analyzer)
        {
            throw new NotImplementedException();
        }
        



    
        public override string Name => "i32";
}