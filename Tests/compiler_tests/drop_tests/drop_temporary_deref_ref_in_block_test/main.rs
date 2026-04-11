// &*GcMut.New(45) — taking a reference to the result of a temporary deref.
// The reference is used within the same scope as the temporary, which is valid.
// The GcMut is dropped at scope exit after the reference is last used.
fn main() -> i32 {
    let r: &i32 = &*GcMut.New(45);
    *r
}
