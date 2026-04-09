fn identity(x: &i32) -> &i32 { x }
fn main() -> i32 {
    let v = 42;
    *identity(&v)
}
