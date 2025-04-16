using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;


namespace LegendaryLang.Parse;

public class  Function: ITopLevel, IDefinition
{
    public NormalLangPath Module { get; }
    public bool HasBeenGened { get; set; }

    public LLVMTypeRef FunctionType { get; set; }
    public LLVMValueRef FunctionValueRef { get; set; }
    public unsafe void CodeGen(CodeGenContext context)
        {
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
            context.AddToDeepestScope(new NormalLangPath(null,[Name]), new FunctionRefItem()
            {
                Function = this,
            });
       
            context.AddScope();

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

    public int Priority => 3;

    public Token? LookUpToken {get; }
    
    
    public void Analyze(SemanticAnalyzer analyzer)
    {

        foreach (var i in BlockExpression.SyntaxNodes)
        {
            i.Analyze(analyzer);
        }
    }
    public BlockExpression BlockExpression { get; }
    public readonly ImmutableArray<Variable> Arguments;
    public string Name { get; }
    public LangPath ReturnType { get; }

    public Function(string name, IEnumerable<Variable> variables, LangPath returnType, BlockExpression blockExpression, NormalLangPath module, Token? lookUpToken = null)
    {
        Arguments = variables.ToImmutableArray();
        Name = name;
        ReturnType = returnType;
        BlockExpression = blockExpression;
        LookUpToken = lookUpToken;
        Module = module;
    }

    public static Function Parse(Parser parser)
    {
        var token = parser.Pop();
        var variables = new List<Variable>();
        if (token is FnToken)
        {
            var name = Identifier.Parse(parser).Identity;
            Parenthesis.ParseLeft(parser);
            var nextToken = parser.Peek();
            while (nextToken is not RightParenthesisToken)
            {
                

                var parameter = Variable.Parse(parser);
                nextToken = parser.Peek();
                if (parameter.TypePath is null)
                {
                    throw new ExpectedParserException(parser,(ParseType.BaseLangPath), parameter.IdentifierToken);
                }
                variables.Add(parameter);
                if (nextToken is CommaToken)
                {
                    parser.Pop();
                }
                nextToken = parser.Peek();
            }
            parser.Pop();
            nextToken = parser.Peek();
            LangPath returnTyp = LangPath.VoidBaseLangPath;
            if (nextToken is RightPointToken)
            {
                parser.Pop();
                returnTyp = LangPath.Parse(parser);
            }
            return new Function(name, variables,returnTyp,Expressions.BlockExpression.Parse(parser),parser.File.Module);
        } else
        {
            throw new ExpectedParserException(parser,(ParseType.Fn), token);
        }
    }

    public Token Token { get; private set; }
}