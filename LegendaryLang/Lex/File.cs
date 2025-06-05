using System.Runtime.InteropServices;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;

namespace LegendaryLang.Lex;

public class File
{
    private readonly List<string> _code = [];

    private readonly List<Token> _tokens = [];

    public File(string path)
    {
        Path = path;
    }

    public NormalLangPath Module => new(null,
        Path.Split("\\").Select(i => (NormalLangPath.PathSegment)i.Replace($".{Compiler.extension}", "")));

    public string Path { get; }


    public Span<Token> Tokens => CollectionsMarshal.AsSpan(_tokens);
    public Span<string> Code => CollectionsMarshal.AsSpan(_code);


    public string GetLine(int line)
    {
        if (line == 0) return "";
        return _code[line - 1];
    }

    public void AddCode(string code)
    {
        _code.Add(code);
    }

    public void AddToken(Token token)
    {
        _tokens.Add(token);
    }
}