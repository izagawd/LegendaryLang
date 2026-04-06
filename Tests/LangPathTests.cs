using System.Collections.Immutable;
using LegendaryLang.Parse;
using NUnit.Framework;

// ReSharper disable EqualExpressionComparison

namespace Tests;

public class LangPathTests
{
    [Test]
    public void TestBasicComparison()
    {
        Assert.That(new NormalLangPath(null, ["hello", "world"])
                    == new NormalLangPath(null, ["hello", "world"]), "");
        Assert.That(new NormalLangPath(null, ["hello", "world"])
                    != new NormalLangPath(null, ["hello"]), "");
        Assert.That(new NormalLangPath(null, ["hello", "earth"])
                    != new NormalLangPath(null, ["hello", "world"]), "");
    }

    [Test]
    public void TestContains()
    {
        Assert.That(new NormalLangPath(null, ["hello", "world"])
            .Contains(
                new NormalLangPath(null, ["hello"])), "");
        Assert.That(new NormalLangPath(null, ["hello", "world"])
            .Contains(
                new NormalLangPath(null, ["hello", "world"])), "");
        Assert.That(!new NormalLangPath(null, ["hello", "world"])
            .Contains(
                new NormalLangPath(null, ["hello", "wold"])), "");
        Assert.That(!new NormalLangPath(null, ["hello"])
            .Contains(
                new NormalLangPath(null, ["hello", "wold"])), "");
    }

    [Test]
    public void TestComparisonWithGenerics()
    {
        // hello.world != hello(dd).world
        Assert.That(new NormalLangPath(null, ["hello", "world"])
                    != new NormalLangPath(null, [
                        new NormalLangPath.NormalPathSegment("hello", [
                            new NormalLangPath(null, ["dd"])
                        ]),
                        "world"
                    ]));
        // hello(d).world == hello(d).world
        Assert.That(new NormalLangPath(null, [
                        new NormalLangPath.NormalPathSegment("hello", [
                            new NormalLangPath(null, ["d"])
                        ]),
                        "world"
                    ])
                    == new NormalLangPath(null, [
                        new NormalLangPath.NormalPathSegment("hello", [
                            new NormalLangPath(null, ["d"])
                        ]),
                        "world"
                    ]));
    }


    [Test]
    public void TestComparisonWithEmptyGenerics()
    {
        // hello.world == hello(no generics).world
        Assert.That(new NormalLangPath(null, ["hello", "world"])
                    == new NormalLangPath(null, [
                        new NormalLangPath.NormalPathSegment("hello", ImmutableArray<LangPath>.Empty),
                        "world"
                    ]), "");
        // hello.world != hello.earth (regardless of empty generics)
        Assert.That(new NormalLangPath(null, ["hello", "world"])
                    != new NormalLangPath(null, [
                        new NormalLangPath.NormalPathSegment("hello", ImmutableArray<LangPath>.Empty),
                        "earth"
                    ]), "");
    }
}
