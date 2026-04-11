struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let g = Gc.New(make Foo { val: 42 });
    g.get()
}
