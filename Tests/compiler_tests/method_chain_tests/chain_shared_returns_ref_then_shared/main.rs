// x.get_inner().sum() — first returns &Inner, second reads through auto-deref.
struct Wrapper { inner: Inner }
struct Inner { x: i32, y: i32 }
impl Wrapper { fn get_inner(self: &Self) -> &Inner { &self.inner } }
impl Inner { fn sum(self: &Self) -> i32 { self.x + self.y } }
fn main() -> i32 {
    let w = make Wrapper { inner: make Inner { x: 20, y: 22 } };
    w.get_inner().sum()
}
