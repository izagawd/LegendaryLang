// w.get_inner().x + w.get_inner().y — method returns &Inner, then field access through deref.
struct Inner { x: i32, y: i32 }
struct Wrapper { inner: Inner }
impl Wrapper { fn get_inner(self: &Self) -> &Inner { &self.inner } }
fn main() -> i32 {
    let w = make Wrapper { inner: make Inner { x: 20, y: 22 } };
    w.get_inner().x + w.get_inner().y
}
