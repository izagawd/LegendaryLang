using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Expressions;

using LegendaryLang.Semantics;
using LLVMSharp.Interop;


namespace LegendaryLang.Parse;

public class  Function: IDefinition
{
    public bool HasBeenGened { get; set; }

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
            }
            LLVMValueRef function = LLVM.AddFunction(context.Module, Name.ToCString(), functionType);

            // 4. Create an entry basic block and position the builder.
            LLVMBasicBlockRef entryBlock = LLVM.AppendBasicBlock(function, "entry".ToCString());
            LLVM.PositionBuilderAtEnd(context.Builder, entryBlock);

            // 5. (Optional) Create a new scope for function parameters.
            // If you have a scoped symbol table, push a new scope.
            context.AddRefScope();

            // 6. For each parameter, allocate space and store the parameter into it.
            for (uint i = 0; i < (uint)Arguments.Length; i++)
            {
                var argument = Arguments[(int)i];
                // Get the function parameter.
                LLVMValueRef param = LLVM.GetParam(function, i);
                
                // Allocate space for the parameter in the entry block.
                LLVMValueRef alloca = LLVM.BuildAlloca(context.Builder, paramTypes[i],argument.Name.ToCString());
                
                // Store the parameter value into the allocated space.
                 LLVM.BuildStore(context.Builder, param, alloca);
    
                
                // adds the stack ptr to codegen so argument can be referenced by name
                context.AddToTop(new NormalLangPath(null,[argument.Name]), new VariableRefItem()
                {
                    Type = (context.GetRefItemFor(argument.TypePath) as TypeRefItem).Type,
                    ValueRef = alloca,
                    ValueClassification = ValueClassification.LValue
                });

            }

            // 7. Generate code for the function body by codegen'ing the BlockExpression.
            var blockValue = BlockExpression.DataRefCodeGen(context);


            LLVM.BuildRet(context.Builder, blockValue.LoadValForRetOrArg(context));
            

            context.PopRefs();
        }

    public int Priority => 3;

    public Token LookUpToken => null;

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
    public BaseLangPath ReturnType { get; }

    public Function(string name, IEnumerable<Variable> variables, BaseLangPath returnType, BlockExpression blockExpression)
    {
        Arguments = variables.ToImmutableArray();
        Name = name;
        ReturnType = returnType;
        BlockExpression = blockExpression;
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
            BaseLangPath returnTyp = BaseLangPath.VoidBaseLangPath;
            if (nextToken is RightPointToken)
            {
                parser.Pop();
                returnTyp = BaseLangPath.Parse(parser);
            }
            return new Function(name, variables,returnTyp,Expressions.BlockExpression.Parse(parser));
        } else
        {
            throw new ExpectedParserException(parser,(ParseType.Fn), token);
        }
    }

    public Token Token { get; private set; }
}