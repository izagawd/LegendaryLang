struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 14 };
    let r: &Foo = &f;
    f.get() + r.get() + f.get()
}
