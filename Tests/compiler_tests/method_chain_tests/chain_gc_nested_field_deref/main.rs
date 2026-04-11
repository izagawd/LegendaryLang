// GcMut<Outer{inner: GcMut<Leaf>}> — o.inner auto-deref GcMut to Outer, field access inner,
// then auto-deref inner GcMut to Leaf, call method.
struct Leaf { val: i32 }
struct Outer { inner: GcMut(Leaf) }
impl Leaf { fn get(self: &Self) -> i32 { self.val } }
fn main() -> i32 {
    let o = GcMut.New(make Outer { inner: GcMut.New(make Leaf { val: 42 }) });
    o.inner.get()
}
