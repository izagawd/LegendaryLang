using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions;

public class Function : IConcreteDefinition,  IPathResolvable
{
    public Function(FunctionDefinition functionDefinition, IEnumerable<LangPath> genericArguments, LLVMValueRef functionValueRef, LLVMTypeRef functionTypeRef, Type returnType,
        NormalLangPath fullPath)
    {
        Definition = functionDefinition;
        FunctionValueRef = functionValueRef;
        FunctionType = functionTypeRef;
        GenericArguments = genericArguments.ToImmutableArray();
        ReturnType = returnType;
        FullPath = fullPath;
    }

    public FunctionDefinition Definition { get; }
    
    public Type ReturnType { get; }

    public ImmutableArray<Variable> Arguments { get; set; }

    public BlockExpression BlockExpression => Definition.BlockExpression;
    public LLVMTypeRef FunctionType { get;  }
    public LLVMValueRef FunctionValueRef { get;  }


    public ImmutableArray<LangPath> GenericArguments { get; }
    IDefinition? IConcreteDefinition.Definition => Definition;

    public NormalLangPath FullPath { get; private set; }
    LangPath IDefinition.TypePath => FullPath;
    public unsafe void CodeGen(CodeGenContext context)
    {
        context.AddScope();
        for (int i = 0; i < GenericArguments.Length; i++)
        {
            context.AddToDeepestScope(new NormalLangPath(null,[ Definition.GenericParameters[i].Name]),
                context.GetRefItemFor(GenericArguments[i]));
        }

        LLVMBasicBlockRef entryBlock = FunctionValueRef.AppendBasicBlock("entry"); 
        LLVM.PositionBuilderAtEnd(context.Builder, entryBlock);

        var argumentsToMonomorphize = new Variable[Definition.Arguments.Length];

        for (uint i = 0; i < (uint)Definition.Arguments.Length; i++)
        {
            var argument = Definition.Arguments[(int)i];
            var argType = context.GetRefItemFor(argument.TypePath) as TypeRefItem;
            argumentsToMonomorphize[i] = new Variable
            {
                Type = argType.Type,
                Name = argument.Name
            };
        }

        
        Arguments = argumentsToMonomorphize.ToImmutableArray();
        var paramTypes = Arguments.Select(i => i.Type.TypeRef).ToImmutableArray();
        // 6. For each parameter, allocate space and store the parameter into it.
        for (uint i = 0; i < (uint)Definition.Arguments.Length; i++)
        {
            var argument = argumentsToMonomorphize[i];

            // Get the function parameter.
            LLVMValueRef param = LLVM.GetParam(FunctionValueRef, i);

            // Allocate space for the parameter in the entry block.
            LLVMValueRef alloca = context.Builder.BuildAlloca(paramTypes[(int)i], argument.Name);
            argument.Type.AssignTo(context, new ValueRefItem
            {
                Type = argument.Type,
                ValueRef = param
            }, new ValueRefItem
            {
                Type = argument.Type,
                ValueRef = alloca
            });

            // adds the stack ptr to codegen so argument can be referenced by name
            context.AddToDeepestScope(new NormalLangPath(null, [argument.Name]), new ValueRefItem
            {
                Type = (context.GetRefItemFor(argument.Type.TypePath) as TypeRefItem).Type,
                ValueRef = alloca
            });
        }
        // sets arguments post monomorphization (eg converting T to i32)


        var gennedVal = BlockExpression.CodeGen(context);
        var built = context.Builder.BuildRet(gennedVal.LoadValue(context));


        context.PopScope();
    }

    public IEnumerable<ISyntaxNode> Children => [BlockExpression];


    public void ResolvePaths(PathResolver resolver)
    {
        
        
    }

    public Token? Token => Definition.Token;


    public string Name => Definition.Name;
    public NormalLangPath Module => Definition.Module;
    public bool HasBeenGened { get; set; }


}