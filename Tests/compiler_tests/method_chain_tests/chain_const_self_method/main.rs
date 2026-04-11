// b.frozen() takes &const Self through GcMut auto-deref. GcMut has DerefConst via DerefUniq.
struct Foo { val: i32 }
impl Foo { fn frozen(self: &const Self) -> i32 { self.val } }
fn main() -> i32 {
    let b = GcMut.New(make Foo { val: 42 });
    b.frozen()
}
