using NUnit.Framework;
using static Tests.CompilerTestHelper;

namespace Tests;

public class RawPtrExprTests
{
    // ── Basic &raw → *shared T ──

    [Test]
    public void RawSharedBasicTest()
    {
        // &raw dd produces *shared i32, deref gives 42
        AssertSuccess("rawptr_tests/raw_shared_basic_test", 42);
    }

    // ── Basic &raw mut → *mut T ──

    [Test]
    public void RawMutBasicTest()
    {
        // &raw mut dd produces *mut i32, deref gives 42
        AssertSuccess("rawptr_tests/raw_mut_basic_test", 42);
    }

    // ── &raw *ref → convert reference to raw pointer ──

    [Test]
    public void RawFromRefSharedTest()
    {
        // &raw *r where r: &i32 → *shared i32 pointing to same memory
        AssertSuccess("rawptr_tests/raw_from_ref_shared_test", 42);
    }

    [Test]
    public void RawFromRefMutTest()
    {
        // &raw mut *r where r: &mut i32 → *mut i32 pointing to same memory
        AssertSuccess("rawptr_tests/raw_from_ref_mut_test", 42);
    }

    // ── Raw ptr and ref point to same memory ──

    [Test]
    public void RawSameAddressTest()
    {
        // AddrEq(&raw *ref, &raw var) verifies both point to same address
        AssertSuccess("rawptr_tests/raw_same_address_test", 1);
    }

    // ── Write through *mut raw pointer ──

    [Test]
    public void RawMutWriteTest()
    {
        // *raw = 42 writes through raw mut pointer, dd reads 42
        AssertSuccess("rawptr_tests/raw_mut_write_test", 42);
    }

    // ── Fat pointer: metadata preserved through &raw ──

    [Test]
    public void RawFatPtrTest()
    {
        // &raw *(&str) → *shared str preserves metadata (length=5 for "hello")
        // AddrEq verifies data pointer matches
        AssertSuccess("rawptr_tests/raw_fat_ptr_test", 1);
    }

    // ── Deref assign through raw mut pointer on struct ──

    [Test]
    public void RawMutDerefAssignTest()
    {
        // Write new Pair through *mut, read fields from original variable
        AssertSuccess("rawptr_tests/raw_mut_deref_assign_test", 42);
    }

    // ── Fat pointer roundtrip: &str → *shared str → &str preserves metadata ──

    [Test]
    public void RawFatPtrRoundtripTest()
    {
        // &str → &raw * → *shared str → &* → &str, GetMetadata still returns 5
        AssertSuccess("rawptr_tests/raw_fat_ptr_roundtrip_test", 1);
    }

    [Test]
    public void RawFatPtrRoundtripMutTest()
    {
        // Roundtrip preserves both metadata (length) and data pointer address
        AssertSuccess("rawptr_tests/raw_fat_ptr_roundtrip_mut_test", 1);
    }

    [Test]
    public void RawFatPtrMetadataPreservedTest()
    {
        // Different strings have different metadata through raw ptr roundtrip
        // "hi" → len 2, "hello" → len 5
        AssertSuccess("rawptr_tests/raw_fat_ptr_metadata_preserved_test", 1);
    }
}
