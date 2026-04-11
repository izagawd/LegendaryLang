fn identity[T:! Sized](x: T) -> T { x }
fn main() -> i32 {
    identity(i32, 42)
}
