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

    [Test]
    public void StructNestedUnsizedPropagatesFailTest()
    {
        // Inner(T) has val: T, Outer has inner: Inner(T) — both unsized when T:! MetaSized
        AssertFail<GenericSemanticError>("sized_tests/struct_nested_unsized_propagates_fail_test");
    }

    [Test]
    public void StructNestedSizedOkTest()
    {
        // All concrete sized fields, nested structs — works
        AssertSuccess("sized_tests/struct_nested_sized_ok_test", 42);
    }

    [Test]
    public void StructTripleNestedUnsizedFailTest()
    {
        // A(T) → B(T) → C(T), all with val: T, T:! MetaSized — unsized propagates 3 levels
        AssertFail<GenericSemanticError>("sized_tests/struct_triple_nested_unsized_fail_test");
    }

    [Test]
    public void StructNestedRefSizedOkTest()
    {
        // Inner has ptr: &T (sized), Outer has inner: Inner(T) — all sized even with T:! MetaSized
        AssertSuccess("sized_tests/struct_nested_ref_sized_ok_test", 0);
    }

    [Test]
    public void EnumUnsizedVariantFailTest()
    {
        // Enum variant Some(T) where T:! MetaSized — enum is unsized
        AssertFail<GenericSemanticError>("sized_tests/enum_unsized_variant_fail_test");
    }

    [Test]
    public void EnumSizedVariantOkTest()
    {
        // Enum variant Some(T) where T:! type (implicit Sized) — works
        AssertSuccess("sized_tests/enum_sized_variant_ok_test", 0);
    }

    [Test]
    public void FieldAssocTypeSizedOkTest()
    {
        // Field is (T as Add(T)).Output — Output :! type → implicitly Sized
        AssertSuccess("sized_tests/field_assoc_type_sized_ok_test", 3);
    }

    [Test]
    public void FieldAssocMetaSizedFailTest()
    {
        // Field is (T as Producer).Item — Item :! MetaSized → unsized, struct unsized
        AssertFail<GenericSemanticError>("sized_tests/field_assoc_metasized_fail_test");
    }

    [Test]
    public void FieldAssocRefOkTest()
    {
        // Field is &(T as Producer).Item — reference to unsized assoc type, still sized
        AssertSuccess("sized_tests/field_assoc_ref_ok_test", 0);
    }

    [Test]
    public void FieldAssocSizedBoundOkTest()
    {
        // Field is (T as Maker).Output — Output :! Sized → explicitly Sized
        AssertSuccess("sized_tests/field_assoc_sized_bound_ok_test", 0);
    }

    [Test]
    public void FieldAssocNestedSizedOkTest()
    {
        // Nested struct with assoc type field — sized propagates through
        AssertSuccess("sized_tests/field_assoc_nested_sized_ok_test", 3);
    }
}
