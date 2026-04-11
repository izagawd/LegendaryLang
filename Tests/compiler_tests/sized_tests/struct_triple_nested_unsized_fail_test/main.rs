struct A(T:! type) { val: T }
struct B(T:! type) { a: A(T) }
struct C(T:! type) { b: B(T) }
fn consume[T:! type](c: C(T)) -> i32 { 0 }
fn main() -> i32 { 0 }
