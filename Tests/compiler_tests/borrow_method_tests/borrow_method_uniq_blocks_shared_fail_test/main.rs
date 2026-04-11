struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get_uniq(self: &mut Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 5 };
    let r: &mut Foo = &mut f;
    let s: &Foo = &f;
    r.get_uniq()
}
