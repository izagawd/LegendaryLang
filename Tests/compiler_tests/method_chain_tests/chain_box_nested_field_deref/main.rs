// Box<Outer{inner: Box<Leaf>}> — o.inner auto-deref Box to Outer, field access inner,
// then auto-deref inner Box to Leaf, call method.
struct Leaf { val: i32 }
struct Outer { inner: Box(Leaf) }
impl Leaf { fn get(self: &Self) -> i32 { self.val } }
fn main() -> i32 {
    let o = Box.New(make Outer { inner: Box.New(make Leaf { val: 42 }) });
    o.inner.get()
}
