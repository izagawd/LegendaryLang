struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn need_mut(self: &mut Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 5 };
    let r: &Foo = &f;
    r.need_mut()
}
