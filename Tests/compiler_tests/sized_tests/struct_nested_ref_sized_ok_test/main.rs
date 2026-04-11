struct Inner['a](T:! type) { ptr: &'a T }
struct Outer['a](T:! type) { inner: Inner['a](T) }
fn consume['a, T:! type](o: Outer(T)) -> i32 { 0 }
fn main() -> i32 {
    let x: i32 = 42;
    consume(make Outer { inner: make Inner { ptr: &x } })
}
