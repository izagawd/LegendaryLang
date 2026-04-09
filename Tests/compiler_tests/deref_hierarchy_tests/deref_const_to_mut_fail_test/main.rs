fn bad(r: &const i32) -> i32 {
    let a: &mut i32 = &mut *r;
    *a
}
fn main() -> i32 { 0 }
