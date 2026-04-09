fn bad(r: &const i32) -> i32 {
    let a: &uniq i32 = &uniq *r;
    *a
}
fn main() -> i32 { 0 }
