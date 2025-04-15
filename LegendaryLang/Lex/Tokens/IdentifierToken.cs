using LegendaryLang.Parse;

namespace LegendaryLang.Lex.Tokens;

public class IdentifierToken: Token
{
     public IdentifierToken(File file,int column, int line, string identity) : base(file,column, line)
     {
          Identity = identity;
     }

     public override string ToString()
     {
          return Identity;
     }

     public override bool Equals(object? obj)
     {
          if (obj is IdentifierToken identTok)
          {
               return Identity == identTok.Identity;
          }
          return false;
     }

     public static bool operator ==(IdentifierToken first, IdentifierToken second)
     {
          return first.Identity == second.Identity;
     }

     public static bool operator !=(IdentifierToken first, IdentifierToken second)
     {
          return !(first == second);
     }

     public override string Symbol => Identity;
     public string Identity { get; }

}