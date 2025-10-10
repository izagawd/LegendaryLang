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
}

public static class OperatorExtensions
{
    public static string ToSymbol(this Operator @operator)
    {
        switch (@operator)
        {
            case Operator.Add:
                return "+";
            case Operator.ExclamationMark:
                return "-";
            case Operator.Subtract:
                return "-";
            case Operator.Multiply:
                return "*";
            case Operator.Divide:
                return "/";
            case Operator.LessThan:
                return "<";
            case Operator.GreaterThan:
                return ">";
            default:
                throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null);
        }
    }

    public static int GetPrecedence(this Operator @operator)
    {
        switch (@operator)
        {
            case Operator.Add:
            case Operator.Subtract:
                return 1; // Lower precedence.
            case Operator.Multiply:
                return 2;
            case Operator.Divide:
                return 3; // Higher precedence.
            case Operator.GreaterThan:
            case Operator.LessThan:
                return 4;
            default:
                throw new ArgumentOutOfRangeException(nameof(@operator), @operator, "Unsupported operator.");
        }
    }
}