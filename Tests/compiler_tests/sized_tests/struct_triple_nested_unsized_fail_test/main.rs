struct A(T:! MetaSized) { val: T }
struct B(T:! MetaSized) { a: A(T) }
struct C(T:! MetaSized) { b: B(T) }
fn consume[T:! MetaSized](c: C(T)) -> i32 { 0 }
fn main() -> i32 { 0 }
