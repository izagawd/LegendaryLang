fn read_via_uniq(p: *mut i32) -> &i32 {
    &*p
}
fn main() -> i32 {
    let b: Gc(i32) = Gc.New(42);
    *read_via_uniq(b.ptr)
}
