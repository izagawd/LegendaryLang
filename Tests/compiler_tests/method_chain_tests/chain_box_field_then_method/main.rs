// s.boxed.get_ref().val — field access to Box, auto-deref, method returns &Inner, field access.
struct Inner { val: i32 }
struct Foo { inner: Inner }
struct Container { boxed: Box(Foo) }
impl Foo { fn get_ref(self: &Self) -> &Inner { &self.inner } }
fn main() -> i32 {
    let s = make Container { boxed: Box.New(make Foo { inner: make Inner { val: 42 } }) };
    s.boxed.get_ref().val
}
