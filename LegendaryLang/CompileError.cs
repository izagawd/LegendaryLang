using LegendaryLang.Parse;

namespace LegendaryLang;

public abstract class CompileError
{
    public abstract string Message { get; }
    public override string ToString() => $"[{GetType().Name}] {Message}";
}

// ── Infrastructure errors ──

public class DirectoryNotFoundError : CompileError
{
    public required string Directory { get; init; }
    public override string Message => $"Directory '{Directory}' does not exist.";
}

public class MainFileNotFoundError : CompileError
{
    public required string Directory { get; init; }
    public override string Message => $"No main file found in '{Directory}'";
}

public class MainFunctionMissingError : CompileError
{
    public required string FilePath { get; init; }
    public override string Message => $"'fn main' function not found in {FilePath}";
}

public class MainReturnTypeError : CompileError
{
    public required string ExpectedType { get; init; }
    public required string FoundType { get; init; }
    public override string Message => $"'fn main' return type must be '{ExpectedType}', not '{FoundType}'";
}

public class MainArgumentsError : CompileError
{
    public override string Message => "'fn main' arguments are not empty";
}

public class CodeGenError : CompileError
{
    public override string Message => "Code generation failed (LLVM module verification or execution engine error)";
}

// ── Parse errors ──

public class ParseError : CompileError
{
    public required string Details { get; init; }
    public override string Message => Details;
}

// ── Semantic errors ──

/// <summary>
/// Fallback for any SemanticException that doesn't have a typed subclass
/// </summary>
public class GenericSemanticError : CompileError
{
    public required string Details { get; init; }
    public override string Message => Details;
}

public class TraitBoundViolationError : CompileError
{
    public required LangPath TypePath { get; init; }
    public required LangPath TraitPath { get; init; }
    public override string Message => $"The type '{TypePath}' does not implement trait '{TraitPath}'";
}

public class TraitNotFoundError : CompileError
{
    public required LangPath TraitPath { get; init; }
    public override string Message => $"Trait '{TraitPath}' not found";
}

public class TraitMethodNotImplementedError : CompileError
{
    public required string MethodName { get; init; }
    public required LangPath TraitPath { get; init; }
    public override string Message => $"Method '{MethodName}' from trait '{TraitPath}' is not implemented";
}

public class TraitExtraMethodError : CompileError
{
    public required string MethodName { get; init; }
    public required LangPath TraitPath { get; init; }
    public override string Message => $"Method '{MethodName}' is not defined in trait '{TraitPath}'";
}

public class FunctionNotFoundError : CompileError
{
    public required LangPath FunctionPath { get; init; }
    public override string Message => $"Cannot find function {FunctionPath}";
}

public class TypeMismatchError : CompileError
{
    public required LangPath ExpectedType { get; init; }
    public required LangPath FoundType { get; init; }
    public required string Context { get; init; }
    public override string Message => $"{Context}: expected '{ExpectedType}', found '{FoundType}'";
}

public class UndefinedVariableError : CompileError
{
    public required LangPath VariablePath { get; init; }
    public override string Message => $"Path to variable '{VariablePath}' not found, or the path is not a variable";
}

public class GenericParamCountError : CompileError
{
    public required int Expected { get; init; }
    public required int Found { get; init; }
    public override string Message => $"Incorrect number of generic parameters: {Found}, expected: {Expected}";
}

public class ReturnTypeMismatchError : CompileError
{
    public required LangPath ExpectedType { get; init; }
    public required LangPath FoundType { get; init; }
    public override string Message => $"Return type of function does not match its definition. Expected: '{ExpectedType}', found: '{FoundType}'";
}

public class MissingSemicolonError : CompileError
{
    public override string Message => "Expected semicolon";
}

public class FieldNotFoundError : CompileError
{
    public required string FieldName { get; init; }
    public required LangPath TypePath { get; init; }
    public override string Message => $"Field '{FieldName}' not found on type '{TypePath}'";
}
