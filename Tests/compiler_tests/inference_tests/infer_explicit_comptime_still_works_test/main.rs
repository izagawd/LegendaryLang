fn identity[T:! Sized](x: T) -> T {
    x
}

fn main() -> i32 {
    let a = identity(42);
    let b = identity(10);
    a + b
}
