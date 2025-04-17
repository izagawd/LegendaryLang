using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;
using LegendaryLang.Semantics;
using LLVMSharp.Interop;

namespace LegendaryLang.Parse;

public class Function : IConcreteDefinition
{
    public FunctionDefinition Definition { get; }

    public LangPath ReturnType => Definition.ReturnType;
    public ImmutableArray<Variable> Arguments => Definition.Arguments;
    
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
                Type = (context.GetRefItemFor(argument) as TypeRefItem).Type,
            });
        }
        FullPath = Module.Append([
            Name, new NormalLangPath.GenericTypesPathSegment(
                Arguments.Select(i => (context.GetRefItemFor(i.TypePath) as TypeRefItem).Type.TypePath))
        ]);
          
        context.AddToOuterScope(new NormalLangPath(null,(this as IDefinition).FullPath), new FunctionRefItem()
        {
            Function = this,
        });

        // 1. Determine the LLVM return type.
        LLVMTypeRef llvmReturnType = (context.GetRefItemFor(ReturnType) as TypeRefItem).TypeRef;
        // 2. Gather LLVM types for each parameter.
        var paramTypes = new LLVMTypeRef[Arguments.Length];
        for (int i = 0; i < Arguments.Length; i++)
        {
            paramTypes[i] = (context.GetRefItemFor(Arguments[i].TypePath) as TypeRefItem).TypeRef;
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


        // 6. For each parameter, allocate space and store the parameter into it.
        for (uint i = 0; i < (uint)Arguments.Length; i++)
        {
            var argument = Arguments[(int)i];
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

            
            // adds the stack ptr to codegen so argument can be referenced by name
            context.AddToDeepestScope(new NormalLangPath(null,[argument.Name]), new VariableRefItem()
            {
                Type = (context.GetRefItemFor(argument.TypePath) as TypeRefItem).Type,
                ValueRef = alloca
            });

        }

        // 7. Generate code for the function body by codegen'ing the BlockExpression.
        var blockValue = BlockExpression.DataRefCodeGen(context);


        LLVM.BuildRet(context.Builder, blockValue.LoadValForRetOrArg(context));
        

        context.PopScope();
    }
    public Function(FunctionDefinition functionDefinition, IEnumerable<LangPath> genericArguments)
    {
        Definition = functionDefinition;
 
        GenericArguments = genericArguments.ToImmutableArray();
    }


    public ImmutableArray<LangPath> GenericArguments { get; }
    public Token? LookUpToken => Definition.LookUpToken;
    public void Analyze(SemanticAnalyzer analyzer)
    {
        throw new NotImplementedException();
    }

    public string Name => Definition.Name;
    public NormalLangPath Module => Definition.Module;
    public bool HasBeenGened { get; set; }
}