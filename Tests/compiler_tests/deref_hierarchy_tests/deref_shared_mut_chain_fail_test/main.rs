struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn set(self: &mut Self) -> i32 { self.val }
}
fn main() -> i32 {
    let dd = make Foo { val: 42 };
    let r: &mut Foo = &mut dd;
    let sr: &&mut Foo = &r;
    sr.set()
}
