using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

[TestFixture]
public class TraitScopeTests
{
    [Test]
    public void InherentNoImportTest()
    {
        // Inherent methods always work — no trait import needed
        AssertSuccess("trait_scope_tests/inherent_no_import_test", 42);
    }

    [Test]
    public void TraitImportedWorksTest()
    {
        // Same-file trait is automatically in scope
        AssertSuccess("trait_scope_tests/trait_imported_works_test", 10);
    }

    [Test]
    public void TraitNotImportedFailTest()
    {
        // TryInto not imported — method call should fail
        AssertFail<GenericSemanticError>("trait_scope_tests/trait_not_imported_fail_test");
    }

    [Test]
    public void InherentPriorityOverTraitTest()
    {
        // Inherent .get() returns 100, trait .get() returns 200 — inherent wins
        AssertSuccess("trait_scope_tests/inherent_priority_over_trait_test", 100);
    }

    [Test]
    public void AmbiguousTraitMethodsFailTest()
    {
        // Two same-file traits both define .doit() — ambiguous, error
        AssertFail<GenericSemanticError>("trait_scope_tests/ambiguous_trait_methods_fail_test");
    }

    [Test]
    public void AmbiguousResolvedQualifiedTest()
    {
        // Qualified syntax disambiguates: (Foo as A).doit() + (Foo as B).doit() = 1 + 2 = 3
        AssertSuccess("trait_scope_tests/ambiguous_resolved_qualified_test", 3);
    }

    [Test]
    public void OneTraitInScopeNoAmbiguityTest()
    {
        // Only TryInto imported, no ambiguity
        AssertSuccess("trait_scope_tests/one_trait_in_scope_no_ambiguity_test", 1);
    }
}
