struct Wrapper(T:! type) { val: T }
fn unwrap[T:! type](w: Wrapper(T)) -> T { w.val }
fn main() -> i32 { 0 }
