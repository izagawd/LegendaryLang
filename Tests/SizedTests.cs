using NUnit.Framework;
using LegendaryLang;
using static Tests.CompilerTestHelper;

namespace Tests;

[TestFixture]
public class SizedFieldTests
{
    [Test]
    public void StructSizedFieldOkTest()
    {
        // Struct with concrete sized fields — works normally
        AssertSuccess("sized_tests/struct_sized_field_ok_test", 3);
    }

    [Test]
    public void StructGenericTypeSizedOkTest()
    {
        // T:! type → implicit Sized, struct field val: T is sized
        AssertSuccess("sized_tests/struct_generic_type_sized_ok_test", 42);
    }

    [Test]
    public void StructGenericMetaSizedDirectFailTest()
    {
        // T:! MetaSized, field val: T — Wrapper(T) is unsized, can't pass by value
        AssertFail<GenericSemanticError>("sized_tests/struct_generic_metasized_direct_fail_test");
    }

    [Test]
    public void StructGenericMetaSizedRefOkTest()
    {
        // T:! MetaSized, field ptr: &T — RefHolder is always sized (ref is sized)
        AssertSuccess("sized_tests/struct_generic_metasized_ref_ok_test", 99);
    }

    [Test]
    public void StructTypeRefFieldOkTest()
    {
        // T:! type, field inner: &T — always sized
        AssertSuccess("sized_tests/struct_type_ref_field_ok_test", 77);
    }

    [Test]
    public void StructMetaSizedRawPtrOkTest()
    {
        // T:! MetaSized, field ptr: *shared T — struct definition compiles
        AssertSuccess("sized_tests/struct_metasized_rawptr_ok_test", 55);
    }

    [Test]
    public void StructGenericSizedBoundOkTest()
    {
        // T:! Sized — explicit Sized, struct works normally
        AssertSuccess("sized_tests/struct_generic_sized_bound_ok_test", 10);
    }

    [Test]
    public void StructSizedMetaSizedOkTest()
    {
        // T:! Sized + MetaSized — Sized wins, struct works normally
        AssertSuccess("sized_tests/struct_sized_metasized_ok_test", 33);
    }
}
