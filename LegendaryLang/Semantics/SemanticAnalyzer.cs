using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Parse;
using LegendaryLang.Parse.Expressions;

namespace LegendaryLang.Semantics;

public class SemanticException : Exception
{
    public SemanticException(string message) : base(message)
    {
    }

    public SemanticException()
    {
    }
}

public class TraitBoundViolationException : SemanticException
{
    public LangPath TypePath { get; }
    public LangPath TraitPath { get; }
    public TraitBoundViolationException(LangPath typePath, LangPath traitPath)
        : base($"The type '{typePath}' does not implement trait '{traitPath}'")
    {
        TypePath = typePath;
        TraitPath = traitPath;
    }
}

public class TraitNotFoundException : SemanticException
{
    public LangPath TraitPath { get; }
    public TraitNotFoundException(LangPath traitPath, string location)
        : base($"Trait '{traitPath}' not found\n{location}")
    {
        TraitPath = traitPath;
    }
}

public class TraitMethodNotImplementedException : SemanticException
{
    public string MethodName { get; }
    public LangPath TraitPath { get; }
    public TraitMethodNotImplementedException(string methodName, LangPath traitPath, string location)
        : base($"Method '{methodName}' from trait '{traitPath}' is not implemented\n{location}")
    {
        MethodName = methodName;
        TraitPath = traitPath;
    }
}

public class TraitExtraMethodException : SemanticException
{
    public string MethodName { get; }
    public LangPath TraitPath { get; }
    public TraitExtraMethodException(string methodName, LangPath traitPath, string location)
        : base($"Method '{methodName}' is not defined in trait '{traitPath}'\n{location}")
    {
        MethodName = methodName;
        TraitPath = traitPath;
    }
}

public class FunctionNotFoundException : SemanticException
{
    public LangPath FunctionPath { get; }
    public FunctionNotFoundException(LangPath functionPath, string location)
        : base($"Cannot find function {functionPath}\n{location}")
    {
        FunctionPath = functionPath;
    }
}

public class TypeMismatchException : SemanticException
{
    public LangPath ExpectedType { get; }
    public LangPath FoundType { get; }
    public string Context { get; }
    public TypeMismatchException(LangPath expectedType, LangPath foundType, string context, string location)
        : base($"{context}: expected '{expectedType}', found '{foundType}'\n{location}")
    {
        ExpectedType = expectedType;
        FoundType = foundType;
        Context = context;
    }
}

public class GenericParamCountException : SemanticException
{
    public int Expected { get; }
    public int Found { get; }
    public GenericParamCountException(int expected, int found, string location)
        : base($"Incorrect number of generic parameters: {found}, expected: {expected}\n{location}")
    {
        Expected = expected;
        Found = found;
    }
}

public class UndefinedVariableException : SemanticException
{
    public LangPath VariablePath { get; }
    public UndefinedVariableException(LangPath variablePath, string location)
        : base($"Path to variable '{variablePath}' not found, or the path is not a variable\n{location}")
    {
        VariablePath = variablePath;
    }
}

public class ReturnTypeMismatchException : SemanticException
{
    public LangPath ExpectedType { get; }
    public LangPath FoundType { get; }
    public ReturnTypeMismatchException(LangPath expectedType, LangPath foundType, string location)
        : base($"Return type of function does not match its definition. Expected: '{expectedType}', found: '{foundType}'\n{location}")
    {
        ExpectedType = expectedType;
        FoundType = foundType;
    }
}

public class UseAfterMoveException : SemanticException
{
    public LangPath VariablePath { get; }
    public UseAfterMoveException(LangPath variablePath, string location)
        : base($"Use of moved value '{variablePath}'\n{location}")
    {
        VariablePath = variablePath;
    }
}


public class SemanticAnalyzer
{
    private readonly Stack<Dictionary<LangPath, IDefinition>> DefinitionsStackMap = [];
    private readonly List<SemanticException> Exceptions = [];

    
    private readonly Stack<Dictionary<LangPath, LangPath>> VariableToTypeMapper = new();

    /// <summary>
    /// Tracks variables that have been moved (consumed by assignment or function call).
    /// Key is the simple variable name.
    /// </summary>
    private readonly HashSet<string> MovedVariables = new();

    /// <summary>
    /// When true, PathExpression.Analyze skips the move check.
    /// Used by AssignVariableExpression so the LHS of assignment doesn't
    /// trigger use-after-move (assignment restores usability).
    /// </summary>
    public bool SuppressMoveChecks { get; set; }

    /// <summary>
    /// Tracks trait bounds for generic parameters currently in scope.
    /// Maps generic param name -> (traitPath, genericParamName)
    /// </summary>
    private readonly Stack<List<(LangPath traitPath, string genericParamName)>> TraitBoundsStack = new();

    /// <summary>
    /// All impl definitions for trait method validation
    /// </summary>
    public List<ImplDefinition> ImplDefinitions { get; } = new();

    public Stack<ParseResult> ParseResults = new();

    public SemanticAnalyzer(IEnumerable<ParseResult> parseResults)
    {
        ParseResults = new Stack<ParseResult>(parseResults);
    }

