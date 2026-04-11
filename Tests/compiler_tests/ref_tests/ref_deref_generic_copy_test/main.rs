fn deref_twice[T:! Sized +Copy](r: &T) -> T {
    let a = *r;
    a
}
fn main() -> i32 {
    let x = 42;
    deref_twice(&x)
}
