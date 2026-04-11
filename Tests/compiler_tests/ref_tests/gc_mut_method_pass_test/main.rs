struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn set(self: &mut Self, v: i32) { self.val = v; }
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let g = GcMut.New(make Foo { val: 0 });
    g.set(42);
    g.get()
}
