fn bad(r: &i32) -> i32 {
    let a: &const i32 = &const *r;
    *a
}
fn main() -> i32 { 0 }
