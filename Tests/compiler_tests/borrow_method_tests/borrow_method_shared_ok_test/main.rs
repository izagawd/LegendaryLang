struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 21 };
    let r1: &Foo = &f;
    let r2: &Foo = &f;
    r1.get() + r2.get()
}
