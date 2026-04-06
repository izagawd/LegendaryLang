fn identity[T:! type](x: T) -> T { x }
fn main() -> i32 {
    identity(i32, 42)
}
