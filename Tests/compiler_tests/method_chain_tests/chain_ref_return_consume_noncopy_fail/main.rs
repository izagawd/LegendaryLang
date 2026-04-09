// w.get_inner().consume() — get_inner returns &Inner. consume takes self:Self.
// Inner is non-Copy. Can't move out of &Inner through auto-deref.
struct Inner { val: i32 }
struct Wrapper { inner: Inner }
impl Wrapper { fn get_inner(self: &Self) -> &Inner { &self.inner } }
impl Inner { fn consume(self: Self) -> i32 { self.val } }
fn main() -> i32 {
    let w = make Wrapper { inner: make Inner { val: 42 } };
    w.get_inner().consume()
}
