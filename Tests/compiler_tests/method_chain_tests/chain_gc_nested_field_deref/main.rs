// Gc<Outer{inner: Gc<Leaf>}> — o.inner auto-deref Gc to Outer, field access inner,
// then auto-deref inner Gc to Leaf, call method.
struct Leaf { val: i32 }
struct Outer { inner: Gc(Leaf) }
impl Leaf { fn get(self: &Self) -> i32 { self.val } }
fn main() -> i32 {
    let o = Gc.New(make Outer { inner: Gc.New(make Leaf { val: 42 }) });
    o.inner.get()
}
