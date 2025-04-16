using System.Runtime.InteropServices;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Parse;

namespace LegendaryLang.Lex;

public class File
{
    public NormalLangPath Module => new NormalLangPath(null, Path.Split("\\").SkipLast(1).Select(i => (PathSegment)i.Replace($".{Compiler.extension}","")));
    public string Path { get; }

    public File(string path)
    {
        Path = path;
    }

    private readonly List<Token> _tokens = [];
    
    
    public Span<Token> Tokens => CollectionsMarshal.AsSpan(_tokens);


    public string GetLine(int line)
    {

        if (line == 0)
        {
            return "";
        }
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
    private readonly List<string> _code = [];
    public Span<string> Code  => CollectionsMarshal.AsSpan(_code);
}