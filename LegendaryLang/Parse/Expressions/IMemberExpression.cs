namespace LegendaryLang.Parse.Expressions;

public interface IMemberExpression : IExpression
{
    public IMemberExpression? NextMemberAccessExpression { get; }
}