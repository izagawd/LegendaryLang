struct Inner(T:! MetaSized) { val: T }
struct Outer(T:! MetaSized) { inner: Inner(T) }
fn consume[T:! MetaSized](o: Outer(T)) -> i32 { 0 }
fn main() -> i32 { 0 }
