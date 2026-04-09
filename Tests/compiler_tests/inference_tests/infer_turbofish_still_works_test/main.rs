fn identity[T:! type](x: T) -> T {
    x
}

fn main() -> i32 {
    let a = identity(42);
    let b = identity(10);
    a + b
}
