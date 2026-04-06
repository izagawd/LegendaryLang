namespace LegendaryLang.Lex;

public enum Operator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    ExclamationMark,
    GreaterThan,
    LessThan,
    Equals,
    NotEquals,
    And,
    Or,
}

public static class OperatorExtensions
{
    public static string ToSymbol(this Operator @operator)
    {
        return @operator switch
        {
            Operator.Add => "+",
            Operator.ExclamationMark => "!",
            Operator.Subtract => "-",
            Operator.Multiply => "*",
            Operator.Divide => "/",
            Operator.LessThan => "<",
            Operator.GreaterThan => ">",
            Operator.Equals => "==",
            Operator.NotEquals => "!=",
            Operator.And => "&&",
            Operator.Or => "||",
            _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null)
        };
    }

    public static int GetPrecedence(this Operator @operator)
    {
        return @operator switch
        {
            Operator.Or => 1,
            Operator.And => 2,
            Operator.Equals => 3,
            Operator.NotEquals => 3,
            Operator.LessThan => 4,
            Operator.GreaterThan => 4,
            Operator.Add => 5,
            Operator.Subtract => 5,
            Operator.Multiply => 6,
            Operator.Divide => 6,
            _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, "Unsupported operator.")
        };
    }
}