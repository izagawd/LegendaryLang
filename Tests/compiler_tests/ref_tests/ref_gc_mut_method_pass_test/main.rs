struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn set(self: &mut Self, v: i32) { self.val = v; }
}
fn try_write_gc(r: &Gc(Foo)) {
    r.set(42);
}
fn main() -> i32 {
    let g = Gc.New(make Foo { val: 0 });
    try_write_gc(&g);
    g.val
}
