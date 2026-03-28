fn first(a: &i32, b: i32) -> &i32 {
    a
}
fn main() -> i32 {
    let x = 10;
    let r = first(&x, 5);
    *r
}
