﻿namespace LegendaryLang.Lex.Tokens;

public class ExclamationMarkToken : Token, IOperatorToken
{
    public ExclamationMarkToken(File file, int column, int line) : base(file, column, line)
    {
    }

    public override string Symbol => "!";
    public Operator Operator => Operator.Negate;
}