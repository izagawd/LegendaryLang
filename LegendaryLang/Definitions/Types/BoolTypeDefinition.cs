using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = System.Type;

namespace LegendaryLang.Parse.Types;

public class BoolTypeDefinition : PrimitiveTypeDefinition
{
    public override ConcreteDefinition.Type? Monomorphize(CodeGenContext context, LangPath langPath)
    {
        var arg = GetGenericArguments(langPath);
        if (arg is null)
        {
            return null;
        }
        var boolType = new BoolType(this);
        boolType.CodeGen(context);
        return boolType;
    }

    public override ImmutableArray<LangPath>? GetGenericArguments(LangPath langPath)
    {
        if (langPath != (this as IDefinition).FullPath)
        {
            return null;
        }

        return [];
    }

    public override string Name => "bool";
    public override LangPath TypePath { get; }
    public override Token LookUpToken => null;


    public override void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }
}