fn identity[T:! type](x: T) -> T {
    x
}

fn main() -> i32 {
    identity(identity(5))
}
