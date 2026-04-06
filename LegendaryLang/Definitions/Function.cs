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

    public BlockExpression? BlockExpression => Definition.BlockExpression;
    public LLVMTypeRef FunctionType { get;  }
    public LLVMValueRef FunctionValueRef { get;  }


    public ImmutableArray<LangPath> GenericArguments { get; }

    /// <summary>
    /// Resolved concrete argument types captured during CreateRefDefinition,
    /// when generic scopes are active. Used in CodeGen to avoid re-resolving
    /// paths like Wrapper&lt;T&gt; after the generic scope is gone.
    /// </summary>
    public ImmutableArray<Type>? ResolvedArgTypes { get; init; }

    /// <summary>
    /// For impl methods from generic impls: the impl's generic param bindings
    /// (e.g., T → i32). Replayed in CodeGen so types like Wrapper&lt;T&gt; resolve correctly.
    /// </summary>
    public Dictionary<string, LangPath>? ImplGenericBindings { get; set; }

    /// <summary>
    /// For impl methods from generic impls: the impl's generic parameters
    /// (needed to push trait bounds during CodeGen).
    /// </summary>
    public ImmutableArray<GenericParameter>? ImplGenericParameters { get; set; }

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

        // Replay impl generic bindings (e.g., T → i32 for impl[T:! type] Trait for Wrapper(T))
        if (ImplGenericBindings != null)
        {
            foreach (var (paramName, boundType) in ImplGenericBindings)
            {
                var boundRefItem = context.GetRefItemFor(boundType);
                if (boundRefItem != null)
                    context.AddToDeepestScope(new NormalLangPath(null, [paramName]), boundRefItem);
            }
        }

        // Push trait bounds so trait method calls resolve to the correct impl
        // Build monomorphized generic args for substitution
        var monoGenericArgs = new LangPath[Definition.GenericParameters.Length];
        for (int i = 0; i < Definition.GenericParameters.Length; i++)
            monoGenericArgs[i] = (context.GetRefItemFor(GenericArguments[i]) as TypeRefItem)?.Type.TypePath ?? GenericArguments[i];

        var traitBounds = new List<(LangPath, LangPath)>();
        for (int i = 0; i < Definition.GenericParameters.Length; i++)
        {
            var gp = Definition.GenericParameters[i];
            foreach (var bound in gp.TraitBounds)
            {
                // Substitute generic params in the bound (e.g., Add(T) → Add(i32))
                var resolvedBound = FieldAccessExpression.SubstituteGenerics(
                    bound.TraitPath, Definition.GenericParameters, monoGenericArgs.ToImmutableArray());
                var concreteType = monoGenericArgs[i];
                traitBounds.Add((resolvedBound, concreteType));
            }
        }

        // Also push impl generic parameter trait bounds
        if (ImplGenericParameters != null && ImplGenericBindings != null)
        {
            var implGps = ImplGenericParameters.Value;
            var implMonoArgs = new LangPath[implGps.Length];
            for (int i = 0; i < implGps.Length; i++)
                implMonoArgs[i] = ImplGenericBindings.TryGetValue(implGps[i].Name, out var bt)
                    ? (context.GetRefItemFor(bt) as TypeRefItem)?.Type.TypePath ?? bt
                    : new NormalLangPath(null, [implGps[i].Name]);

            for (int i = 0; i < implGps.Length; i++)
            {
                var gp = implGps[i];
                foreach (var bound in gp.TraitBounds)
                {
                    var resolvedBound = FieldAccessExpression.SubstituteGenerics(
                        bound.TraitPath, implGps, implMonoArgs.ToImmutableArray());
                    var concreteType = implMonoArgs[i];
                    traitBounds.Add((resolvedBound, concreteType));
                }
            }
        }

        context.PushTraitBounds(traitBounds);

        LLVMBasicBlockRef entryBlock = FunctionValueRef.AppendBasicBlock("entry"); 
        LLVM.PositionBuilderAtEnd(context.Builder, entryBlock);

        var argumentsToMonomorphize = new Variable[Definition.Arguments.Length];

        for (uint i = 0; i < (uint)Definition.Arguments.Length; i++)
        {
            var argument = Definition.Arguments[(int)i];
            Type argType;
            // Use pre-resolved types if available (handles impl generic methods where T is out of scope)
            if (ResolvedArgTypes != null && (int)i < ResolvedArgTypes.Value.Length)
            {
                argType = ResolvedArgTypes.Value[(int)i];
            }
            else
            {
                argType = (context.GetRefItemFor(argument.TypePath) as TypeRefItem).Type;
            }
            argumentsToMonomorphize[i] = new Variable
            {
                Type = argType,
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

            // For pointer/reference types, the param IS the raw pointer value —
            // just store it directly. AssignTo would incorrectly dereference it.
            if (argument.Type is RefType or RawPtrType)
            {
                context.Builder.BuildStore(param, alloca);
            }
            else
            {
                argument.Type.AssignTo(context, new ValueRefItem
                {
                    Type = argument.Type,
                    ValueRef = param
                }, new ValueRefItem
                {
                    Type = argument.Type,
                    ValueRef = alloca
                });
            }

            // Track the alloca for intrinsic codegen access
            argument.Alloca = alloca;

            // adds the stack ptr to codegen so argument can be referenced by name
            context.AddToDeepestScope(new NormalLangPath(null, [argument.Name]), new ValueRefItem
            {
                Type = argument.Type,
                ValueRef = alloca
            });

            // Register function parameters for drop if they implement Drop
            // or have fields that need dropping
            if (argument.Type.TypePath != null &&
                (context.IsTypeDrop(argument.Type.TypePath) || context.TypeHasDroppableFields(argument.Type.TypePath)))
            {
                context.RegisterDroppable(argument.Name, argument.Type.TypePath, alloca);
            }
        }
        // sets arguments post monomorphization (eg converting T to i32)

        // Check if this function is a compiler intrinsic (e.g., size_of, alloc, Box.New).
        // If so, emit specialized IR and skip the normal body.
        if (IntrinsicCodeGen.TryEmit(this, context))
        {
            context.PopTraitBounds();
            context.PopScope();
            return;
        }

        if (BlockExpression == null)
            throw new InvalidOperationException($"Cannot codegen bodyless function '{Definition.Name}' — must be an intrinsic");

        var gennedVal = BlockExpression.CodeGen(context);

        // Save the return value to a temp BEFORE drops run.
        // Drops may free memory that the return value points to (e.g., *box_val
        // returns a pointer into heap memory that Box.Drop will free).
        var returnVal = gennedVal.LoadValue(context);
        var returnTemp = context.Builder.BuildAlloca(gennedVal.Type.TypeRef, "ret_tmp");
        context.Builder.BuildStore(returnVal, returnTemp);

        // Emit drop calls for function parameters before returning
        context.EmitDropCalls();

        var result = context.Builder.BuildLoad2(gennedVal.Type.TypeRef, returnTemp, "ret_val");
        context.Builder.BuildRet(result);

        context.PopTraitBounds();
        context.PopScope();
    }

    public IEnumerable<ISyntaxNode> Children => BlockExpression != null ? [BlockExpression] : [];


    public void ResolvePaths(PathResolver resolver)
    {
        
        
    }

    public Token? Token => Definition.Token;


    public string Name => Definition.Name;
    public NormalLangPath Module => Definition.Module;


}