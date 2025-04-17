using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse;

public class Function : IConcreteDefinition
{
    public FunctionDefinition Definition { get; }
    IDefinition? IConcreteDefinition.Definition => Definition;
    public LangPath ReturnType { get; set; }

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
            var kk = (context.GetRefItemFor(argument) as TypeRefItem);
            context.AddToDeepestScope(new NormalLangPath(null,
                [Definition.GenericParameters[i].Name]) , new TypeRefItem()
            {
                Type = (context.GetRefItemFor(argument) as TypeRefItem).Type ?? throw new NullReferenceException(),
            });
        }
        FullPath = Module.Append([
            Name, new NormalLangPath.GenericTypesPathSegment(
                Definition.GenericParameters.Select(i => (context.GetRefItemFor(new NormalLangPath(null,
                [i.Name])) as TypeRefItem).Type.TypePath))
        ]);

        // 1. Determine the LLVM return type.
        LLVMTypeRef llvmReturnType = (context.GetRefItemFor(Definition.ReturnType) as TypeRefItem).TypeRef;
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

        var newArguments = new Variable[Definition.Arguments.Length];

        // 6. For each parameter, allocate space and store the parameter into it.
        for (uint i = 0; i < (uint)Definition.Arguments.Length; i++)
        {
            var argument = Definition.Arguments[(int)i];
            var argType = context.GetRefItemFor(argument.TypePath) as TypeRefItem;
            
            // Get the function parameter.
            LLVMValueRef param = LLVM.GetParam(function, i);
            
            // Allocate space for the parameter in the entry block.
            LLVMValueRef alloca = LLVM.BuildAlloca(context.Builder, paramTypes[i],argument.Name.ToCString());
            argType.Type.AssignTo(context,new VariableRefItem()
            {
                Type = argType.Type,
                ValueRef = param,
            }, new VariableRefItem()
            {
                Type = argType.Type,
                ValueRef = alloca,
            });
            newArguments[(int) i] = new Variable(argument.IdentifierToken, argType.Type.TypePath);
            argument.TypePath = argType.Type.TypePath;
            // adds the stack ptr to codegen so argument can be referenced by name
            context.AddToDeepestScope(new NormalLangPath(null,[argument.Name]), new VariableRefItem()
            {
                Type = (context.GetRefItemFor(argument.TypePath) as TypeRefItem).Type,
                ValueRef = alloca
            });

        }
        // sets arguments post monomorphization (eg converting T to i32)
        Arguments = newArguments.ToImmutableArray(); 

        var blockValue = BlockExpression.DataRefCodeGen(context);
        
        
        // sets return type post monomorphization (eg converting T to i32)
        ReturnType = blockValue.Type.TypePath;
        var returnVal = LLVM.BuildRet(context.Builder, blockValue.LoadValForRetOrArg(context));
        

        context.PopScope();
    }
    public Function(FunctionDefinition functionDefinition, IEnumerable<LangPath> genericArguments)
    {
        Definition = functionDefinition;
 
        GenericArguments = genericArguments.ToImmutableArray();
    }


    public ImmutableArray<LangPath> GenericArguments { get; }
    public IEnumerable<NormalLangPath> GetAllFunctionsUsed()
    {
        return BlockExpression.GetAllFunctionsUsed();
    }

    public Token? LookUpToken => Definition.LookUpToken;
    public void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }

    public string Name => Definition.Name;
    public NormalLangPath Module => Definition.Module;
    public bool HasBeenGened { get; set; }
}