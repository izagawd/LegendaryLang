fn identity(T:! Sized +Copy, x: T) -> T {
    x
}
fn main() -> i32 {
    let a = identity(i32, 5);
    let b = identity(i32, 10);
    a + b
}
