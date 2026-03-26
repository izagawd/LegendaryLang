fn identity<T>(x: T) -> T {
    x
}

fn main() -> i32 {
    identity(identity(5))
}
