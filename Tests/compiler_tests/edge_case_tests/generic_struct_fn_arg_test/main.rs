struct Wrapper(T:! Sized) {
    val: T
}
impl[T:! Sized +Copy] Copy for Wrapper(T) {}
fn extract[T:! Sized +Copy](w: Wrapper(T)) -> T {
    w.val
}
fn main() -> i32 {
    let w = make Wrapper(i32) { val : 55 };
    extract(w)
}
