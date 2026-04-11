struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}
fn read_gc(r: &Gc(Foo)) -> i32 {
    r.get()
}
fn main() -> i32 {
    let g = Gc.New(make Foo { val: 42 });
    read_gc(&g)
}
