using LLVMSharp.Interop;

namespace LegendaryLang.ConcreteDefinition;

public class Variable
{
    public required string Name { get; init; }
    public required Type Type { get; init; }

    /// <summary>
    /// Stack allocation for this variable's value, set during Function.CodeGen.
    /// Used by intrinsic codegen to access argument values directly.
    /// </summary>
    public LLVMValueRef Alloca { get; set; }
}