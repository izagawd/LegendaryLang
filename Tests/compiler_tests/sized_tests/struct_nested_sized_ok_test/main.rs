struct Inner { val: i32 }
struct Outer { inner: Inner }
fn consume(o: Outer) -> i32 { o.inner.val }
fn main() -> i32 {
    consume(make Outer { inner: make Inner { val: 42 } })
}
