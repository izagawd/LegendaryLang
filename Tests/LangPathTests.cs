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
        Assert.That(new NormalLangPath(null, ["hello", "world"])
                    != new NormalLangPath(null, [
                        "hello",
                        new NormalLangPath.GenericTypesPathSegment([
                            new NormalLangPath(null,
                                ["dd"])
                        ]),
                        "world"
                    ]));
        Assert.That(new NormalLangPath(null, [
                        "hello",
                        new NormalLangPath.GenericTypesPathSegment(
                        [
                            new NormalLangPath(null, ["d"])
                        ]),
                        "world"
                    ])
                    == new NormalLangPath(null, [
                        "hello",
                        new NormalLangPath.GenericTypesPathSegment(
                        [
                            new NormalLangPath(null,
                                ["d"])
                        ]),
                        "world"
                    ]));
    }


    [Test]
    public void TestComparisonWithEmptyGenerics()
    {
        Assert.That(new NormalLangPath(null, ["hello", "world"])
                    == new NormalLangPath(null, [
                        "hello",
                        new NormalLangPath.GenericTypesPathSegment([]), "world"
                    ]), "");
        Assert.That(new NormalLangPath(null, ["hello", "world"])
                    != new NormalLangPath(null, [
                        "hello",
                        new NormalLangPath.GenericTypesPathSegment([]), "earth"
                    ]), "");
    }
}