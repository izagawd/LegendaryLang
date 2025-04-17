using LegendaryLang.Parse;
using NUnit.Framework;

namespace Tests;

public class LangPathTests
{
    [Test]
    public void TestBasicComparison()
    {
        
        Assert.That(      new NormalLangPath(null, ["hello", "world"]) 
                            == new NormalLangPath(null,["hello", "world"]),"");
        Assert.That(      new NormalLangPath(null, ["hello", "world"]) 
                          != new NormalLangPath(null,["hello"]),"");
        Assert.That(      new NormalLangPath(null, ["hello", "earth"]) 
                          != new NormalLangPath(null,["hello", "world"]),"");
    }
    [Test]
    public void TestComparisonWithEmptyGenerics()
    {
        Assert.That(      new NormalLangPath(null, ["hello", "world"]) 
                            == new NormalLangPath(null,["hello",
                                new NormalLangPath.GenericTypesPathSegment([])
                                , "world"]),"");
        Assert.That(      new NormalLangPath(null, ["hello", "world"]) 
                            != new NormalLangPath(null,["hello",
                                new NormalLangPath.GenericTypesPathSegment([])
                                , "earth"]),"");
    }


}