using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;


public class EmptyPathException(LangPath paths) : ParseException
{
    public LangPath Path => paths;

    public override string Message => $"Expected at least one string in the path. None were found";
}




public class TupleLangPath : LangPath
{
    public override string ToString()
    {
        return $"({string.Join(",", TypePaths)})";
    }

    public ImmutableArray<LangPath> TypePaths { get; }
    public override LangPath Monomorphize(CodeGenContext codeGen)
    {
        return new TupleLangPath(TypePaths.Select(i => i.Monomorphize(codeGen)));
    }


    public override bool Equals(object? obj)
    {
        if (obj is TupleLangPath tupleLangPath)
        {
            return TypePaths.SequenceEqual(tupleLangPath.TypePaths);
        }
        return false;
    }

    public TupleLangPath(IEnumerable<LangPath> paths, IdentifierToken? firstIdentifierToken = null)
    {
        FirstIdentifierToken = firstIdentifierToken;
        TypePaths = paths.ToImmutableArray();
    }



    public override LangPath GetAsShortCutIfPossible(SemanticAnalyzer analyzer)
    {
        return new TupleLangPath(TypePaths.Select(i => i.GetAsShortCutIfPossible(analyzer)));
     
    }
}


public abstract class LangPath
{
    public static implicit operator LangPath(ImmutableArray<NormalLangPath.PathSegment> segments)
    {
        return new NormalLangPath(null,segments);
    }
    /// <summary>
    /// MAKES THE COMPILER understand that the i32 is actually 'std::primitive::i32' if
    /// use std::primitive::i32;
    /// is declared. provide it with i32, and its should return std::primitive::i32
    /// </summary>
    public abstract LangPath GetAsShortCutIfPossible(SemanticAnalyzer analyzer);
    public static NormalLangPath PrimitivePath = new NormalLangPath(null,["std", "primitive"]);
    public static TupleLangPath VoidBaseLangPath { get; } = new TupleLangPath([]);
    public static bool operator ==(LangPath? path1, LangPath? path2)
    {
        if (path1 is null && path2 is null)
        {
            return true;
        }

        if (path1 is null)
        {
            return false;
        }
        return path1.Equals(path2);
    }

    public static bool operator !=(LangPath? path1, LangPath? path2)
    {
        return !(path1 == path2);
    }

    public IdentifierToken? FirstIdentifierToken { get; init; }
    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }

    public static  LangPath  Parse(Parser parser)
    {
        var next = parser.Peek();
        if (next is LeftParenthesisToken)
        {
            Parenthesis.ParseLeft(parser);
            
            List<LangPath> tuplePaths = new List<LangPath>();

            while (parser.Peek() is not RightParenthesisToken)
            {
                tuplePaths.Add(Parse(parser));
                if (parser.Peek() is CommaToken)
                {
                    parser.Pop();
                }
                else
                {
                    break;
                }
            }
            Parenthesis.ParseRight(parser);
            return new TupleLangPath(tuplePaths);
        }
        var firstIdent = Identifier.Parse(parser);
        var segments = new List<NormalLangPath.PathSegment>() { firstIdent.Identity };
        ;
        while (parser.Peek() is DoubleColonToken)
        {
            parser.Pop();
            if (parser.Peek() is LessThanToken)
            {
                parser.Pop();
                var arguments = new List<LangPath>();
                while (parser.Peek() is not GreaterThanToken)
                {
                    arguments.Add(LangPath.Parse(parser));
                    if (parser.Peek() is CommaToken)
                    {
                        parser.Pop();
                        
                    }
                    else
                    {
                        break;
                    }
                }
                Comparator.ParseGreater(parser);
                var genericsSegment = new NormalLangPath.GenericTypesPathSegment(arguments);
                segments.Add(genericsSegment);
            }
            else
            {
                segments.Add(Identifier.Parse(parser).Identity);
            }
          
        }

        return new NormalLangPath( firstIdent ,segments.Select(i => i))
        {
            FirstIdentifierToken = firstIdent
        };
    }
    public  override abstract bool Equals(object? obj);
    public  override abstract string ToString();

    public abstract LangPath Monomorphize(CodeGenContext codeGen);
}