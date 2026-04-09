fn bad(p: *const i32) -> &mut i32 {
    &mut *p
}
fn main() -> i32 { 0 }
