fn add_refs(a: &i32, b: &i32) -> i32 {
    *a + *b
}
fn main() -> i32 {
    let x = 3;
    let y = 7;
    add_refs(&x, &y)
}
