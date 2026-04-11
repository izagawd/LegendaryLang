// &GcMut<Foo> → double auto-deref → Foo → consume(self:Self). Foo non-Copy.
struct Foo { val: i32 }
impl Foo { fn consume(self: Self) -> i32 { self.val } }
fn main() -> i32 {
    let b = GcMut.New(make Foo { val: 42 });
    let r: &GcMut(Foo) = &b;
    r.consume()
}
