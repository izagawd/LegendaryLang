using NUnit.Framework;
using LegendaryLang;
using static Tests.CompilerTestHelper;

namespace Tests;

[TestFixture]
public class LifetimeFieldTests
{
    [Test]
    public void RefFieldNoLifetimeFailTest()
    {
        // struct Foo { idk: &i32 } — reference field without lifetime parameter
        AssertFail<GenericSemanticError>("lifetime_field_tests/ref_field_no_lifetime_fail_test");
    }

    [Test]
    public void UnusedLifetimeParamFailTest()
    {
        // struct Foo['a] { val: i32 } — 'a declared but never used
        AssertFail<GenericSemanticError>("lifetime_field_tests/unused_lifetime_param_fail_test");
    }

    [Test]
    public void RefFieldWithLifetimeOkTest()
    {
        // struct Foo['a] { foo: &'a i32 } — valid
        AssertSuccess("lifetime_field_tests/ref_field_with_lifetime_ok_test", 42);
    }

    [Test]
    public void NestedStructDeclaredLifetimeOkTest()
    {
        // struct Foo['a] { dd: Bar['a] } — valid, lifetime propagated
        AssertSuccess("lifetime_field_tests/nested_struct_declared_lifetime_ok_test", 10);
    }

    [Test]
    public void TwoFieldsSameLifetimeOkTest()
    {
        // struct Foo['a] { dd: Bar['a], other: Bar['a] } — valid
        AssertSuccess("lifetime_field_tests/two_fields_same_lifetime_ok_test", 6);
    }

    [Test]
    public void TwoFieldsDifferentLifetimesOkTest()
    {
        // struct Foo['a, 'b] { dd: Bar['a], other: Bar['b] } — valid
        AssertSuccess("lifetime_field_tests/two_fields_different_lifetimes_ok_test", 7);
    }

    [Test]
    public void EnumRefVariantNoLifetimeFailTest()
    {
        // enum Holder { Some(&i32) } — reference variant without lifetime
        AssertFail<GenericSemanticError>("lifetime_field_tests/enum_ref_variant_no_lifetime_fail_test");
    }

    [Test]
    public void EnumRefVariantWithLifetimeOkTest()
    {
        // enum Holder['a] { Some(&'a i32), None } — valid
        AssertSuccess("lifetime_field_tests/enum_ref_variant_with_lifetime_ok_test", 99);
    }

    [Test]
    public void NestedStructMissingLifetimeFailTest()
    {
        // struct Foo { dd: Bar } where Bar['a] expects a lifetime — error
        AssertFail<GenericSemanticError>("lifetime_field_tests/nested_struct_missing_lifetime_fail_test");
    }

    [Test]
    public void NestedStructUndeclaredLifetimeFailTest()
    {
        // struct Foo { dd: Bar['a] } where 'a is not declared on Foo — error
        AssertFail<GenericSemanticError>("lifetime_field_tests/nested_struct_undeclared_lifetime_fail_test");
    }

    [Test]
    public void EnumNestedLifetimeStructOkTest()
    {
        // enum Maybe['a] { Some(Ref['a]) } — valid, lifetime propagated
        AssertSuccess("lifetime_field_tests/enum_nested_lifetime_struct_ok_test", 88);
    }

    [Test]
    public void EnumNestedMissingLifetimeFailTest()
    {
        // enum Maybe { Some(Ref['a]) } — 'a not declared on Maybe
        AssertFail<GenericSemanticError>("lifetime_field_tests/enum_nested_missing_lifetime_fail_test");
    }

    [Test]
    public void EnumUnusedLifetimeFailTest()
    {
        // enum Foo['a] { Val(i32) } — 'a never used
        AssertFail<GenericSemanticError>("lifetime_field_tests/enum_unused_lifetime_fail_test");
    }

    [Test]
    public void EnumTwoVariantsDifferentLifetimesOkTest()
    {
        // enum Either['a, 'b] { Left(&'a i32), Right(&'b i32) } — valid
        AssertSuccess("lifetime_field_tests/enum_two_variants_different_lifetimes_ok_test", 3);
    }

    [Test]
    public void LifetimeInParensFailTest()
    {
        // Inner('b) uses () instead of [] for lifetime args — lifetime is silently erased, 'b reported unused
        AssertFail<GenericSemanticError>("lifetime_field_tests/lifetime_in_parens_fail_test");
    }
}
