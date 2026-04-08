struct Inner[T:! MetaSized] { ptr: &T }
struct Outer[T:! MetaSized] { inner: Inner(T) }
fn consume[T:! MetaSized](o: Outer(T)) -> i32 { 0 }
fn main() -> i32 {
    let x: i32 = 42;
    consume(make Outer { inner: make Inner { ptr: &x } })
}
