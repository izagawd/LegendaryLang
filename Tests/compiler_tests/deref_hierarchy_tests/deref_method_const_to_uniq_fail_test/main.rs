struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn need_uniq(self: &uniq Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 5 };
    let r: &const Foo = &const f;
    r.need_uniq()
}
