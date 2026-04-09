fn deref_twice[T:! Copy](r: &T) -> T {
    let a = *r;
    a
}
fn main() -> i32 {
    let x = 42;
    deref_twice(&x)
}
