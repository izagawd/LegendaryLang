struct Wrapper[T:! Sized + MetaSized] { val: T }
fn unwrap[T:! Sized + MetaSized](w: Wrapper(T)) -> T { w.val }
fn main() -> i32 { unwrap(make Wrapper { val: 33 }) }
