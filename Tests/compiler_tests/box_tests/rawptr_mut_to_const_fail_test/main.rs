fn bad(p: *mut i32) -> &const i32 {
    &const *p
}
fn main() -> i32 { 0 }
