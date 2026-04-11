struct Inner(T:! type) { val: T }
struct Outer(T:! type) { inner: Inner(T) }
fn consume[T:! type](o: Outer(T)) -> i32 { 0 }
fn main() -> i32 { 0 }
