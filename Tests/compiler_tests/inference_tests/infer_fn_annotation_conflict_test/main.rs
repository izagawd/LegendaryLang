fn identity[T:! Sized](x: T) -> T {
    x
}

fn main() -> i32 {
    let a : bool = identity(5);
    3
}
