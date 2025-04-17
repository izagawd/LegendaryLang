namespace LegendaryLang.Parse;

public class Function
{
    public Function(FunctionDefinition functionDefinition, IEnumerable<LangPath> genericArguments)
    {
        FunctionDefinition = functionDefinition;
        GenericArguments = genericArguments;
    }

    public FunctionDefinition FunctionDefinition {get; }
    public IEnumerable<LangPath> GenericArguments { get; }
}