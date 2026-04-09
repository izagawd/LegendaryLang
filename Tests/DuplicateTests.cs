using LegendaryLang;
using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class DuplicateTests
{
    [Test] public void DuplicateFnTest() => AssertFail<DuplicateDefinitionError>("duplicate_tests/duplicate_fn_test");

    [Test] public void DuplicateStructTest() => AssertFail<DuplicateDefinitionError>("duplicate_tests/duplicate_struct_test");

    [Test] public void DuplicateNestedFnTest() => AssertFail<DuplicateDefinitionError>("duplicate_tests/duplicate_nested_fn_test");

    [Test] public void DuplicateNestedStructTest() => AssertFail<DuplicateDefinitionError>("duplicate_tests/duplicate_nested_struct_test");

    [Test] public void DuplicateNestedTraitTest() => AssertFail<DuplicateDefinitionError>("duplicate_tests/duplicate_nested_trait_test");
}