    public void PushTraitBounds(IEnumerable<(LangPath traitPath, string genericParamName)> bounds)
    {
        TraitBoundsStack.Push(bounds.ToList());
    }

    public void PopTraitBounds()
    {
        if (TraitBoundsStack.Count > 0)
            TraitBoundsStack.Pop();
    }

    /// <summary>
    /// Gets all trait definitions associated with trait bounds for a given generic param name
    /// </summary>
    public IEnumerable<TraitDefinition> GetTraitBoundsFor(string genericParamName)
    {
        foreach (var bounds in TraitBoundsStack)
            foreach (var (traitPath, paramName) in bounds)
                if (paramName == genericParamName)
                    if (GetDefinition(traitPath) is TraitDefinition td)
                        yield return td;
    }

    /// <summary>
    /// Checks if a path resolves to a trait method call (TraitName::method)
    /// and returns the method's return type path
    /// </summary>
    public LangPath? ResolveTraitMethodReturnType(NormalLangPath path)
    {
        if (path.PathSegments.Length < 2) return null;
        var lastSeg = path.GetLastPathSegment();
        if (lastSeg == null) return null;
        var methodName = lastSeg.ToString();
        var parentPath = path.Pop();
        if (parentPath == null || parentPath.PathSegments.Length == 0) return null;

        // Case 1: TraitName::method — parent is a trait directly
        var traitDef = GetDefinition(parentPath) as TraitDefinition;

        // Case 2: T::method — parent is a generic param with trait bound(s)
        if (traitDef == null && parentPath is NormalLangPath nlpParent && nlpParent.PathSegments.Length == 1)
        {
            var paramName = nlpParent.PathSegments[0].ToString();
            // Search all bounds for this param to find one that has the method
            traitDef = GetTraitBoundsFor(paramName)
                .FirstOrDefault(td => td.GetMethod(methodName) != null);
        }

        // Case 3: ConcreteType::method — parent is a concrete type, search impls for a trait with the method
        if (traitDef == null)
        {
            var typeDef = GetDefinition(parentPath);
            if (typeDef != null && typeDef is not TraitDefinition)
            {
                // Search all impls where ForTypePath pattern-matches this type
                foreach (var impl in ImplDefinitions)
                {
                    var bindings = impl.TryMatchConcreteType(parentPath);
                    if (bindings != null && impl.CheckBounds(bindings, this))
                    {
                        var implTraitDef = GetDefinition(impl.TraitPath) as TraitDefinition;
                        if (implTraitDef?.GetMethod(methodName) != null)
                        {
                            traitDef = implTraitDef;
                            var method = traitDef.GetMethod(methodName);
                            var returnType = method!.ReturnTypePath;
                            if (returnType is NormalLangPath nlpSelf && nlpSelf.PathSegments.Length == 1
                                && nlpSelf.PathSegments[0].ToString() == "Self")
                            {
                                return parentPath;
                            }
                            return returnType;
                        }
                    }
                }
            }
        }

        if (traitDef == null) return null;

        var foundMethod = traitDef.GetMethod(methodName);
        if (foundMethod == null) return null;

        var foundReturnType = foundMethod.ReturnTypePath;

        // If the return type is "Self", substitute it with the generic parameter
        // that has this trait as its bound
        if (foundReturnType is NormalLangPath nlp && nlp.PathSegments.Length == 1
            && nlp.PathSegments[0].ToString() == "Self")
        {
            var traitTypePath = (traitDef as IDefinition).TypePath;
            foreach (var bounds in TraitBoundsStack)
                foreach (var (tp, paramName) in bounds)
                    if (tp == traitTypePath)
                        return new NormalLangPath(null, [paramName]);
        }

        return foundReturnType;
    }

    /// <summary>
    /// Checks whether a type has an impl block for the given trait.
    /// Handles both non-generic and generic impls via pattern matching.
    /// </summary>
    public bool TypeImplementsTrait(LangPath typePath, LangPath traitPath)
    {
        return ImplDefinitions.Any(i =>
        {
            if (i.TraitPath != traitPath) return false;
            var bindings = i.TryMatchConcreteType(typePath);
            if (bindings == null) return false;
            return i.CheckBounds(bindings, this);
        });
    }

    /// <summary>
    /// Checks whether a type implements the Copy trait.
    /// Copy types are bitwise-copied on assignment; non-Copy types are moved.
    /// Also recognizes generic params that have Copy as a trait bound.
    /// </summary>
    public bool IsTypeCopy(LangPath? typePath)
    {
        if (typePath == null) return true;
        var copyPath = GetCopyTraitPath();
        if (copyPath == null) return true;

        // Check concrete impl
        if (TypeImplementsTrait(typePath, copyPath)) return true;

        // Check if this is a generic param with a Copy trait bound
        if (typePath is NormalLangPath nlp && nlp.PathSegments.Length == 1)
        {
            var paramName = nlp.PathSegments[0].ToString();
            foreach (var bounds in TraitBoundsStack)
                foreach (var (tp, pName) in bounds)
                    if (pName == paramName && tp == copyPath)
                        return true;
        }

        return false;
    }

