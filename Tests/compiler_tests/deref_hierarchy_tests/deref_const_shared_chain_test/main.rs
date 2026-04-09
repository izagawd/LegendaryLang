struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let dd = make Foo { val: 42 };
    let r: &Foo = &dd;
    let cr: &const &Foo = &const r;
    cr.get()
}
