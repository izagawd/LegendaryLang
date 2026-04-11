using LegendaryLang;
using NUnit.Framework;

namespace Tests;

public class ArrayTests
{
    [Test] public void ArrayBasicTest() =>
        CompilerTestHelper.AssertSuccess("array_tests/array_basic_test", 42);

    [Test] public void ArrayTypeAnnotationTest() =>
        CompilerTestHelper.AssertSuccess("array_tests/array_type_annotation_test", 42);

    [Test] public void ArraySingleElementTest() =>
        CompilerTestHelper.AssertSuccess("array_tests/array_single_element_test", 42);

    [Test] public void ArrayOutOfBoundsTest() =>
        CompilerTestHelper.AssertSuccess("array_tests/array_out_of_bounds_test", 42);

    [Test] public void ArrayGetMutTest() =>
        CompilerTestHelper.AssertSuccess("array_tests/array_get_mut_test", 42);

    [Test] public void ArrayAllElementsTest() =>
        CompilerTestHelper.AssertSuccess("array_tests/array_all_elements_test", 15);

    [Test] public void ArrayBoolTest() =>
        CompilerTestHelper.AssertSuccess("array_tests/array_bool_test", 42);

    [Test] public void ArrayTypeMismatchFailTest() =>
        CompilerTestHelper.AssertFail<GenericSemanticError>("array_tests/array_type_mismatch_fail_test");

    [Test] public void ArrayEmptyFailTest() =>
        CompilerTestHelper.AssertFail<GenericSemanticError>("array_tests/array_empty_fail_test");
}
