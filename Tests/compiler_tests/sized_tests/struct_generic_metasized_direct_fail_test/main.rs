struct Wrapper[T:! MetaSized] { val: T }
fn unwrap[T:! MetaSized](w: Wrapper(T)) -> T { w.val }
fn main() -> i32 { 0 }
