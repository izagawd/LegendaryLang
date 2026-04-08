namespace LegendaryLang.Definitions;

/// <summary>
/// Scoped impl definitions for trait method resolution. Shared between
/// SemanticAnalyzer (validation) and CodeGenContext (codegen).
/// Push/pop scopes so impls defined inside blocks are only visible within that block.
/// </summary>
public class ScopedImplDefinitions
{
    private readonly Stack<List<ImplDefinition>> _stack = new();

    public IEnumerable<ImplDefinition> All => _stack.SelectMany(scope => scope);

    public void Add(ImplDefinition impl) => _stack.Peek().Add(impl);

    public void PushScope() => _stack.Push(new List<ImplDefinition>());

    public void PopScope() => _stack.Pop();
}
