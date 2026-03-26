struct Wrapper<T> {
    val: T
}
impl<T: Copy> Copy for Wrapper<T> {}
fn extract<T: Copy>(w: Wrapper<T>) -> T {
    w.val
}
fn main() -> i32 {
    let w = Wrapper::<i32> { val = 55 };
    extract(w)
}
