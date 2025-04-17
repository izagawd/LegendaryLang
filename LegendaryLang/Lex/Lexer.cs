using System.Text;
using LegendaryLang.Lex.Tokens;

namespace LegendaryLang.Lex
{
    public class UnrecognizedCharacterException : System.Exception
    {
        public UnrecognizedCharacterException(char c) : base($"Character {c} not recognized as a token") {}
        
    }
    public static class Lexer
    {
        public static File Lex(string code, string filePath)
        {
            // Create a new File instance that will hold the tokens.
            var file = new File(filePath);
            int index = 0;
            int column = 0;
            int line = 1;
            int last_line_break_pos = 0;
            while (index < code.Length)
            {
                char current = code[index];



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
                        if (index + 1 < code.Length && code[index + 1] == '=')
                        {
                            file.AddToken(new DoubleEquality(file, column, line));
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
                    case '-':
                        if (index + 1 < code.Length && code[index + 1] == '>')
                        {
                            file.AddToken(new RightPointToken(file, column, line));
                            index++;
                        }
                        else
                        {
                            file.AddToken(new Minus(file, column, line));
                        }
                        break;
                    case '+':
                        file.AddToken(new Plus(file, column, line));
                        break;
                    case '/':
                        file.AddToken(new ForwardSlash(file, column, line));
                        break;
                    case '*':
                        file.AddToken(new Star(file, column, line));
                        break;
                    case '<':
                        file.AddToken(new LessThanToken(file, column, line));
                        break;
                    case '>':
                        file.AddToken(new GreaterThanToken(file, column, line));
                        break;
                    case '!':
                        file.AddToken(new ExclamationMarkToken(file, column, line));
                        break;
                    case ':':
                        if (index + 1 < code.Length && code[index + 1] == ':')
                        {
                            file.AddToken(new DoubleColonToken(file, column, line));
                            index++;
                        }
                        else
                        {
                            file.AddToken(new ColonToken(file, column, line));
                        }
                        break;
                    case  '\r':
                        break;
                    case  '\n':
                        line += 1;
                        file.AddCode(code.Substring((int)last_line_break_pos, (int)column));
                        column = 0;

                        last_line_break_pos = index;
                        break;
                    
                    case var b when (b >= '0' && b <= '9'):
                        
                        // Record the starting column of the number.
                        int startColumn = column;
                        // Use a StringBuilder to accumulate the digits (and possibly a decimal point).
                        StringBuilder numberBuilder = new StringBuilder();
                        numberBuilder.Append(current);

                        // Flag to ensure that only one decimal point is included (if supporting floating-point numbers).
                        bool hasDecimalPoint = false;

                        // Loop to accumulate all subsequent digits and an optional single decimal point.
                        while (index + 1 < code.Length)
                        {
                            char nextChar = code[index + 1];

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
                    case var c when (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'):
                        startColumn = column;
                        StringBuilder identifierBuilder = new StringBuilder();
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

                        string ident = identifierBuilder.ToString();
                        // Use a switch expression (or a traditional switch) to decide which token to create.
                        switch (ident)
                        {
                            case "let":
                                file.AddToken(new LetToken(file, column, line));
                                break;
                            case "if":
                                file.AddToken(new If(file, column, line));
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
                            case "struct":
                                file.AddToken(new StructToken(file, column, line));
                                break;
                            case "use":
                                file.AddToken(new UseToken(file, column, line));
                                break;
                            case "fn":
                                file.AddToken(new FnToken(file, column, line));
                                break;
                            default:
                                file.AddToken(new IdentifierToken(file, column, line, ident));
                                break;
                        }
                                   break;
                        
                    default:
                        throw new UnrecognizedCharacterException(current);
                        break;
                }
              
                // Move to the next character after processing a token.
                index++;
                column = index - last_line_break_pos;
            }
            file.AddCode(code.Substring((int)last_line_break_pos, (int)column));
            return file;
        }
    }
}