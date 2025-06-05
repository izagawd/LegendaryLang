using System.Collections.Immutable;
using System.Text.RegularExpressions;
using LegendaryLang.Definitions;
using LegendaryLang.Definitions.Types;
using LegendaryLang.Lex.Tokens;
using File = LegendaryLang.Lex.File;

namespace LegendaryLang.Parse;

public class ParseException : Exception
{
    public ParseException()
    {
    }

    public ParseException(string message) : base(message)
    {
    }
}

public class ExpectedParserException : ParseException
{
    private readonly Token? _found;
    public ImmutableArray<ParseType> Expecteds;

    public ExpectedParserException(Parser parser, IEnumerable<ParseType> expected, Token? found)
    {
        Expecteds = expected.ToImmutableArray();
        _found = found;
        Parser = parser;
    }

    public ExpectedParserException(Parser parser, ParseType expected, Token? found) : this(parser, [expected], found)
    {
    }

    public Parser Parser { get; }

    public override string Message
    {
        get
        {
            // Join all expected representations.
            var expectedList = string.Join(", ", Expecteds.Select(e => e.ToString()));
            // Use the found token's representation if it exists.
            var foundRepresentation = _found != null ? _found.ToString() : "nothing";

            var tokenToUse = _found ?? Parser.LastToken;

            return
                $"Expected one of the following: [{expectedList}] but found {SpaceByCaps(_found?.GetType().Name)} '{foundRepresentation}'\n{tokenToUse.GetLocationStringRepresentation()}";
        }
    }


    public static string SpaceByCaps(string input)
    {
        return Regex.Replace(input, "(?<!^)([A-Z])", " $1");
    }
}

public class ParseResult
{
    public File? File { get; init; }
    public required List<IItem> Items { get; init; }
}

public class Parser
{
    private readonly Queue<Token> tokens = new();

    public Parser(File file)
    {
        File = file;
        tokens = new Queue<Token>(file.Tokens.ToArray());
    }

    public Token? LastToken => File.Tokens.ToArray().Last();
    public File File { get; }


    public Token? Peek()
    {
        if (tokens.Count > 0) return tokens.Peek();

        return null;
    }

    public Token? Pop()
    {
        if (tokens.Count > 0) return tokens.Dequeue();

        return null;
    }

    public ParseResult Parse()
    {
        var items = new List<IItem>();

        while (tokens.Count > 0)
        {
            items.Add(IItem.Parse(this, File.Module));
        }

        return new ParseResult
        {
            Items = items,
            File = File
        };
    }
}