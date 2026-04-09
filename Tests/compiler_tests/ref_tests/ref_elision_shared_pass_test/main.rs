fn identity(r: &i32) -> &i32 {
    r
}
fn main() -> i32 {
    let a = 5;
    let derived = identity(&a);
    &a;
    *derived
}
