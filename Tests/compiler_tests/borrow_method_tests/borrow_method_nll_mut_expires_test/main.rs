struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get(self: &Self) -> i32 { self.val }
    fn get_mut(self: &mut Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 21 };
    let r: &mut Foo = &mut f;
    let a: i32 = r.get_mut();
    f.get() + a
}
