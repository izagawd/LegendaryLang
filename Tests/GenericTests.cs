using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class GenericTests
{
    [Test]
    public void GenericNestedTests()
    {
        var compiled =Compiler.Compile("compiler_tests/generic_tests/generic_nested_test",false,false);
        if (compiled() != 7)
        {
            throw new Exception();
        }
    }
    [Test]
    public void GenericReturnTests()
    {
        var compiled =Compiler.Compile("compiler_tests/generic_tests/generic_return_test",false,false);
        if (compiled() != 5)
        {
            throw new Exception();
        }
    }

    [Test] public void GenericImportedAddBoundTest()
        => CompilerTestHelper.AssertSuccess("generic_tests/generic_imported_add_bound_test", 42);

    [Test] public void GenericImportedSubBoundTest()
        => CompilerTestHelper.AssertSuccess("generic_tests/generic_imported_sub_bound_test", 42);

    [Test] public void GenericImportedMulBoundTest()
        => CompilerTestHelper.AssertSuccess("generic_tests/generic_imported_mul_bound_test", 42);

    [Test] public void GenericImportedAddNoAssocTest()
        => CompilerTestHelper.AssertSuccess("generic_tests/generic_imported_add_no_assoc_test", 42);

    [Test] public void GenericImportedMultiBoundTest()
        => CompilerTestHelper.AssertSuccess("generic_tests/generic_imported_multi_bound_test", 42);

    [Test] public void GenericNameConflictFailTest()
        => CompilerTestHelper.AssertFail<GenericSemanticError>("generic_tests/generic_name_conflict_fail_test");

    [Test] public void DuplicateNameTraitStructFailTest()
        => CompilerTestHelper.AssertFail<DuplicateDefinitionError>("generic_tests/duplicate_name_trait_struct_fail_test");
}