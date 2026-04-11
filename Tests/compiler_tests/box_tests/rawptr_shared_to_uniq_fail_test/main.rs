fn bad(p: *shared i32) -> &mut i32 {
    &mut *p
}
fn main() -> i32 { 0 }
