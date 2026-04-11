// b.frozen() takes &const Self through Gc auto-deref. Gc has DerefConst via DerefUniq.
struct Foo { val: i32 }
impl Foo { fn frozen(self: &const Self) -> i32 { self.val } }
fn main() -> i32 {
    let b = Gc.New(make Foo { val: 42 });
    b.frozen()
}
