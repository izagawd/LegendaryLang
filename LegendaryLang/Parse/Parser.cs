using System.Collections.Immutable;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse.Types;
using File = LegendaryLang.Lex.File;

namespace LegendaryLang.Parse;


public class ParseException : Exception;

public  class ExpectedParserException : ParseException
{
    public ImmutableArray<ParseType> Expecteds;
    private readonly Token? _found;
    public Parser Parser {get; }




    public override string Message
    {
        get
        {
            // Join all expected representations.
            string expectedList = string.Join(", ", Expecteds.Select(e => e.ToString()));
            // Use the found token's representation if it exists.
            string foundRepresentation = _found != null ? _found.ToString() : "nothing";

            var line = _found?.Line ?? Parser.LastToken?.Line ?? 0;
            // Assuming the Token class provides access to file and line information.
            string foundLine = Parser.File.GetLine(line) ;

            return $"Expected one of the following: {expectedList} but found {foundRepresentation}\nat: '{foundLine}'\nline {line}";
        }
    }
    public ExpectedParserException(Parser parser,IEnumerable<ParseType> expected, Token? found)
    {
        Expecteds = expected.ToImmutableArray();
        _found = found;
        Parser = parser;
    }
    public ExpectedParserException(Parser parser,ParseType expected, Token? found) : this(parser,[expected], found)
    {
      
    }
}

public class ParseResult
{
    public File? File { get; init; }
    public required List<IDefinition> Definitions { get; init; }

}
public class Parser
{
    public Token? LastToken => File.Tokens.ToArray().Last();
    private Queue<Token> tokens = new Queue<Token>();
    public File File { get; }

    public Parser(File file)
    {
        File = file;
        tokens = new Queue<Token>(file.Tokens.ToArray());
    }


    public Token? Peek()
    {
        if (tokens.Count > 0)
        {
            return tokens.Peek();
        }
        else
        {
            return null;
        }
    }

    public Token? Pop()
    {
        if (tokens.Count > 0)
        {
            return tokens.Dequeue();
        }
        else
        {
            return null;
        }
    }
    
    public ParseResult Parse()
    {
        var definitions = new List<IDefinition>();

        while (tokens.Count > 0)
        { 
            var gotten = Peek();
            if (gotten is FnToken)
            {
                definitions.Add(Function.Parse(this));
            } else if (gotten is StructToken)
            {
                definitions.Add(Struct.Parse(this));
            }
            else
            {
                throw new ExpectedParserException(this, [ParseType.Struct,ParseType.Fn], gotten);
            }
        }

        return new ParseResult()
        {
            Definitions = definitions,
        };
    }
    
}