    /// <summary>
    /// The canonical path of the Copy marker trait: std::core::marker::Copy
    /// </summary>
    public static readonly NormalLangPath CopyTraitPath =
        new(null, new NormalLangPath.PathSegment[] { "std", "core", "marker", "Copy" });

    private LangPath? GetCopyTraitPath()
    {
        return CopyTraitPath;
    }

    public void MarkAsMoved(string variableName)
    {
        MovedVariables.Add(variableName);
    }

    public void UnmarkMoved(string variableName)
    {
        MovedVariables.Remove(variableName);
    }

    public bool IsMoved(string variableName)
    {
        return MovedVariables.Contains(variableName);
    }

    /// <summary>
    /// If the expression is a simple variable reference to a non-Copy type, mark it as moved.
    /// </summary>
    public void TryMarkExpressionAsMoved(IExpression expr)
    {
        if (expr is PathExpression pe && expr.TypePath != null && !IsTypeCopy(expr.TypePath))
        {
            if (pe.Path is NormalLangPath nlp && nlp.PathSegments.Length == 1)
            {
                MarkAsMoved(nlp.PathSegments[0].ToString());
            }
        }
    }

    public void AddException(SemanticException exception)
    {
        Exceptions.Add(exception);
    }


    public IDefinition? GetDefinition(LangPath langPath)
    {
        foreach (var def in DefinitionsStackMap)
        {
            if (def.TryGetValue(langPath, out var definition)) return definition;
        }
        return null;
    }

    public LangPath? GetVariableTypePath(LangPath variableLangPath)
    {
        foreach (var scope in VariableToTypeMapper)
            if (scope.TryGetValue(variableLangPath, out var symbol))
                return symbol;

        return null;
    }

    public void RegisterVariableType(LangPath variableLangPath, LangPath typPath, int? scope = null)
    {
        if (scope is null)
            VariableToTypeMapper.Peek().Add(variableLangPath, typPath);
        else
            VariableToTypeMapper.Reverse().Skip(scope.Value).First().Add(variableLangPath, typPath);
    }

    public void RegisterDefinitionAtDeepestScope(LangPath path, IDefinition definition)
    {
        DefinitionsStackMap.Peek()[path] = definition;

    }

    public void AddScope()
    {

        DefinitionsStackMap.Push(new ());
        VariableToTypeMapper.Push(new Dictionary<LangPath, LangPath>());
    }

    


    public void PopScope()
    {   
        DefinitionsStackMap.Pop();
     
        VariableToTypeMapper.Pop();
    }

    private void ResolvePaths()
    {
        var pathShortcutContext = new PathResolver();
        
        pathShortcutContext.AddScope();
        var primitiveParsed = ParseResults.First(i => i.Items.Any(j => j is I32TypeDefinition));
        foreach (var i in primitiveParsed.Items.OfType<PrimitiveTypeDefinition>())
        {
            var usings = new UseDefinition((NormalLangPath)i.TypePath, null);
            usings.RegisterUsings(pathShortcutContext);
        }
        // Register std library definitions (like Copy) globally so they're available without 'use'
        // User definitions at deeper scopes will shadow these if names conflict
        foreach (var result in ParseResults.Where(r => r.File?.Path.StartsWith("std") == true))
            foreach (var def in result.Items.OfType<IDefinition>())
            {
                var usings = new UseDefinition((NormalLangPath)def.TypePath, null);
                usings.RegisterUsings(pathShortcutContext);
            }
        foreach (var result in ParseResults)
        {
            pathShortcutContext.AddScope();
            foreach (var i in result.Items.OfType<IDefinition>())
            {
                var usings = new UseDefinition((NormalLangPath) i.TypePath, null);
                usings.RegisterUsings(pathShortcutContext);
            }
            foreach (var useDefinition in result.Items.OfType<UseDefinition>())
            {
                useDefinition.RegisterUsings(pathShortcutContext);
            }
            foreach (var i in result.Items.OfType<IPathResolvable>())
            {
                i.ResolvePaths(pathShortcutContext);
            }
            pathShortcutContext.PopScope();
        }
        pathShortcutContext.PopScope();
    }
    /// <returns>Collection of semantic errors that occured</returns>
    public SemanticException[] Analyze()
    {
        AddScope();
        // registers path mapping
        foreach (var i in ParseResults.SelectMany(i => i.Items.OfType<IDefinition>()))
            RegisterDefinitionAtDeepestScope(i.TypePath, i);

        // Collect impl definitions
        foreach (var impl in ParseResults.SelectMany(i => i.Items.OfType<ImplDefinition>()))
        {
            ImplDefinitions.Add(impl);
            // Register each impl method as a definition so it can be found during codegen
            foreach (var method in impl.Methods)
                RegisterDefinitionAtDeepestScope(method.TypePath, method);
        }

        ResolvePaths();

        foreach (var result in ParseResults)
        {
            AddScope();
      
            foreach (var i in result.Items.OfType<IAnalyzable>())
            {
                i.Analyze(this);
            }
            PopScope();
        }
        PopScope();

        return Exceptions.ToArray();
    }
}