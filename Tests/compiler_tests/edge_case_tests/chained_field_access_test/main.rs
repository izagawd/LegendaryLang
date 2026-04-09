struct Inner {
    val: i32
}
struct Outer {
    inner: Inner
}
impl Copy for Inner {}
impl Copy for Outer {}
fn main() -> i32 {
    let o = make Outer { inner : make Inner { val : 99 } };
    o.inner.val
}
