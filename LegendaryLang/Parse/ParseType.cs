namespace LegendaryLang.Parse;

public enum ParseType
{
    Identifier, Expression, LeftParenthesis, RightParenthesis, Fn, Comma, SemiColon, Number,
    DoubleColon,
    Colon,
    LeftCurlyBrace,
    RightCurlyBrace,
    BaseLangPath,
    Let,
    Equality,
    Struct,
    Bool,
    Operator,
    Dot,
    StructPath,
    FunctionCall,
    Use,
    LessThan,
    GreaterThan,
    ReturnToken
}
