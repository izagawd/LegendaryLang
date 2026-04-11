struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
    fn get_uniq(self: &mut Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 5 };
    let s: &Foo = &f;
    let r: &mut Foo = &mut f;
    s.get() + r.get_uniq()
}
