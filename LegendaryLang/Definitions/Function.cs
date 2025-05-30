using System.Collections.Immutable;
using LegendaryLang.ConcreteDefinition;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;
using Type = LegendaryLang.ConcreteDefinition.Type;

namespace LegendaryLang.Definitions;

public class Function : IConcreteDefinition
{
    public FunctionDefinition Definition { get; }
    IDefinition? IConcreteDefinition.Definition => Definition;
    public Type ReturnType { get; set; }

    public ImmutableArray<Variable> Arguments { get; set; }
    
    public BlockExpression BlockExpression => Definition.BlockExpression;
    public LLVMTypeRef FunctionType {get; set;}
    public LLVMValueRef FunctionValueRef {get; set;}

    public NormalLangPath FullPath {get; private set;}

    public unsafe void CodeGen(CodeGenContext context)
    {


        context.AddScope();
        for (int i = 0; i < GenericArguments.Length; i++)
        {
            var argument = GenericArguments[i];

            context.AddToDeepestScope(new NormalLangPath(null,
                [Definition.GenericParameters[i].Name]) , new TypeRefItem()
            {
                Type = (context.GetRefItemFor(argument) as TypeRefItem).Type ?? throw new NullReferenceException(),
            });
        }

        ReturnType = (context.GetRefItemFor(Definition.ReturnTypePath) as TypeRefItem).Type;
        
        FullPath = Module.Append([
            Name, new NormalLangPath.GenericTypesPathSegment(
                Definition.GenericParameters.Select(i => (context.GetRefItemFor(new NormalLangPath(null,
                [i.Name])) as TypeRefItem).Type.TypePath))
        ]);

        // 1. Determine the LLVM return type.
        LLVMTypeRef llvmReturnType = (context.GetRefItemFor(Definition.ReturnTypePath) as TypeRefItem).TypeRef;
        // 2. Gather LLVM types for each parameter.
        var paramTypes = new LLVMTypeRef[Definition.Arguments.Length];
        for (int i = 0; i < Definition.Arguments.Length; i++)
        {
            paramTypes[i] = (context.GetRefItemFor(Definition.Arguments[i].TypePath) as TypeRefItem).TypeRef;
        }

        LLVMTypeRef functionType;
        // 3. Create the function type and add the function to the module.
        fixed (LLVMTypeRef* llvmFunctionType = paramTypes)
        {
             functionType = LLVM.FunctionType(llvmReturnType,(LLVMSharp.Interop.LLVMOpaqueType**) llvmFunctionType,(uint) paramTypes.Length, 0);
             FunctionType = functionType;
        }
        LLVMValueRef function = LLVM.AddFunction(context.Module,(this as IDefinition).FullPath.ToString().ToCString(), functionType);

        FunctionValueRef = function;

        LLVMBasicBlockRef entryBlock = LLVM.AppendBasicBlock(function, "entry".ToCString());
        LLVM.PositionBuilderAtEnd(context.Builder, entryBlock);

        var argumentsToMonomorphize = new Variable[Definition.Arguments.Length];

        for (uint i = 0; i < (uint)Definition.Arguments.Length; i++)
        {
            var argument = Definition.Arguments[(int)i];
            var argType = context.GetRefItemFor(argument.TypePath) as TypeRefItem;
            argumentsToMonomorphize[i] = new Variable()
            {
                Type = argType.Type,
                Name = argument.Name,
            };
        }
        Arguments = argumentsToMonomorphize.ToImmutableArray(); 
        // 6. For each parameter, allocate space and store the parameter into it.
        for (uint i = 0; i < (uint)Definition.Arguments.Length; i++)
        {

            var argument = argumentsToMonomorphize[i];
            
            // Get the function parameter.
            LLVMValueRef param = LLVM.GetParam(function, i);
            
            // Allocate space for the parameter in the entry block.
            LLVMValueRef alloca = LLVM.BuildAlloca(context.Builder, paramTypes[i],argument.Name.ToCString());
            argument.Type.AssignTo(context,new ValueRefItem()
            {
                Type = argument.Type,
                ValueRef = param,
            }, new ValueRefItem()
            {
                Type = argument.Type,
                ValueRef = alloca,
            });

            // adds the stack ptr to codegen so argument can be referenced by name
            context.AddToDeepestScope(new NormalLangPath(null,[argument.Name]), new ValueRefItem()
            {
                Type = (context.GetRefItemFor(argument.Type.TypePath) as TypeRefItem).Type,
                ValueRef = alloca
            });

        }
        // sets arguments post monomorphization (eg converting T to i32)


        var gennedVal = BlockExpression.DataRefCodeGen(context);
        LLVM.BuildRet(context.Builder,   gennedVal.LoadValForRetOrArg(context));
        

        context.PopScope();
    }
    public Function(FunctionDefinition functionDefinition, IEnumerable<LangPath> genericArguments)
    {
        Definition = functionDefinition;
 
        GenericArguments = genericArguments.ToImmutableArray();
    }


    public ImmutableArray<LangPath> GenericArguments { get; }
    public IEnumerable<ISyntaxNode> Children => [BlockExpression];

    public void SetFullPathOfShortCutsDirectly(SemanticAnalyzer analyzer)
    {
        
    }



    public Token? Token => Definition.Token;


    public string Name => Definition.Name;
    public NormalLangPath Module => Definition.Module;
    public bool HasBeenGened { get; set; }
}