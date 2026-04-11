fn bad(r: &mut i32) -> i32 {
    let a: &mut i32 = &mut *r;
    *a
}
fn main() -> i32 { 0 }
