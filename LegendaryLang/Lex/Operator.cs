namespace LegendaryLang.Lex;

public enum Operator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Negate,
    
}

public static class OperatorExtensions
{
    public static string ToSymbol(this Operator @operator)
    {
        switch (@operator)
        {
            case Operator.Add:
                return "+";
                break;
            case Operator.Subtract:
                return "-";
                break;
            case Operator.Multiply:
                return "*";
                break;
            case Operator.Divide:
                return "/";
                break;
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
            default:
                throw new ArgumentOutOfRangeException(nameof(@operator), @operator, "Unsupported operator.");
        }
    }
}