// b.get_inner().sum() — Box<Wrapper> auto-derefs. get_inner returns &Inner.
// sum auto-derefs &Inner → Inner, takes &Self.
struct Inner { x: i32, y: i32 }
struct Wrapper { inner: Inner }
impl Wrapper { fn get_inner(self: &Self) -> &Inner { &self.inner } }
impl Inner { fn sum(self: &Self) -> i32 { self.x + self.y } }
fn main() -> i32 {
    let b = Box.New(make Wrapper { inner: make Inner { x: 20, y: 22 } });
    b.get_inner().sum()
}
