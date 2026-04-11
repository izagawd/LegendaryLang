fn read_via_uniq(p: *mut i32) -> &i32 {
    &*p
}
fn main() -> i32 {
    let b: GcMut(i32) = GcMut.New(42);
    *read_via_uniq(b.ptr)
}
