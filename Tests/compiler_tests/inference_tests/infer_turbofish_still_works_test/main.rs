fn identity<T>(x: T) -> T {
    x
}

fn main() -> i32 {
    let a = identity::<i32>(42);
    let b = identity(10);
    a + b
}
