using System.Text;
using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Lex;

public class UnrecognizedCharacterException : Exception
{
    public UnrecognizedCharacterException(char c) : base($"Character {c} not recognized as a token")
    {
    }
}

public static class Lexer
{
    public static File Lex(string code, string filePath)
    {
        // Create a new File instance that will hold the tokens.
        var file = new File(filePath);
        var index = 0;
        var column = 0;
        var line = 1;
        var last_line_break_pos = 0;
        while (index < code.Length)
        {
            var current = code[index];


            // Check for single-character tokens.
            switch (current)
            {
                case ';':
                    file.AddToken(new SemiColonToken(file, column, line));
                    break;
                case '.':
                    file.AddToken(new DotToken(file, column, line));
                    break;

                case ' ':
                    break;
                case '=':
                    if (index + 1 < code.Length && code[index + 1] == '>')
                    {
                        file.AddToken(new FatArrowToken(file, column, line));
                        index++;
                    }
                    else if (index + 1 < code.Length && code[index + 1] == '=')
                    {
                        file.AddToken(new OperatorToken(file, column, line, Operator.Equals));
                        index++;
                    }
                    else
                    {
                        file.AddToken(new EqualityToken(file, column, line));
                    }

                    break;
                case ',':
                    file.AddToken(new CommaToken(file, column, line));
                    break;
                case '{':
                    file.AddToken(new LeftCurlyBraceToken(file, column, line));
                    break;
                case '}':
                    file.AddToken(new RightCurlyBraceToken(file, column, line));
                    break;
                case '(':
                    file.AddToken(new LeftParenthesisToken(file, column, line));
                    break;
                case ')':
                    file.AddToken(new RightParenthesisToken(file, column, line));
                    break;
                case '[':
                    file.AddToken(new LeftBracketToken(file, column, line));
                    break;
                case ']':
                    file.AddToken(new RightBracketToken(file, column, line));
                    break;
                case '-':
                    if (index + 1 < code.Length && code[index + 1] == '>')
                    {
                        file.AddToken(new RightPointToken(file, column, line));
                        index++;
                    }
                    else
                    {
                        file.AddToken(new OperatorToken(file,column,line,Operator.Subtract));
                    }

                    break;
                case '+':
                    file.AddToken(new OperatorToken(file,column,line,Operator.Add));
                    break;
                case '/':
                    file.AddToken(new OperatorToken(file,column,line,Operator.Divide));
                    break;
                case '*':
                    file.AddToken(new OperatorToken(file,column,line,Operator.Multiply));
                    break;
                case '<':
                    file.AddToken(new OperatorToken(file,column,line,Operator.LessThan));
                    break;
                case '>':
                    file.AddToken(new OperatorToken(file,column,line,Operator.GreaterThan));
                    break;
                case '!':
                    if (index + 1 < code.Length && code[index + 1] == '=')
                    {
                        file.AddToken(new OperatorToken(file, column, line, Operator.NotEquals));
                        index++;
                    }
                    else
                    {
                        file.AddToken(new OperatorToken(file,column,line,Operator.ExclamationMark));
                    }
                    break;
                case ':':
                    if (index + 1 < code.Length && code[index + 1] == ':')
                    {
                        file.AddToken(new DoubleColonToken(file, column, line));
                        index++;
                    }
                    else if (index + 1 < code.Length && code[index + 1] == '!')
                    {
                        file.AddToken(new ColonBangToken(file, column, line));
                        index++;
                    }
                    else
                    {
                        file.AddToken(new ColonToken(file, column, line));
                    }

                    break;
                case '&':
                    if (index + 1 < code.Length && code[index + 1] == '&')
                    {
                        file.AddToken(new OperatorToken(file, column, line, Operator.And));
                        index++;
                    }
                    else
                    {
                        file.AddToken(new AmpersandToken(file, column, line));
                    }
                    break;
                case '|':
                    if (index + 1 < code.Length && code[index + 1] == '|')
                    {
                        file.AddToken(new OperatorToken(file, column, line, Operator.Or));
                        index++;
                    }
                    break;
                case '\r':
                    break;
                case '\n':
                    line += 1;
                    file.AddCode(code.Substring(last_line_break_pos, column));
                    column = 0;

                    last_line_break_pos = index;
                    break;

                case var b when b >= '0' && b <= '9':

                    // Record the starting column of the number.
                    var startColumn = column;
                    // Use a StringBuilder to accumulate the digits (and possibly a decimal point).
                    var numberBuilder = new StringBuilder();
                    numberBuilder.Append(current);

                    // Flag to ensure that only one decimal point is included (if supporting floating-point numbers).
                    var hasDecimalPoint = false;

                    // Loop to accumulate all subsequent digits and an optional single decimal point.
                    while (index + 1 < code.Length)
                    {
                        var nextChar = code[index + 1];

                        if (char.IsDigit(nextChar))
                        {
                            index++;
                            column++;
                            numberBuilder.Append(nextChar);
                        }
                        else if (nextChar == '.' && !hasDecimalPoint)
                        {
                            // Accept a single decimal point.
                            hasDecimalPoint = true;
                            index++;
                            column++;
                            numberBuilder.Append(nextChar);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Create a NumberToken from the accumulated characters.
                    // Adjust the constructor parameters as needed for your implementation.
                    file.AddToken(new NumberToken(file, startColumn, line, numberBuilder.ToString()));
                    break;
                case var c when (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_':
                    startColumn = column;
                    var identifierBuilder = new StringBuilder();
                    identifierBuilder.Append(current);

                    // Continue accumulating valid identifier characters:
                    // letters, digits, hyphen, or underscore.
                    while (index + 1 < code.Length &&
                           (char.IsLetterOrDigit(code[index + 1]) ||
                            code[index + 1] == '-' ||
                            code[index + 1] == '_'))
                    {
                        index++;
                        column++;
                        identifierBuilder.Append(code[index]);
                    }

                    var ident = identifierBuilder.ToString();
                    // Use a switch expression (or a traditional switch) to decide which token to create.
                    switch (ident)
                    {
                        case "let":
                            file.AddToken(new LetToken(file, column, line));
                            break;
                        case "if":
                            file.AddToken(new IfToken(file, column, line));
                            break;
                        case "else":
                            file.AddToken(new Else(file, column, line));
                            break;
                        case "false":
                            file.AddToken(new False(file, column, line));
                            break;
                        case "true":
                            file.AddToken(new True(file, column, line));
                            break;
                        case "mut":
                            file.AddToken(new MutToken(file, column, line));
                            break;
                        case "while":
                            file.AddToken(new WhileToken(file, column, line));
                            break;
                        case "struct":
                            file.AddToken(new StructToken(file, column, line));
                            break;
                        case "return":
                            file.AddToken(new ReturnToken(file, column, line));
                            break;
                        case "use":
                            file.AddToken(new UseToken(file, column, line));
                            break;
                        case "fn":
                            file.AddToken(new FnToken(file, column, line));
                            break;
                        case "trait":
                            file.AddToken(new TraitToken(file, column, line));
                            break;
                        case "impl":
                            file.AddToken(new ImplToken(file, column, line));
                            break;
                        case "for":
                            file.AddToken(new ForToken(file, column, line));
                            break;
                        case "as":
                            file.AddToken(new AsToken(file, column, line));
                            break;
                        case "type":
                            file.AddToken(new TypeKeywordToken(file, column, line));
                            break;
                        case "enum":
                            file.AddToken(new EnumToken(file, column, line));
                            break;
                        case "match":
                            file.AddToken(new MatchToken(file, column, line));
                            break;
                        default:
                            file.AddToken(new IdentifierToken(file, column, line, ident));
                            break;
                    }

                    break;

                default:
                    // Lifetime: 'a, 'b, 'static, etc.
                    if (current == '\'')
                    {
                        if (index + 1 < code.Length && char.IsLetter(code[index + 1]))
                        {
                            startColumn = column;
                            var lifetimeBuilder = new StringBuilder();
                            index++;
                            column++;
                            while (index < code.Length && (char.IsLetterOrDigit(code[index]) || code[index] == '_'))
                            {
                                lifetimeBuilder.Append(code[index]);
                                index++;
                                column++;
                            }
                            file.AddToken(new LifetimeToken(file, startColumn, line, lifetimeBuilder.ToString()));
                            continue;
                        }
                    }
                    throw new UnrecognizedCharacterException(current);
                    break;
            }

            // Move to the next character after processing a token.
            index++;
            column = index - last_line_break_pos;
        }

        file.AddCode(code.Substring(last_line_break_pos, column));
        return file;
    }
}