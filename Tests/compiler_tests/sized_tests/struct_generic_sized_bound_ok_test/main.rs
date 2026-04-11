struct Wrapper(T:! Sized +Sized) { val: T }
fn unwrap[T:! Sized +Sized](w: Wrapper(T)) -> T { w.val }
fn main() -> i32 { unwrap(make Wrapper { val: 10 }) }
