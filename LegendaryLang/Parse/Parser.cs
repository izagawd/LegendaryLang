using System.Collections.Immutable;
using System.Text.RegularExpressions;
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



    public static string SpaceByCaps(string input)
    {
        return Regex.Replace(input, "(?<!^)([A-Z])", " $1");
    }

    public override string Message
    {
        get
        {
            // Join all expected representations.
            string expectedList = string.Join(", ", Expecteds.Select(e => e.ToString()));
            // Use the found token's representation if it exists.
            string foundRepresentation = _found != null ? _found.ToString() : "nothing";

            var tokenToUse = _found ?? Parser.LastToken;

            return $"Expected one of the following: [{expectedList}] but found {SpaceByCaps(_found?.GetType().Name)} '{foundRepresentation}'\n{tokenToUse.GetLocationStringRepresentation()}";
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
    public required List<ITopLevel> TopLevels { get; init; }

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
        var topLevels = new List<ITopLevel>();

        while (tokens.Count > 0)
        { 
            var gotten = Peek();
            if (gotten is FnToken)
            {
                topLevels.Add(FunctionDefinition.Parse(this));
            } else if (gotten is StructToken)
            {
                topLevels.Add(Struct.Parse(this));
            }
            else if (gotten is UseToken)
            {
                topLevels.Add(UseDefinition.Parse(this));
            }
            else
            {
                throw new ExpectedParserException(this, [ParseType.Struct,ParseType.Fn], gotten);
            }
        }

        return new ParseResult()
        {
            TopLevels = topLevels,
            File = File
        };
    }
    
}