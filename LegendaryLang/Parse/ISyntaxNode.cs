﻿using LegendaryLang.Lex.Tokens;
using LegendaryLang.Semantics;

namespace LegendaryLang.Parse;


public interface ICanHaveExplicitReturn : ISyntaxNode
{
    public bool HasGuaranteedExplicitReturn { get; }
}
public interface ISyntaxNode
{
    
    public IEnumerable<ISyntaxNode> Children { get; }
    public bool NeedsSemiColonAfterIfNotLastInBlock => true;


    /// <summary>
    ///     Token used to locate where the syntax node is written
    /// </summary>
    public Token Token { get; }
}