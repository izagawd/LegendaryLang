struct Foo { val: i32 }
impl Copy for Foo {}
impl Foo {
    fn get_uniq(self: &uniq Self) -> i32 { self.val }
}
fn main() -> i32 {
    let f = make Foo { val: 5 };
    let r1: &uniq Foo = &uniq f;
    let r2: &uniq Foo = &uniq f;
    r1.get_uniq() + r2.get_uniq()
}
